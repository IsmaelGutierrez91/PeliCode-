using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.CloudServices;
using PeliCode.Localization;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace PeliCode.Authentication
{
    public class AuthManager : Singleton<AuthManager>
    {
        public event Action<PlayerProfileData> OnSignedIn;
        public event Action OnSignedOut;

        public PlayerProfileData CurrentProfile { get; private set; }
        public bool IsSignedIn => AuthenticationService.Instance != null &&
                                   AuthenticationService.Instance.IsSignedIn;

        // Nombre elegido en un registro cuya cuenta se creo pero cuyo perfil no se pudo
        // guardar: se usa en el siguiente inicio de sesion de esa cuenta.
        private readonly Dictionary<string, string> _pendingUserNames = new Dictionary<string, string>();

        private static string EmailToUgsUsername(string email)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
                if (sb.Length >= 16) break;
            }
            return sb.ToString();
        }

        public async Task<(bool success, string error)> SignInAsync(string email, string password)
        {
            await ServicesInitializer.InitializeAsync();

            if (!PasswordValidator.IsValidEmail(email))
                return (false, Loc.Get("auth.invalidEmail"));
            if (string.IsNullOrEmpty(password))
                return (false, Loc.Get("auth.enterPassword"));

            string ugsUsername = EmailToUgsUsername(email);
            SignOutOfService();

            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(ugsUsername, password);
            }
            catch (RequestFailedException e) when (IsConnectionError(e))
            {
                return (false, ConnectionMessage(e));
            }
            catch (RequestFailedException)
            {
                return (false, Loc.Get("auth.wrongCredentials"));
            }

            string fallbackName = _pendingUserNames.TryGetValue(ugsUsername, out var pending)
                ? pending
                : email.Trim().Split('@')[0];
            var result = await FinishSignInAsync(fallbackName, email.Trim());
            if (result.success) _pendingUserNames.Remove(ugsUsername);
            return result;
        }

        /// <summary>
        /// Vuelve a entrar con la sesion guardada en este dispositivo, sin pedir correo ni
        /// contrasena. Unity guarda la sesion al iniciar sesion y SignInAnonymouslyAsync la reusa
        /// (con la misma cuenta, aunque sea de usuario y contrasena). Solo se llama si hay una
        /// sesion guardada: sin ella crearia una cuenta anonima nueva. Cerrar sesion la borra.
        /// </summary>
        public async Task<bool> TryResumeSessionAsync()
        {
            await ServicesInitializer.InitializeAsync();
            if (!ServicesInitializer.IsInitialized || AuthenticationService.Instance == null) return false;
            if (IsSignedIn && CurrentProfile != null) return true;
            if (!AuthenticationService.Instance.SessionTokenExists) return false;

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (RequestFailedException e)
            {
                // Sesion vencida o invalida: se borra y se pide el login normal
                Debug.LogWarning($"[AuthManager] No se pudo retomar la sesion: {e.Message}");
                if (!IsConnectionError(e)) AuthenticationService.Instance.ClearSessionToken();
                return false;
            }

            var (success, _) = await FinishSignInAsync(Loc.Get("common.player"), string.Empty);
            return success;
        }

        /// <summary>Despues de autenticar: carga (o crea) el perfil, actualiza la racha y lo guarda.</summary>
        private async Task<(bool success, string error)> FinishSignInAsync(string fallbackName, string email)
        {
            // Ya hay sesion en Unity: si algo falla de aqui en adelante se cierra, para que
            // el siguiente intento no choque con "el jugador ya inicio sesion".
            try
            {
                var profile = await CloudSaveManager.Instance.LoadProfileAsync();
                if (profile == null)
                {
                    Debug.LogWarning("[AuthManager] La cuenta no tenia player_profile, se crea uno nuevo.");
                    profile = PlayerProfileData.CreateNew(fallbackName, email);
                }

                CloudSaveManager.Instance.UpdateLoginStreak(profile);
                CloudSaveManager.Instance.ResetWeeklyStatsIfNeeded(profile);
                await CloudSaveManager.Instance.SaveProfileAsync(profile);

                CurrentProfile = profile;
                await SyncPlayerNameAsync(profile.userName);

                OnSignedIn?.Invoke(CurrentProfile);
                return (true, string.Empty);
            }
            catch (RequestFailedException e)
            {
                Debug.LogError($"[AuthManager] Error cargando el perfil: {e}");
                // Sin conexion la sesion guardada sigue sirviendo: no se borra
                SignOut(clearSavedSession: !IsConnectionError(e));
                return (false, IsConnectionError(e)
                    ? ConnectionMessage(e)
                    : Loc.Get("auth.profileLoadFailed"));
            }
        }

        public async Task<(bool success, string error)> SignUpAsync(string userName, string email, string password)
        {
            await ServicesInitializer.InitializeAsync();

            string userNameError = PasswordValidator.GetUserNameValidationMessage(userName);
            if (!string.IsNullOrEmpty(userNameError))
                return (false, userNameError);

            string validation = PasswordValidator.GetValidationMessage(email, password);
            if (!string.IsNullOrEmpty(validation))
                return (false, validation);

            userName = userName.Trim();
            email = email.Trim();
            string ugsUsername = EmailToUgsUsername(email);
            SignOutOfService();

            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(ugsUsername, password);
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked ||
                                                    e.ErrorCode == CommonErrorCodes.Conflict)
            {
                return (false, Loc.Get("auth.emailTaken"));
            }
            catch (RequestFailedException e) when (IsConnectionError(e))
            {
                return (false, ConnectionMessage(e));
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.InvalidParameters)
            {
                return (false, Loc.Get("auth.serverRejected"));
            }
            catch (RequestFailedException e)
            {
                Debug.LogError($"[AuthManager] Error creando la cuenta: {e}");
                return (false, Loc.Get("auth.signUpFailed"));
            }

            try
            {
                var profile = PlayerProfileData.CreateNew(userName, email);
                await CloudSaveManager.Instance.SaveProfileAsync(profile);

                CurrentProfile = profile;
                await SyncPlayerNameAsync(userName);

                OnSignedIn?.Invoke(CurrentProfile);
                return (true, string.Empty);
            }
            catch (RequestFailedException e)
            {
                // La cuenta existe pero sin perfil: el siguiente inicio de sesion lo crea.
                Debug.LogError($"[AuthManager] Cuenta creada sin perfil: {e}");
                _pendingUserNames[ugsUsername] = userName;
                SignOut();
                return (false, Loc.Get("auth.accountWithoutProfile"));
            }
        }

        /// <summary>Cerrar sesion: tambien borra la sesion guardada en el dispositivo.</summary>
        public void SignOut() => SignOut(clearSavedSession: true);

        private void SignOut(bool clearSavedSession)
        {
            SignOutOfService(clearSavedSession);
            CurrentProfile = null;
            OnSignedOut?.Invoke();
        }

        // clearCredentials: true borra tambien la sesion guardada, para que al cerrar sesion (o
        // al entrar con otra cuenta) el juego no vuelva a entrar solo con la cuenta anterior.
        private static void SignOutOfService(bool clearSavedSession = true)
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut(clearCredentials: clearSavedSession);
            }
        }

        /// <summary>
        /// El Leaderboard muestra el "Player Name" de Unity, que por defecto es un nombre
        /// aleatorio: se iguala al nombre del perfil. Si falla, no impide jugar.
        /// </summary>
        private static async Task SyncPlayerNameAsync(string userName)
        {
            string wanted = PlayerNames.ToServiceName(userName);
            try
            {
                string current = AuthenticationService.Instance.PlayerName;
                if (!string.IsNullOrEmpty(current) && PlayerNames.ToDisplayName(current) == wanted) return;
                await AuthenticationService.Instance.UpdatePlayerNameAsync(wanted);
            }
            catch (RequestFailedException e)
            {
                Debug.LogWarning($"[AuthManager] No se pudo actualizar el nombre en el ranking: {e.Message}");
            }
        }

        private static bool IsConnectionError(RequestFailedException e) =>
            e.ErrorCode == CommonErrorCodes.TransportError ||
            e.ErrorCode == CommonErrorCodes.Timeout ||
            e.ErrorCode == CommonErrorCodes.ServiceUnavailable ||
            e.ErrorCode == CommonErrorCodes.TooManyRequests;

        private static string ConnectionMessage(RequestFailedException e) =>
            e.ErrorCode == CommonErrorCodes.TooManyRequests
                ? Loc.Get("auth.tooManyRequests")
                : Loc.Get("auth.noConnection");
    }
}

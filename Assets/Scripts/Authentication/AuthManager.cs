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

            // Ya hay sesion en Unity: si algo falla de aqui en adelante se cierra, para que
            // el siguiente intento no choque con "el jugador ya inicio sesion".
            try
            {
                var profile = await CloudSaveManager.Instance.LoadProfileAsync();
                if (profile == null)
                {
                    Debug.LogWarning("[AuthManager] La cuenta no tenia player_profile, se crea uno nuevo.");
                    string userName = _pendingUserNames.TryGetValue(ugsUsername, out var pending)
                        ? pending
                        : email.Trim().Split('@')[0];
                    profile = PlayerProfileData.CreateNew(userName, email.Trim());
                }

                CloudSaveManager.Instance.UpdateLoginStreak(profile);
                CloudSaveManager.Instance.ResetWeeklyStatsIfNeeded(profile);
                await CloudSaveManager.Instance.SaveProfileAsync(profile);
                _pendingUserNames.Remove(ugsUsername);

                CurrentProfile = profile;
                await SyncPlayerNameAsync(profile.userName);

                OnSignedIn?.Invoke(CurrentProfile);
                return (true, string.Empty);
            }
            catch (RequestFailedException e)
            {
                Debug.LogError($"[AuthManager] Error cargando el perfil: {e}");
                SignOut();
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

        public void SignOut()
        {
            SignOutOfService();
            CurrentProfile = null;
            OnSignedOut?.Invoke();
        }

        private static void SignOutOfService()
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
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

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.CloudServices;
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
                return (false, "Correo electronico invalido.");

            try
            {
                string ugsUsername = EmailToUgsUsername(email);
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(ugsUsername, password);
                CurrentProfile = await CloudSaveManager.Instance.LoadProfileAsync();
                CurrentProfile = CloudSaveManager.Instance.UpdateLoginStreak(CurrentProfile);
                await CloudSaveManager.Instance.SaveProfileAsync(CurrentProfile);

                OnSignedIn?.Invoke(CurrentProfile);
                return (true, string.Empty);
            }
            catch (AuthenticationException)
            {
                return (false, "Correo o contrasena incorrectos.");
            }
            catch (RequestFailedException e)
            {
                return (false, $"Error de red/servicio: {e.Message}");
            }
        }

        public async Task<(bool success, string error)> SignUpAsync(string userName, string email, string password)
        {
            await ServicesInitializer.InitializeAsync();

            string validation = PasswordValidator.GetValidationMessage(email, password);
            if (!string.IsNullOrEmpty(validation))
                return (false, validation);

            if (string.IsNullOrWhiteSpace(userName))
                return (false, "El nombre de usuario no puede estar vacio.");

            try
            {
                string ugsUsername = EmailToUgsUsername(email);
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(ugsUsername, password);

                CurrentProfile = PlayerProfileData.CreateNew(userName, email);
                await CloudSaveManager.Instance.SaveProfileAsync(CurrentProfile);

                OnSignedIn?.Invoke(CurrentProfile);
                return (true, string.Empty);
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                return (false, "Ese correo ya esta registrado.");
            }
            catch (AuthenticationException e)
            {
                return (false, $"No se pudo crear la cuenta: {e.Message}");
            }
            catch (RequestFailedException e)
            {
                return (false, $"Error de red/servicio: {e.Message}");
            }
        }

        public void SignOut()
        {
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
            CurrentProfile = null;
            OnSignedOut?.Invoke();
        }
    }
}
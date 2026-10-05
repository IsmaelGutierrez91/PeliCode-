using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace PeliCode.Core
{
    /// <summary>
    /// Inicializa el SDK de Unity Gaming Services (UGS) una sola vez por sesion de juego.
    /// Todos los managers (AuthManager, CloudSaveManager, LeaderboardManager) deben
    /// esperar a InitializeAsync() antes de usar sus servicios.
    /// </summary>
    public static class ServicesInitializer
    {
        public static bool IsInitialized { get; private set; }
        public static event Action OnServicesInitialized;

        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                await UnityServices.InitializeAsync();
                IsInitialized = true;
                Debug.Log("[ServicesInitializer] Unity Services inicializado correctamente.");
                OnServicesInitialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServicesInitializer] Error al inicializar Unity Services: {e}");
            }
        }
    }
}

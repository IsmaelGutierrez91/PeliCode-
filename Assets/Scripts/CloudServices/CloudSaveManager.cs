using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

namespace PeliCode.CloudServices
{
    /// <summary>
    /// Encapsula Unity Cloud Save (Player Data) para el perfil del jugador:
    /// nombre, top ranked, rachas y estadisticas por minijuego.
    /// Clave usada en Cloud Save: "player_profile".
    /// Las reglas (rachas, reseteo semanal) viven en PeliCode.Data.ProfileRules.
    /// </summary>
    public class CloudSaveManager : Singleton<CloudSaveManager>
    {
        private const string PROFILE_KEY = "player_profile";

        public async Task SaveProfileAsync(PlayerProfileData profile)
        {
            var data = new Dictionary<string, object> { { PROFILE_KEY, profile } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }

        /// <summary>
        /// Devuelve el perfil guardado, o null si el jugador todavia no tiene uno
        /// (por ejemplo, si el registro creo la cuenta pero fallo al guardar el perfil).
        /// </summary>
        public async Task<PlayerProfileData> LoadProfileAsync()
        {
            var query = new HashSet<string> { PROFILE_KEY };
            Dictionary<string, Item> result = await CloudSaveService.Instance.Data.Player.LoadAsync(query);

            if (result.TryGetValue(PROFILE_KEY, out var item))
            {
                var profile = item.Value.GetAs<PlayerProfileData>();
                profile.minigameStats ??= new Dictionary<string, MiniGameStat>();
                return profile;
            }
            return null;
        }

        /// <summary>
        /// Calcula la racha de dias consecutivos de inicio de sesion.
        /// Llamar justo despues de un SignIn exitoso.
        /// </summary>
        public PlayerProfileData UpdateLoginStreak(PlayerProfileData profile)
        {
            ProfileRules.UpdateLoginStreak(profile, DateTime.UtcNow);
            return profile;
        }

        public void ResetWeeklyStatsIfNeeded(PlayerProfileData profile)
        {
            ProfileRules.ResetWeeklyStatsIfNeeded(profile, DateTime.UtcNow);
        }

        public void RegisterNormalGameResult(
            PlayerProfileData profile,
            MiniGameCategory category,
            Difficulty difficulty,
            int correctAnswers,
            int totalQuestions)
        {
            ProfileRules.RegisterNormalGameResult(
                profile, category, difficulty, correctAnswers, totalQuestions, DateTime.UtcNow);
        }
    }
}

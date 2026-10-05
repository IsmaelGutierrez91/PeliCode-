using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace PeliCode.CloudServices
{
    /// <summary>
    /// Encapsula Unity Cloud Save (Player Data) para el perfil del jugador:
    /// nombre, top ranked, rachas y estadisticas por minijuego.
    /// Clave usada en Cloud Save: "player_profile".
    /// </summary>
    public class CloudSaveManager : Singleton<CloudSaveManager>
    {
        private const string PROFILE_KEY = "player_profile";

        public async Task SaveProfileAsync(PlayerProfileData profile)
        {
            var data = new Dictionary<string, object> { { PROFILE_KEY, profile } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }

        public async Task<PlayerProfileData> LoadProfileAsync()
        {
            var query = new HashSet<string> { PROFILE_KEY };
            Dictionary<string, Item> result = await CloudSaveService.Instance.Data.Player.LoadAsync(query);

            if (result.TryGetValue(PROFILE_KEY, out var item))
            {
                return item.Value.GetAs<PlayerProfileData>();
            }

            // No deberia pasar si el registro creo el perfil correctamente,
            // pero se deja como salvaguarda.
            Debug.LogWarning("[CloudSaveManager] No se encontro player_profile, se crea uno vacio.");
            return PlayerProfileData.CreateNew("Jugador", string.Empty);
        }

        /// <summary>
        /// Calcula la racha de dias consecutivos de inicio de sesion.
        /// Llamar justo despues de un SignIn exitoso.
        /// </summary>
        public PlayerProfileData UpdateLoginStreak(PlayerProfileData profile)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime lastLogin = DateTime.TryParse(profile.lastLoginDateUtc, out var parsed)
                ? parsed
                : today;

            int daysSinceLastLogin = (today - lastLogin).Days;

            if (daysSinceLastLogin == 1)
            {
                profile.currentStreak += 1;
            }
            else if (daysSinceLastLogin > 1)
            {
                profile.currentStreak = 1;
            }
            // daysSinceLastLogin == 0 -> mismo dia, no se modifica la racha.

            if (profile.currentStreak > profile.highestStreak)
            {
                profile.highestStreak = profile.currentStreak;
            }

            profile.lastLoginDateUtc = today.ToString("yyyy-MM-dd");
            return profile;
        }

        /// <summary>
        /// El porcentaje de aciertos de cada minijuego se resetea semanalmente.
        /// Se compara el "week stamp" (formato yyyy-Www, ISO 8601) guardado contra el actual.
        /// </summary>
        public void ResetWeeklyStatsIfNeeded(PlayerProfileData profile)
        {
            string currentWeekStamp = GetIsoWeekStamp(DateTime.UtcNow);

            foreach (var stat in profile.minigameStats.Values)
            {
                if (stat.weekStamp != currentWeekStamp)
                {
                    stat.easyPercentage = 0f;
                    stat.mediumPercentage = 0f;
                    stat.hardPercentage = 0f;
                    stat.weekStamp = currentWeekStamp;
                }
            }
        }

        private static string GetIsoWeekStamp(DateTime date)
        {
            int week = ISOWeek.GetWeekOfYear(date);
            int year = ISOWeek.GetYear(date);
            return $"{year}-W{week:D2}";
        }

        /// <summary>
        /// Actualiza el porcentaje de aciertos de una categoria/dificultad tras jugar
        /// una partida normal (no ranked). correctAnswers/totalQuestions definen el % de esa partida.
        /// Regla del documento: se guarda el porcentaje de la partida (no un promedio acumulado).
        /// </summary>
        public void RegisterNormalGameResult(
            PlayerProfileData profile,
            MiniGameCategory category,
            Difficulty difficulty,
            int correctAnswers,
            int totalQuestions)
        {
            ResetWeeklyStatsIfNeeded(profile);

            string key = category.ToString();
            if (!profile.minigameStats.TryGetValue(key, out var stat))
            {
                stat = new MiniGameStat();
                profile.minigameStats[key] = stat;
            }

            float percentage = totalQuestions > 0
                ? (correctAnswers / (float)totalQuestions) * 100f
                : 0f;

            stat.weekStamp = GetIsoWeekStamp(DateTime.UtcNow);

            switch (difficulty)
            {
                case Difficulty.Facil: stat.easyPercentage = percentage; break;
                case Difficulty.Medio: stat.mediumPercentage = percentage; break;
                case Difficulty.Dificil: stat.hardPercentage = percentage; break;
            }
        }
    }
}

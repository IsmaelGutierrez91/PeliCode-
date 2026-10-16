using System;
using System.Globalization;

namespace PeliCode.Data
{
    /// <summary>
    /// Reglas puras del perfil (rachas, reseteo semanal y porcentaje por partida).
    /// No dependen de Unity ni de Cloud Save, asi se pueden probar en EditMode
    /// pasando la fecha "de hoy" como parametro.
    /// </summary>
    public static class ProfileRules
    {
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// Calcula la racha de dias consecutivos de inicio de sesion.
        /// </summary>
        public static void UpdateLoginStreak(PlayerProfileData profile, DateTime nowUtc)
        {
            DateTime today = nowUtc.Date;
            DateTime lastLogin = DateTime.TryParseExact(profile.lastLoginDateUtc, DateFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : today;

            int daysSinceLastLogin = (today - lastLogin).Days;

            if (daysSinceLastLogin == 1)
            {
                profile.currentStreak += 1;
            }
            else if (daysSinceLastLogin > 1 || profile.currentStreak < 1)
            {
                profile.currentStreak = 1;
            }
            // daysSinceLastLogin == 0 -> mismo dia, no se modifica la racha.

            if (profile.currentStreak > profile.highestStreak)
            {
                profile.highestStreak = profile.currentStreak;
            }

            profile.lastLoginDateUtc = today.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// El porcentaje de aciertos de cada minijuego se resetea semanalmente.
        /// Se compara el "week stamp" (formato yyyy-Www, ISO 8601) guardado contra el actual.
        /// Devuelve true si cambio algo.
        /// </summary>
        public static bool ResetWeeklyStatsIfNeeded(PlayerProfileData profile, DateTime nowUtc)
        {
            string currentWeekStamp = GetIsoWeekStamp(nowUtc);
            bool changed = false;

            foreach (var stat in profile.minigameStats.Values)
            {
                if (stat.weekStamp != currentWeekStamp)
                {
                    stat.easyPercentage = 0f;
                    stat.mediumPercentage = 0f;
                    stat.hardPercentage = 0f;
                    stat.weekStamp = currentWeekStamp;
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Guarda el porcentaje de aciertos de una categoria/dificultad tras una partida
        /// normal (no ranked). Regla del documento: se guarda el porcentaje de la partida,
        /// no un promedio acumulado. Una partida sin preguntas no cambia nada.
        /// </summary>
        public static void RegisterNormalGameResult(
            PlayerProfileData profile,
            MiniGameCategory category,
            Difficulty difficulty,
            int correctAnswers,
            int totalQuestions,
            DateTime nowUtc)
        {
            if (totalQuestions <= 0) return;

            ResetWeeklyStatsIfNeeded(profile, nowUtc);

            string key = category.ToString();
            if (!profile.minigameStats.TryGetValue(key, out var stat))
            {
                stat = new MiniGameStat();
                profile.minigameStats[key] = stat;
            }

            float percentage = correctAnswers / (float)totalQuestions * 100f;
            stat.weekStamp = GetIsoWeekStamp(nowUtc);

            switch (difficulty)
            {
                case Difficulty.Facil: stat.easyPercentage = percentage; break;
                case Difficulty.Medio: stat.mediumPercentage = percentage; break;
                case Difficulty.Dificil: stat.hardPercentage = percentage; break;
            }
        }

        public static string GetIsoWeekStamp(DateTime date)
        {
            int week = ISOWeek.GetWeekOfYear(date);
            int year = ISOWeek.GetYear(date);
            return $"{year}-W{week:D2}";
        }
    }
}

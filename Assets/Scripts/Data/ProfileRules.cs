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

        /// <summary>Dias seguidos desde los que el perfil muestra la insignia de racha.</summary>
        public const int StreakBadgeMinDays = 5;

        /// <summary>La imagen de la racha solo aparece desde <see cref="StreakBadgeMinDays"/> dias.</summary>
        public static bool ShowsStreakBadge(int currentStreak) => currentStreak >= StreakBadgeMinDays;

        /// <summary>El numero de la racha se ve siempre, salvo que sea 0.</summary>
        public static bool ShowsStreakNumber(int currentStreak) => currentStreak > 0;

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
        /// El porcentaje de aciertos de cada minijuego se resetea cada fin de semana (sabado 00:00).
        /// Se compara la marca de semana guardada (ver GetIsoWeekStamp) contra la actual.
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

        /// <summary>Dia en que empieza la "semana" de los porcentajes: los fines de semana se reinician.</summary>
        public const DayOfWeek WeekStartsOn = DayOfWeek.Saturday;

        /// <summary>
        /// Identifica la semana de una fecha, contando desde <see cref="WeekStartsOn"/> a las 00:00:
        /// de sabado a viernes es la misma semana. La "s" del final la distingue de las marcas
        /// ISO (lunes a domingo) que guardaban los perfiles anteriores, que se reinician una vez.
        /// </summary>
        public static string GetIsoWeekStamp(DateTime date)
        {
            // Se corre la fecha hasta que el dia de inicio caiga en lunes y se usa la semana ISO.
            int shift = ((int)DayOfWeek.Monday - (int)WeekStartsOn + 7) % 7;
            DateTime shifted = date.AddDays(shift);
            int week = ISOWeek.GetWeekOfYear(shifted);
            int year = ISOWeek.GetYear(shifted);
            return $"{year}-W{week:D2}s";
        }
    }
}

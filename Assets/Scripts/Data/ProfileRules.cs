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
                    stat.easyGames = stat.easyCorrect = stat.easyAnswered = 0;
                    stat.mediumGames = stat.mediumCorrect = stat.mediumAnswered = 0;
                    stat.hardGames = stat.hardCorrect = stat.hardAnswered = 0;
                    stat.weekStamp = currentWeekStamp;
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Suma una partida normal (no ranked) al porcentaje de su categoria/dificultad.
        /// Regla del tercer documento: el porcentaje combina todas las partidas de la semana
        /// (100 % y luego 0 % dan 50 %), contando aciertos sobre preguntas respondidas.
        /// Una partida sin preguntas no cambia nada.
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

            stat.weekStamp = GetIsoWeekStamp(nowUtc);

            switch (difficulty)
            {
                case Difficulty.Facil:
                    stat.easyPercentage = Accumulate(ref stat.easyGames, ref stat.easyCorrect, ref stat.easyAnswered,
                        correctAnswers, totalQuestions);
                    break;
                case Difficulty.Medio:
                    stat.mediumPercentage = Accumulate(ref stat.mediumGames, ref stat.mediumCorrect, ref stat.mediumAnswered,
                        correctAnswers, totalQuestions);
                    break;
                case Difficulty.Dificil:
                    stat.hardPercentage = Accumulate(ref stat.hardGames, ref stat.hardCorrect, ref stat.hardAnswered,
                        correctAnswers, totalQuestions);
                    break;
            }
        }

        private static float Accumulate(ref int games, ref int correct, ref int answered, int gameCorrect, int gameAnswered)
        {
            games++;
            correct += gameCorrect;
            answered += gameAnswered;
            return correct / (float)answered * 100f;
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

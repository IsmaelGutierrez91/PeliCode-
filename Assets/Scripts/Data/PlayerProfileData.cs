using System;
using System.Collections.Generic;

namespace PeliCode.Data
{
    /// <summary>
    /// Estructura del perfil del jugador tal como se guarda/lee de Unity Cloud Save
    /// (Player Data). Se serializa a JSON automaticamente por el SDK.
    /// Clave en Cloud Save: "player_profile"
    /// </summary>
    [Serializable]
    public class PlayerProfileData
    {
        public string userName;
        public string email;

        public int currentStreak;
        public int highestStreak;
        public int rankedHighScore;

        // Ultimo dia (fecha local del jugador, formato "yyyy-MM-dd") en que el usuario inicio
        // sesion, usado para calcular la racha de dias. El nombre del campo se mantiene por los
        // perfiles ya guardados en Cloud Save, aunque ya no es la fecha UTC.
        public string lastLoginDateUtc;

        // Estadisticas por minijuego (5 categorias). Clave = MiniGameCategory.ToString()
        public Dictionary<string, MiniGameStat> minigameStats = new Dictionary<string, MiniGameStat>();

        public static PlayerProfileData CreateNew(string userName, string email)
        {
            var profile = new PlayerProfileData
            {
                userName = userName,
                email = email,
                currentStreak = 1,
                highestStreak = 1,
                rankedHighScore = 0,
                lastLoginDateUtc = DateTime.Now.Date.ToString(ProfileRules.DateFormat, System.Globalization.CultureInfo.InvariantCulture)
            };

            foreach (MiniGameCategory category in Enum.GetValues(typeof(MiniGameCategory)))
            {
                profile.minigameStats[category.ToString()] = new MiniGameStat();
            }

            return profile;
        }
    }

    /// <summary>
    /// Porcentaje de aciertos por dificultad para una categoria de minijuego.
    /// Se resetea semanalmente (ver CloudSaveManager.ResetWeeklyStatsIfNeeded).
    /// </summary>
    [Serializable]
    public class MiniGameStat
    {
        public float easyPercentage;
        public float mediumPercentage;
        public float hardPercentage;

        // Semana ISO (yyyy-Www) de la ultima actualizacion, para saber cuando resetear.
        public string weekStamp = string.Empty;
    }

    public enum MiniGameCategory
    {
        VariablesTiposOperadores,
        Condicionales,
        SwitchEstados,
        ColeccionesArraysListas,
        BucleWhileDoWhile
    }

    public enum Difficulty
    {
        Facil,
        Medio,
        Dificil
    }
}

using System;
using System.Globalization;

namespace PeliCode.Data
{
    /// <summary>
    /// Una partida jugada, tal como queda en el historial de Cloud Save (clave "game_history",
    /// ver CloudSaveManager). A diferencia de los porcentajes del perfil, el historial no se
    /// reinicia cada semana: sirve para medir el avance de cada jugador en el tiempo y se lee
    /// desde el Dashboard.
    /// </summary>
    [Serializable]
    public class GameRecord
    {
        public const string NormalMode = "normal";
        public const string RankedMode = "ranked";
        public const string DateFormat = "yyyy-MM-ddTHH:mm:sszzz";

        public string date;        // fecha y hora del jugador con su zona horaria, ej. 2026-11-24T18:05:00-05:00
        public string mode;        // "normal" o "ranked"
        public string category;    // MiniGameCategory; vacio en ranked (mezcla todas)
        public string difficulty;  // Difficulty; vacio en ranked
        public int correct;        // preguntas acertadas
        public int answered;       // preguntas respondidas (incluye las que se acabo el tiempo)
        public int questions;      // preguntas de la partida; en ranked es igual a answered
        public int seconds;        // duracion, desde la primera pregunta hasta el final o la salida
        public bool completed;     // false si el jugador salio a mitad de partida

        public static GameRecord Normal(MiniGameCategory category, Difficulty difficulty, int correct, int answered,
            int questions, float seconds, bool completed, DateTime now) => new GameRecord
        {
            date = now.ToString(DateFormat, CultureInfo.InvariantCulture),
            mode = NormalMode,
            category = category.ToString(),
            difficulty = difficulty.ToString(),
            correct = correct,
            answered = answered,
            questions = questions,
            seconds = (int)Math.Round(seconds),
            completed = completed
        };

        public static GameRecord Ranked(int correct, int answered, float seconds, bool completed, DateTime now) => new GameRecord
        {
            date = now.ToString(DateFormat, CultureInfo.InvariantCulture),
            mode = RankedMode,
            category = string.Empty,
            difficulty = string.Empty,
            correct = correct,
            answered = answered,
            questions = answered,
            seconds = (int)Math.Round(seconds),
            completed = completed
        };
    }
}

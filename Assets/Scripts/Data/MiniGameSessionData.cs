using PeliCode.Data;

namespace PeliCode.Core
{
    /// <summary>
    /// "Puente" estatico entre la escena Menu y la escena MiniGame.
    /// Se rellena antes de cargar la escena MiniGame y se lee en MiniGameManager.Start().
    /// No es un MonoBehaviour: al ser estatico, sobrevive al cambio de escena sin
    /// necesidad de DontDestroyOnLoad.
    /// </summary>
    public static class MiniGameSessionData
    {
        public static bool IsRanked;
        public static MiniGameCategory Category;
        public static Difficulty Difficulty;

        public static void SetNormalSession(MiniGameCategory category, Difficulty difficulty)
        {
            IsRanked = false;
            Category = category;
            Difficulty = difficulty;
        }

        public static void SetRankedSession()
        {
            IsRanked = true;
            // En ranked se combinan todas las categorias y dificultades;
            // Category/Difficulty no se usan como filtro en este modo.
        }
    }
}

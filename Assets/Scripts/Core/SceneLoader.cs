using UnityEngine.SceneManagement;

namespace PeliCode.Core
{
    /// <summary>
    /// Helper estatico para cambiar de escena. Centraliza los nombres de escena
    /// para evitar strings magicos repartidos por el proyecto.
    /// </summary>
    public static class SceneLoader
    {
        public static void LoadLogin() => SceneManager.LoadScene(GameManager.SCENE_LOGIN);
        public static void LoadMenu() => SceneManager.LoadScene(GameManager.SCENE_MENU);
        public static void LoadMiniGame() => SceneManager.LoadScene(GameManager.SCENE_MINIGAME);
    }
}

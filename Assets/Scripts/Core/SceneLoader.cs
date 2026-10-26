namespace PeliCode.Core
{
    /// <summary>
    /// Helper estatico para cambiar de escena. Centraliza los nombres de escena
    /// para evitar strings magicos repartidos por el proyecto. Todos los cambios pasan
    /// por SceneTransition (fundido a negro y pantalla de carga).
    /// </summary>
    public static class SceneLoader
    {
        public static void LoadLogin() => Load(GameManager.SCENE_LOGIN);
        public static void LoadMenu() => Load(GameManager.SCENE_MENU);
        public static void LoadMiniGame() => Load(GameManager.SCENE_MINIGAME);

        private static void Load(string scene)
        {
            var transition = SceneTransition.Instance;
            if (transition != null) transition.Load(scene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(scene); // al cerrar el juego
        }
    }
}

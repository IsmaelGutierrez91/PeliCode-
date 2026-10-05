using UnityEngine;

namespace PeliCode.Core
{
    /// <summary>
    /// Orquestador general del juego. Persiste entre escenas y centraliza
    /// el arranque de Unity Services y el acceso a datos de sesion.
    /// Colocalo en la escena Login dentro de un GameObject "GameManager".
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public const string SCENE_LOGIN = "Login";
        public const string SCENE_MENU = "Menu";
        public const string SCENE_MINIGAME = "MiniGame";

        [Header("Configuracion general")]
        [SerializeField] private int targetFrameRate = 60;

        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = targetFrameRate;
        }

        private async void Start()
        {
            // El GameManager es el unico responsable de inicializar Unity Services.
            // Todo lo demas (Auth, CloudSave, Leaderboards) espera a este evento.
            await ServicesInitializer.InitializeAsync();
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

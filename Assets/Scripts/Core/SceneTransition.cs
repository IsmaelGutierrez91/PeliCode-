using System.Collections;
using System.Linq;
using PeliCode.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PeliCode.Core
{
    /// <summary>
    /// Cambio de escena con fundido a negro y pantalla de carga:
    ///   escena actual -> negro -> pantalla de carga -> (carga la escena y espera a que este
    ///   lista) -> negro -> escena nueva.
    /// La pantalla de carga es un fondo negro con "CARGANDO" (los puntos van de 1 a 3) abajo a
    /// la izquierda y un cuadrado blanco que gira (placeholder para un logo).
    /// Se construye por codigo y persiste entre escenas, asi que no hay nada que armar en ellas.
    /// Al abrir el juego, la primera escena tambien aparece con un fundido desde negro.
    /// </summary>
    public class SceneTransition : Singleton<SceneTransition>
    {
        public const float FadeSeconds = 0.35f;
        private const float MinLoadingSeconds = 0.6f;
        private const float ReadyTimeoutSeconds = 15f;

        private static bool _transitioning;

        /// <summary>True mientras dura un cambio de escena (fundidos y carga incluidos).</summary>
        public static bool IsTransitioning => _transitioning;

        private CanvasGroup _fade;
        private GameObject _loadingPanel;
        private RectTransform _spinner;
        private TMP_Text _loadingText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FadeInFirstScene()
        {
            var transition = Instance;
            if (transition != null) transition.StartCoroutine(transition.FadeFromBlack());
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            Build();
        }

        public void Load(string sceneName)
        {
            if (_transitioning) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        /// <summary>"CARGANDO" con 1, 2 o 3 puntos segun el tiempo (cambia 3 veces por segundo).</summary>
        public static string LoadingLabel(string word, float seconds) =>
            word + new string('.', 1 + Mathf.FloorToInt(seconds * 3f) % 3);

        private IEnumerator LoadRoutine(string sceneName)
        {
            _transitioning = true;
            _fade.blocksRaycasts = true;

            yield return Fade(0f, 1f);            // la escena actual se va a negro
            _loadingPanel.SetActive(true);
            yield return Fade(1f, 0f);            // aparece la pantalla de carga
            float shownAt = Time.unscaledTime;

            var load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;

            // Espera a que la escena nueva este lista (o a que pase el tiempo maximo)
            float readySince = Time.unscaledTime;
            while (!SceneIsReady() && Time.unscaledTime - readySince < ReadyTimeoutSeconds)
                yield return null;
            while (Time.unscaledTime - shownAt < MinLoadingSeconds)
                yield return null;

            yield return Fade(0f, 1f);            // la pantalla de carga se va a negro
            _loadingPanel.SetActive(false);
            yield return Fade(1f, 0f);            // aparece la escena nueva

            _fade.blocksRaycasts = false;
            _transitioning = false;
        }

        private IEnumerator FadeFromBlack()
        {
            _fade.alpha = 1f;
            _fade.blocksRaycasts = true; // sin clics a ciegas mientras la pantalla esta negra
            yield return null; // un cuadro para que la escena termine de armarse
            yield return Fade(1f, 0f);
            if (!_transitioning) _fade.blocksRaycasts = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                _fade.alpha = Mathf.Lerp(from, to, t / FadeSeconds);
                yield return null;
            }
            _fade.alpha = to;
        }

        private static bool SceneIsReady() =>
            FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<ISceneReadiness>()
                .All(s => s.IsSceneReady);

        private void Update()
        {
            if (_loadingPanel == null || !_loadingPanel.activeSelf) return;
            _spinner.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
            _loadingText.text = LoadingLabel(Loc.Get("loading.text"), Time.unscaledTime);
        }

        // ---------------------------------------------------------------- construccion

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // por encima de todo
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            _loadingPanel = FullScreen("PantallaDeCarga", transform, Color.black);

            var spinner = new GameObject("Cuadrado", typeof(RectTransform), typeof(Image));
            _spinner = (RectTransform)spinner.transform;
            _spinner.SetParent(_loadingPanel.transform, false);
            _spinner.anchorMin = _spinner.anchorMax = Vector2.zero;
            _spinner.anchoredPosition = new Vector2(70f, 64f);
            _spinner.sizeDelta = new Vector2(40f, 40f);
            spinner.GetComponent<Image>().color = Color.white;

            var label = new GameObject("TextoCargando", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(_loadingPanel.transform, false);
            labelRect.anchorMin = labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(110f, 64f);
            labelRect.sizeDelta = new Vector2(500f, 60f);
            _loadingText = label.GetComponent<TextMeshProUGUI>();
            _loadingText.fontSize = 32f;
            _loadingText.color = Color.white;
            _loadingText.alignment = TextAlignmentOptions.MidlineLeft;
            _loadingPanel.SetActive(false);

            var fade = FullScreen("Fundido", transform, Color.black);
            _fade = fade.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _fade.interactable = false;
        }

        private static GameObject FullScreen(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }
    }
}

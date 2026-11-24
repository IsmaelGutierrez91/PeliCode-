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
    /// la izquierda, junto al logo (Resources/PeliCode_Logo), que late mientras carga.
    /// Se construye por codigo y persiste entre escenas, asi que no hay nada que armar en ellas.
    /// Al abrir el juego, la primera escena tambien aparece con un fundido desde negro.
    /// </summary>
    public class SceneTransition : Singleton<SceneTransition>
    {
        public const float FadeSeconds = 0.35f;
        private const float MinLoadingSeconds = 0.6f;
        private const float ReadyTimeoutSeconds = 15f;

        public const string LogoResource = "PeliCode_Logo";
        private const float LogoSize = 72f;
        private const float LogoPulse = 0.08f; // el logo crece y se achica un 8 %, una vez por segundo

        private static bool _transitioning;
        private static bool _firstFading;
        private Coroutine _firstFade;

        /// <summary>True mientras dura un cambio de escena (fundidos y carga incluidos) o la entrada a la primera.</summary>
        public static bool IsTransitioning => _transitioning || _firstFading;

        private CanvasGroup _fade;
        private GameObject _loadingPanel;
        private RectTransform _logo;
        private TMP_Text _loadingText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FadeInFirstScene()
        {
            var transition = Instance;
            if (transition != null) transition._firstFade = transition.StartCoroutine(transition.FadeFromBlack());
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
            if (_firstFade != null)
            {
                // La primera escena aun esperaba (por ejemplo el login comprobando la sesion):
                // se sigue desde la pantalla de carga que ya se ve, sin volver a mostrarla.
                StopCoroutine(_firstFade);
                _firstFade = null;
                _firstFading = false;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        /// <summary>"CARGANDO" con 1, 2 o 3 puntos segun el tiempo (cambia 3 veces por segundo).</summary>
        public static string LoadingLabel(string word, float seconds) =>
            word + new string('.', 1 + Mathf.FloorToInt(seconds * 3f) % 3);

        private IEnumerator LoadRoutine(string sceneName)
        {
            _transitioning = true;
            _fade.blocksRaycasts = true;

            if (_loadingPanel.activeSelf)
            {
                _fade.alpha = 0f;                 // la pantalla de carga ya se ve
            }
            else
            {
                yield return Fade(0f, 1f);        // la escena actual se va a negro
                _loadingPanel.SetActive(true);
                yield return Fade(1f, 0f);        // aparece la pantalla de carga
            }
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
            _firstFading = true;
            _fade.alpha = 1f;
            _fade.blocksRaycasts = true; // sin clics a ciegas mientras la pantalla esta negra
            yield return null; // un cuadro para que la escena termine de armarse

            if (!SceneIsReady())
            {
                // La escena aun se prepara (el login mira si hay sesion guardada): pantalla de carga
                // en vez de dejar ver la escena a medias.
                _loadingPanel.SetActive(true);
                yield return Fade(1f, 0f);
                float since = Time.unscaledTime;
                while (!SceneIsReady() && Time.unscaledTime - since < ReadyTimeoutSeconds)
                    yield return null;
                yield return Fade(0f, 1f);
                _loadingPanel.SetActive(false);
            }

            yield return Fade(1f, 0f);
            _firstFading = false;
            _firstFade = null;
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
            _logo.localScale = Vector3.one * LogoScale(Time.unscaledTime);
            _loadingText.text = LoadingLabel(Loc.Get("loading.text"), Time.unscaledTime);
        }

        /// <summary>Escala del logo en la pantalla de carga: un latido suave de un segundo.</summary>
        public static float LogoScale(float seconds) =>
            1f + LogoPulse * 0.5f * (1f - Mathf.Cos(seconds * 2f * Mathf.PI));

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

            var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            _logo = (RectTransform)logo.transform;
            _logo.SetParent(_loadingPanel.transform, false);
            _logo.anchorMin = _logo.anchorMax = Vector2.zero;
            _logo.anchoredPosition = new Vector2(80f, 64f);
            _logo.sizeDelta = new Vector2(LogoSize, LogoSize);
            var logoImage = logo.GetComponent<Image>();
            logoImage.sprite = Resources.Load<Sprite>(LogoResource); // sin el sprite queda un cuadrado blanco
            logoImage.preserveAspect = true;

            var label = new GameObject("TextoCargando", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(_loadingPanel.transform, false);
            labelRect.anchorMin = labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(132f, 64f);
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

using DG.Tweening;
using PeliCode.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Controla el deslizamiento entre los 5 paneles del Menu (Anuncios, Usuario,
    /// Minijuegos, Ranked, Opciones) usando DOTween sobre un RectTransform "Container"
    /// que agrupa los 5 paneles en COLUMNA vertical (uno debajo del otro).
    ///
    /// Setup en el editor:
    ///  - "Container" (RectTransform) contiene los 5 paneles hijos, apilados
    ///    verticalmente en orden Anuncios(0) -> Usuario(1) -> Minijuegos(2) ->
    ///    Ranked(3) -> Opciones(4). La separacion entre paneles puede ser cualquiera:
    ///    el desplazamiento se calcula con la posicion real de cada panel.
    ///  - Los 5 botones de navegacion llaman a GoToPanel(indice).
    ///  - El panel por defecto al iniciar la escena es Minijuegos (indice 2).
    /// </summary>
    public class MenuPanelController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RectTransform container;

        [Header("Botones de navegacion (orden: Anuncios, Usuario, Minijuegos, Ranked, Opciones)")]
        [SerializeField] private Button[] navigationButtons = new Button[5];

        [Header("Configuracion de desplazamiento")]
        [SerializeField] private float tweenDuration = 0.45f;
        [SerializeField] private Ease tweenEase = Ease.InOutCubic;

        // Indice por defecto: 2 = Minijuegos, segun el documento de diseno.
        [SerializeField] private int defaultPanelIndex = 2;

        private int _currentIndex;
        private Tween _activeTween;

        public int CurrentIndex => _currentIndex;

        private void Awake()
        {
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                int capturedIndex = i; // evitar captura por referencia en el loop
                navigationButtons[i].onClick.AddListener(() => GoToPanel(capturedIndex));
            }
        }

        private void Start()
        {
            _currentIndex = Mathf.Clamp(defaultPanelIndex, 0, container.childCount - 1);
            var pos = container.anchoredPosition;
            pos.y = TargetY(_currentIndex);
            container.anchoredPosition = pos;
            HighlightNavigation();
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }

        public void GoToPanel(int index)
        {
            if (index == _currentIndex || index < 0 || index >= container.childCount) return;

            _currentIndex = index;
            HighlightNavigation();

            _activeTween?.Kill();
            _activeTween = container
                .DOAnchorPosY(TargetY(index), tweenDuration)
                .SetEase(tweenEase);
        }

        // El boton del panel visible queda marcado.
        private void HighlightNavigation()
        {
            for (int i = 0; i < navigationButtons.Length; i++)
                UITheme.SetSelected(navigationButtons[i], i == _currentIndex);
        }

        /// <summary>
        /// Posicion de Container que deja el panel "index" donde esta el panel 0 al inicio.
        /// Antes se calculaba como index * (alto de pantalla + separacion), que no coincidia
        /// con la separacion real de los paneles en la escena (1100) y los dejaba corridos
        /// hasta 120 px; ademas cambiaba con la relacion de aspecto de la pantalla.
        /// </summary>
        private float TargetY(int index)
        {
            var first = (RectTransform)container.GetChild(0);
            var target = (RectTransform)container.GetChild(index);
            return first.anchoredPosition.y - target.anchoredPosition.y;
        }
    }
}

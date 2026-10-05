using DG.Tweening;
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
    ///    Ranked(3) -> Opciones(4), separados por "panelSpacing" pixeles.
    ///  - Los 5 botones de navegacion llaman a GoToPanel(indice).
    ///  - El panel por defecto al iniciar la escena es Minijuegos (indice 2).
    /// </summary>
    public class MenuPanelController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RectTransform container;
        [SerializeField] private RectTransform viewport; // area visible (alto de referencia)

        [Header("Botones de navegacion (orden: Anuncios, Usuario, Minijuegos, Ranked, Opciones)")]
        [SerializeField] private Button[] navigationButtons = new Button[5];

        [Header("Configuracion de desplazamiento")]
        [SerializeField] private float panelSpacing = 50f;
        [SerializeField] private float tweenDuration = 0.45f;
        [SerializeField] private Ease tweenEase = Ease.InOutCubic;

        // Indice por defecto: 2 = Minijuegos, segun el documento de diseno.
        [SerializeField] private int defaultPanelIndex = 2;

        private int _currentIndex;
        private Tween _activeTween;

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
            _currentIndex = defaultPanelIndex;
            SnapToPanelImmediate(_currentIndex);
        }

        public void GoToPanel(int index)
        {
            if (index == _currentIndex) return;

            _currentIndex = index;
            // Layout vertical: el panel 0 (Anuncios) esta en Y=0 dentro de Container,
            // y los siguientes van hacia abajo. En Unity UI, "abajo" es Y negativo,
            // por lo que Container se mueve hacia arriba (Y positivo) para traer al
            // frente un panel que esta mas abajo en el layout.
            float targetY = index * (viewport.rect.height + panelSpacing);

            _activeTween?.Kill();
            _activeTween = container
                .DOAnchorPosY(targetY, tweenDuration)
                .SetEase(tweenEase);
        }

        private void SnapToPanelImmediate(int index)
        {
            float targetY = index * (viewport.rect.height + panelSpacing);
            var pos = container.anchoredPosition;
            pos.y = targetY;
            container.anchoredPosition = pos;
        }
    }
}

using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    public class DragDropBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        public int BlockIndex { get; private set; }

        /// <summary>Donde estaba el bloque antes de empezar a arrastrarlo (la lista o una casilla).</summary>
        public Transform ParentBeforeDrag { get; private set; }

        private Transform _originalParent;
        private bool _draggable = true;
        private bool _dragging;

        public void Initialize(int blockIndex, string text, Transform originalParent)
        {
            BlockIndex = blockIndex;
            label.text = CodeText.Format(text);
            _originalParent = originalParent;
        }

        /// <summary>Pinta el bloque (verde si esta bien puesto, rojo si no) con el texto oscuro para que se lea.</summary>
        public void Mark(Color background)
        {
            var image = GetComponent<Image>();
            if (image != null) image.color = background;
            label.color = UITheme.TextOnAccent;
        }

        public void SetDraggable(bool value)
        {
            _draggable = value;
            // Si se corta el arrastre a mitad (se acabo el tiempo), el bloque vuelve a su lugar.
            if (!value && _dragging) EndDrag();
        }

        /// <summary>Devuelve el bloque a la lista de bloques sin usar.</summary>
        public void ReturnToOrigin()
        {
            transform.SetParent(_originalParent, false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_draggable) return;
            _dragging = true;
            ParentBeforeDrag = transform.parent;
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(transform.root);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            EndDrag();
        }

        private void EndDrag()
        {
            _dragging = false;
            canvasGroup.blocksRaycasts = true;

            // Soltado fuera de una casilla: vuelve a la lista de bloques.
            if (transform.parent == transform.root)
            {
                ReturnToOrigin();
            }
        }
    }
}

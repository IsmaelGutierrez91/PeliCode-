using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PeliCode.MiniGame.Questions
{
    public class DragDropBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        public int BlockIndex { get; private set; }
        private Transform _originalParent;
        private bool _draggable = true;

        public void Initialize(int blockIndex, string text, Transform originalParent)
        {
            BlockIndex = blockIndex;
            label.text = text;
            _originalParent = originalParent;
        }

        public void SetDraggable(bool value) => _draggable = value;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_draggable) return;
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(transform.root);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_draggable) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_draggable) return;
            canvasGroup.blocksRaycasts = true;

            if (transform.parent == transform.root)
            {
                transform.SetParent(_originalParent);
            }
        }
    }
}

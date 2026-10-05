using UnityEngine;
using UnityEngine.EventSystems;

namespace PeliCode.MiniGame.Questions
{
    public class DragDropSlot : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var droppedBlock = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<DragDropBlock>()
                : null;

            if (droppedBlock == null) return;

            if (transform.childCount > 0)
            {
                var existing = transform.GetChild(0);
                existing.SetParent(transform.root);
            }

            droppedBlock.transform.SetParent(transform);
            droppedBlock.transform.localPosition = Vector3.zero;
        }
    }
}

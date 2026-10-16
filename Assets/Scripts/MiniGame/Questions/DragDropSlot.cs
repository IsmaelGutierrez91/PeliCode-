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

            // Si la casilla ya tenia un bloque: se intercambia con la casilla de donde vino
            // el bloque soltado, o vuelve a la lista de bloques. Antes quedaba suelto en el
            // canvas y seguia en pantalla en la pregunta siguiente.
            var existing = transform.childCount > 0
                ? transform.GetChild(0).GetComponent<DragDropBlock>()
                : null;

            if (existing != null && existing != droppedBlock)
            {
                Transform previous = droppedBlock.ParentBeforeDrag;
                if (previous != null && previous != transform && previous.GetComponent<DragDropSlot>() != null)
                {
                    existing.transform.SetParent(previous, false);
                    existing.transform.localPosition = Vector3.zero;
                }
                else
                {
                    existing.ReturnToOrigin();
                }
            }

            droppedBlock.transform.SetParent(transform);
            droppedBlock.transform.localPosition = Vector3.zero;
        }
    }
}

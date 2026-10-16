using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PeliCode.UI
{
    /// <summary>
    /// Teclado en un formulario: Tab / Shift+Tab pasan de un campo a otro y Enter en el
    /// ultimo campo pulsa el boton de enviar (Enter en los demas salta al siguiente).
    /// </summary>
    public class FormNavigator : MonoBehaviour
    {
        [SerializeField] private TMP_InputField[] fields;
        [SerializeField] private Button submitButton;
        [SerializeField] private bool focusFirstOnEnable = true;

        private void Awake()
        {
            for (int i = 0; i < fields.Length; i++)
            {
                int index = i;
                fields[i].onSubmit.AddListener(_ => OnSubmit(index));
            }
        }

        private void OnEnable()
        {
            if (focusFirstOnEnable && fields.Length > 0) Focus(0);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

            int current = CurrentIndex();
            if (current < 0) return;
            // Por si el campo alcanzo a escribir el tabulador
            if (fields[current].text.Contains("\t"))
                fields[current].text = fields[current].text.Replace("\t", string.Empty);

            bool back = keyboard.shiftKey.isPressed;
            Focus((current + (back ? fields.Length - 1 : 1)) % fields.Length);
        }

        private void OnSubmit(int index)
        {
            // TMP tambien manda onSubmit al perder el foco con Enter; solo cuenta la tecla.
            var keyboard = Keyboard.current;
            bool enter = keyboard == null || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (!enter) return;

            if (index < fields.Length - 1)
            {
                Focus(index + 1);
            }
            else if (submitButton != null && submitButton.interactable && submitButton.gameObject.activeInHierarchy)
            {
                submitButton.onClick.Invoke();
            }
        }

        private int CurrentIndex()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < fields.Length; i++)
                if (fields[i] != null && (fields[i].isFocused || fields[i].gameObject == selected)) return i;
            return -1;
        }

        private void Focus(int index)
        {
            var field = fields[index];
            if (field == null || !field.interactable) return;
            field.Select();
            field.ActivateInputField();
        }
    }
}

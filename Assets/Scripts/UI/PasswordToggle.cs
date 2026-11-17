using PeliCode.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.UI
{
    /// <summary>
    /// Boton "Mostrar / Ocultar" dentro de un campo de contrasena. Se arma por codigo al borde
    /// derecho del campo (y deja libre ese espacio para el texto), asi sirve en cualquier
    /// formulario con solo llamar a <see cref="Attach"/>.
    /// </summary>
    public class PasswordToggle : MonoBehaviour
    {
        private const float ButtonWidth = 150f;
        private const float Gap = 8f;

        private TMP_InputField _input;
        private TMP_Text _label;
        private bool _visible;

        public bool Visible => _visible;

        public static PasswordToggle Attach(TMP_InputField input)
        {
            var existing = input.GetComponentInChildren<PasswordToggle>(true);
            if (existing != null) return existing;

            var go = new GameObject("BotonVerContrasena", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PasswordToggle));
            var rect = (RectTransform)go.transform;
            rect.SetParent(input.transform, false);
            // Pegado al borde derecho del campo, con casi todo su alto
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(ButtonWidth, -2f * Gap);
            rect.anchoredPosition = new Vector2(-Gap, 0f);

            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None }; // Tab sigue pasando entre campos
            UITheme.Paint(button, UITheme.SurfaceRaised);

            var labelGo = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = input.textComponent.font;
            label.fontSize = Mathf.Max(14f, input.textComponent.fontSize * 0.75f);
            label.color = UITheme.Text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            // El texto escrito no debe quedar debajo del boton
            if (input.textViewport != null)
            {
                var viewport = input.textViewport;
                viewport.offsetMax = new Vector2(-(ButtonWidth + 2f * Gap), viewport.offsetMax.y);
            }

            var toggle = go.GetComponent<PasswordToggle>();
            toggle._input = input;
            toggle._label = label;
            button.onClick.AddListener(toggle.Toggle);
            toggle.Apply(false);
            return toggle;
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Apply(false); // al volver a abrir el formulario, la contrasena vuelve a ocultarse
        }

        private void OnDisable() => Loc.LanguageChanged -= Refresh;

        private void Toggle() => Apply(!_visible);

        /// <summary>Muestra u oculta lo escrito.</summary>
        public void Apply(bool visible)
        {
            _visible = visible;
            if (_input != null)
            {
                _input.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
                _input.ForceLabelUpdate();
            }
            Refresh();
        }

        private void Refresh()
        {
            if (_label != null) _label.text = Loc.Get(_visible ? "password.hide" : "password.show");
        }
    }
}

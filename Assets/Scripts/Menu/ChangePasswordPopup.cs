using System.Threading.Tasks;
using PeliCode.Authentication;
using PeliCode.Localization;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Ventana para cambiar la contrasena (contrasena actual, nueva y repetida). Se arma por
    /// codigo encima de todo el canvas: <see cref="Show"/> y listo. Es opcional: el jugador la
    /// abre desde Opciones solo si quiere cambiarla.
    /// </summary>
    public class ChangePasswordPopup : MonoBehaviour
    {
        private TMP_InputField _current, _new, _repeat;
        private Button _save, _cancel;
        private TMP_Text _feedback;
        private bool _busy;

        /// <summary>Abre la ventana sobre el canvas que contiene a <paramref name="anchor"/>.</summary>
        public static ChangePasswordPopup Show(Transform anchor)
        {
            var canvas = anchor.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : anchor;

            var root = new GameObject("CambiarContrasena", typeof(RectTransform), typeof(Image), typeof(ChangePasswordPopup));
            var popup = root.GetComponent<ChangePasswordPopup>();
            popup.Build((RectTransform)root.transform, parent);
            return popup;
        }

        private void Build(RectTransform root, Transform parent)
        {
            root.SetParent(parent, false);
            Stretch(root);
            root.SetAsLastSibling();
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f); // oscurece y bloquea lo de atras

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 760f);
            panel.GetComponent<Image>().color = UITheme.Surface;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = Label(panelRect, Loc.Get("changePw.title"), 40f, FontStyles.Bold, 70f);
            title.alignment = TextAlignmentOptions.Center;

            _current = Field(panelRect, "changePw.current");
            _new = Field(panelRect, "changePw.new");
            _repeat = Field(panelRect, "changePw.confirm");

            _feedback = Label(panelRect, string.Empty, 26f, FontStyles.Normal, 90f);
            _feedback.alignment = TextAlignmentOptions.Center;
            _feedback.color = UITheme.Error;

            var row = new GameObject("Botones", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(panelRect, false);
            row.GetComponent<LayoutElement>().preferredHeight = 90f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 30f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            _cancel = MakeButton(rowRect, Loc.Get("changePw.cancel"), UITheme.SurfaceRaised, UITheme.Text);
            _save = MakeButton(rowRect, Loc.Get("changePw.save"), UITheme.Accent, UITheme.TextOnAccent);
            _cancel.onClick.AddListener(Close);
            _save.onClick.AddListener(Save);
            _repeat.onSubmit.AddListener(_ => Save());

            _current.Select();
            _current.ActivateInputField();
        }

        private async void Save()
        {
            if (_busy) return;
            SetBusy(true);
            SetFeedback(Loc.Get("changePw.saving"), UITheme.TextMuted);

            var (ok, error) = await AuthManager.Instance.ChangePasswordAsync(_current.text, _new.text, _repeat.text);
            if (this == null) return;

            if (ok)
            {
                SetFeedback(Loc.Get("changePw.done"), UITheme.Success);
                await Task.Delay(900);
                if (this != null) Close();
                return;
            }
            SetFeedback(error, UITheme.Error);
            SetBusy(false);
        }

        private void Close() => Destroy(gameObject);

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _save.interactable = _cancel.interactable = !busy;
            _current.interactable = _new.interactable = _repeat.interactable = !busy;
        }

        private void SetFeedback(string text, Color color)
        {
            _feedback.text = CodeText.Plain(text);
            _feedback.color = color;
        }

        // ---------------------------------------------------------------- construccion

        private static TMP_Text Label(Transform parent, string text, float size, FontStyles style, float height)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = UITheme.Text;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_InputField Field(Transform parent, string placeholderKey)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "Campo_" + placeholderKey;
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 84f;

            var input = go.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.Password;
            input.characterLimit = PasswordValidator.MaxPasswordLength;
            input.textComponent.fontSize = 30f;
            input.textComponent.color = UITheme.Text;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = Loc.Get(placeholderKey);
                placeholder.fontSize = 30f;
                placeholder.fontStyle = FontStyles.Normal;
                placeholder.color = UITheme.TextMuted;
            }
            go.GetComponent<Image>().color = UITheme.CodeBackground;
            PasswordToggle.Attach(input);
            return input;
        }

        private static Button MakeButton(Transform parent, string text, Color background, Color textColor)
        {
            var go = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            UITheme.Paint(button, background);

            var label = Label(go.transform, text, 32f, FontStyles.Normal, 0f);
            Destroy(label.GetComponent<LayoutElement>());
            Stretch((RectTransform)label.transform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

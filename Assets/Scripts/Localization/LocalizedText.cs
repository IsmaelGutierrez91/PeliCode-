using TMPro;
using UnityEngine;

namespace PeliCode.Localization
{
    /// <summary>
    /// Pone en el TMP_Text de este GameObject el texto de "key" en el idioma actual,
    /// y lo actualiza si el jugador cambia de idioma.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_text == null) _text = GetComponent<TMP_Text>();
            _text.text = Loc.Get(key);
        }
    }
}

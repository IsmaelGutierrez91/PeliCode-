using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Localization
{
    /// <summary>
    /// Boton que cambia entre espanol e ingles. Muestra el idioma al que se cambia
    /// ("English" mientras el juego esta en espanol, y al reves), como es habitual,
    /// para que quien no entiende el idioma actual lo reconozca.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LanguageToggleButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Toggle);
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

        private void Toggle()
        {
            Loc.SetLanguage(Loc.Current == Language.Spanish ? Language.English : Language.Spanish);
        }

        private void Refresh()
        {
            if (label != null)
                label.text = Loc.NativeName(Loc.Current == Language.Spanish ? Language.English : Language.Spanish);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Localization;
using TMPro;
using Unity.Services.CloudSave;
using UnityEngine;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Anuncios: muestra un texto de actualizaciones guardado como
    /// Custom Data en Cloud Save (clave "announcements"), editable desde el
    /// Dashboard sin necesidad de subir una nueva build.
    /// El valor puede ser un texto (se muestra igual en los dos idiomas) o un objeto
    /// {"es": "...", "en": "..."}; si falta el del idioma actual se muestra el otro.
    /// </summary>
    public class AnnouncementsPanelUI : MonoBehaviour
    {
        private const string ANNOUNCEMENTS_KEY = "announcements";

        [SerializeField] private TMP_Text announcementsText;

        private bool _loading;
        private AnnouncementTexts _announcement; // null = no hay anuncio en el servidor

        [System.Serializable]
        private class AnnouncementTexts
        {
            public string es;
            public string en;

            public string For(Language language)
            {
                string wanted = language == Language.English ? en : es;
                return !string.IsNullOrWhiteSpace(wanted) ? wanted : (language == Language.English ? es : en);
            }
        }

        private async void OnEnable()
        {
            Loc.LanguageChanged += Show;
            _loading = true;
            Show();
            _announcement = await LoadAnnouncementsAsync();
            _loading = false;
            if (this != null) Show();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= Show;
        }

        private void Show()
        {
            if (announcementsText == null) return;
            announcementsText.text = _loading ? Loc.Get("announcements.loading")
                : !string.IsNullOrWhiteSpace(_announcement?.For(Loc.Current)) ? _announcement.For(Loc.Current)
                : Loc.Get("announcements.none");
        }

        private async Task<AnnouncementTexts> LoadAnnouncementsAsync()
        {
            try
            {
                var query = new HashSet<string> { ANNOUNCEMENTS_KEY };
                var result = await CloudSaveService.Instance.Data.Custom.LoadAsync(ANNOUNCEMENTS_KEY, query);

                if (result.TryGetValue(ANNOUNCEMENTS_KEY, out var item))
                {
                    // Texto plano (como antes) u objeto con los dos idiomas
                    try
                    {
                        string text = item.Value.GetAs<string>();
                        return new AnnouncementTexts { es = text, en = text };
                    }
                    catch (System.Exception)
                    {
                        return item.Value.GetAs<AnnouncementTexts>();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnnouncementsPanelUI] {e.Message}");
            }

            return null;
        }
    }
}

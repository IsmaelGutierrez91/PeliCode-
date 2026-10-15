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
    /// El anuncio viene del servidor en un solo idioma; "Cargando..." y el texto por
    /// defecto si cambian con el idioma del juego.
    /// </summary>
    public class AnnouncementsPanelUI : MonoBehaviour
    {
        private const string ANNOUNCEMENTS_KEY = "announcements";

        [SerializeField] private TMP_Text announcementsText;

        private bool _loading;
        private string _announcement; // null = no hay anuncio en el servidor

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
                : !string.IsNullOrWhiteSpace(_announcement) ? _announcement
                : Loc.Get("announcements.none");
        }

        private async Task<string> LoadAnnouncementsAsync()
        {
            try
            {
                var query = new HashSet<string> { ANNOUNCEMENTS_KEY };
                var result = await CloudSaveService.Instance.Data.Custom.LoadAsync(ANNOUNCEMENTS_KEY, query);

                if (result.TryGetValue(ANNOUNCEMENTS_KEY, out var item))
                {
                    return item.Value.GetAs<string>();
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

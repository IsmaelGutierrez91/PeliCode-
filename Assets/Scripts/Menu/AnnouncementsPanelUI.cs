using System.Collections.Generic;
using System.Threading.Tasks;
using PeliCode.Core;
using TMPro;
using Unity.Services.CloudSave;
using UnityEngine;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Anuncios: muestra un texto de actualizaciones guardado como
    /// Custom Data en Cloud Save (clave "announcements"), editable desde el
    /// Dashboard sin necesidad de subir una nueva build.
    /// </summary>
    public class AnnouncementsPanelUI : MonoBehaviour
    {
        private const string ANNOUNCEMENTS_KEY = "announcements";

        [SerializeField] private TMP_Text announcementsText;

        private async void OnEnable()
        {
            announcementsText.text = "Cargando...";
            announcementsText.text = await LoadAnnouncementsAsync();
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

            return "Sin novedades por el momento.";
        }
    }
}

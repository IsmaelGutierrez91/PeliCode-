using System;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.CloudServices;
using PeliCode.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Ranked: lista deslizable (ScrollRect) con el Top 25 global y
    /// el boton para iniciar una partida ranked. Mientras carga, si no hay puntajes
    /// o si falla la conexion, lo dice en statusText.
    /// </summary>
    public class RankedPanelUI : MonoBehaviour
    {
        [SerializeField] private Transform contentParent; // "Content" del ScrollRect
        [SerializeField] private RankedEntryUI entryPrefab;
        [SerializeField] private Button startRankedButton;
        [SerializeField] private TMP_Text statusText;

        private bool _refreshing;
        private string _statusKey;

        private void Awake()
        {
            startRankedButton.onClick.AddListener(StartRanked);
        }

        private async void OnEnable()
        {
            Loc.LanguageChanged += ShowStatus;
            await RefreshLeaderboard();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= ShowStatus;
        }

        public async Task RefreshLeaderboard()
        {
            if (_refreshing) return;
            _refreshing = true;
            try
            {
                foreach (Transform child in contentParent)
                    Destroy(child.gameObject);
                SetStatus("ranked.loading");

                var topScores = await LeaderboardManager.Instance.GetTopScoresAsync();
                if (this == null) return; // se cambio de escena mientras cargaba

                foreach (var entry in topScores)
                {
                    var row = Instantiate(entryPrefab, contentParent);
                    row.SetData(entry.rank, entry.playerName, entry.score, entry.isMe);
                }
                SetStatus(topScores.Count == 0 ? "ranked.empty" : null);
            }
            catch (Exception e)
            {
                // Sin conexion o sin leaderboard creado en el Dashboard: se avisa, y el resto
                // del menu sigue funcionando.
                Debug.LogError($"[RankedPanelUI] No se pudo cargar el ranking: {e.Message}");
                if (this != null) SetStatus("ranked.loadFailed");
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void SetStatus(string key)
        {
            _statusKey = key;
            ShowStatus();
        }

        private void ShowStatus()
        {
            if (statusText == null) return;
            statusText.gameObject.SetActive(_statusKey != null);
            if (_statusKey != null) statusText.text = Loc.Get(_statusKey);
        }

        private void StartRanked()
        {
            MiniGameSessionData.SetRankedSession();
            SceneLoader.LoadMiniGame();
        }
    }
}

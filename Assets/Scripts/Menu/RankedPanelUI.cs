using System;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.CloudServices;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Ranked: lista deslizable (ScrollRect) con el Top 25 global y
    /// el boton para iniciar una partida ranked.
    /// </summary>
    public class RankedPanelUI : MonoBehaviour
    {
        [SerializeField] private Transform contentParent; // "Content" del ScrollRect
        [SerializeField] private RankedEntryUI entryPrefab;
        [SerializeField] private Button startRankedButton;

        private bool _refreshing;

        private void Awake()
        {
            startRankedButton.onClick.AddListener(StartRanked);
        }

        private async void OnEnable()
        {
            await RefreshLeaderboard();
        }

        public async Task RefreshLeaderboard()
        {
            if (_refreshing) return;
            _refreshing = true;
            try
            {
                foreach (Transform child in contentParent)
                    Destroy(child.gameObject);

                var topScores = await LeaderboardManager.Instance.GetTopScoresAsync();
                if (this == null) return; // se cambio de escena mientras cargaba

                foreach (var entry in topScores)
                {
                    var row = Instantiate(entryPrefab, contentParent);
                    row.SetData(entry.playerName, entry.score);
                }
            }
            catch (Exception e)
            {
                // Sin conexion o sin leaderboard creado en el Dashboard: la lista queda vacia,
                // pero el resto del menu sigue funcionando.
                Debug.LogError($"[RankedPanelUI] No se pudo cargar el ranking: {e.Message}");
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void StartRanked()
        {
            MiniGameSessionData.SetRankedSession();
            SceneLoader.LoadMiniGame();
        }
    }
}

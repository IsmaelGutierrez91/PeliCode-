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

        private void Awake()
        {
            startRankedButton.onClick.AddListener(StartRanked);
        }

        private async void OnEnable()
        {
            await RefreshLeaderboard();
        }

        public async System.Threading.Tasks.Task RefreshLeaderboard()
        {
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            var topScores = await LeaderboardManager.Instance.GetTopScoresAsync();

            foreach (var entry in topScores)
            {
                var row = Instantiate(entryPrefab, contentParent);
                row.SetData(entry.playerName, entry.score);
            }
        }

        private void StartRanked()
        {
            MiniGameSessionData.SetRankedSession();
            SceneLoader.LoadMiniGame();
        }
    }
}

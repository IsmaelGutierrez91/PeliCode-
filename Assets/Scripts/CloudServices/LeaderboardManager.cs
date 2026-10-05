using System.Collections.Generic;
using System.Threading.Tasks;
using PeliCode.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace PeliCode.CloudServices
{
    /// <summary>
    /// Envuelve el servicio de Unity Leaderboards para el modo Ranked.
    /// Requiere crear en el Dashboard un Leaderboard con ID "ranked_top_score"
    /// (SortOrder: Descending, UpdateType: KeepBest).
    /// </summary>
    public class LeaderboardManager : Singleton<LeaderboardManager>
    {
        private const string LEADERBOARD_ID = "ranked_top_score";
        private const int TOP_COUNT = 25;

        /// <summary>
        /// Envia el puntaje del jugador. Como el leaderboard esta configurado como
        /// KeepBest, Unity descarta automaticamente el envio si es menor al guardado.
        /// </summary>
        public async Task SubmitScoreAsync(double score)
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LEADERBOARD_ID, score);
        }

        /// <summary>
        /// Devuelve el Top 25 global (nombre + puntaje) para el panel de Ranked.
        /// </summary>
        public async Task<List<LeaderboardEntryDto>> GetTopScoresAsync()
        {
            var options = new GetScoresOptions { Offset = 0, Limit = TOP_COUNT };
            LeaderboardScoresPage page = await LeaderboardsService.Instance
                .GetScoresAsync(LEADERBOARD_ID, options);

            var result = new List<LeaderboardEntryDto>();
            foreach (var entry in page.Results)
            {
                result.Add(new LeaderboardEntryDto
                {
                    playerName = string.IsNullOrEmpty(entry.PlayerName) ? "Jugador" : entry.PlayerName,
                    score = (int)entry.Score,
                    rank = entry.Rank + 1
                });
            }
            return result;
        }
    }

    public struct LeaderboardEntryDto
    {
        public string playerName;
        public int score;
        public int rank;
    }
}

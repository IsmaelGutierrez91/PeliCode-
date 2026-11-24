using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

namespace PeliCode.CloudServices
{
    /// <summary>
    /// Encapsula Unity Cloud Save (Player Data) del jugador. Claves usadas:
    ///  - "player_profile": nombre, top ranked, rachas y estadisticas por minijuego.
    ///  - "game_history": lista de partidas jugadas (GameRecord), que no se reinicia.
    /// Las dos se crean solas al guardar por primera vez: no hay que crearlas en el Dashboard.
    /// Las reglas (rachas, reseteo semanal) viven en PeliCode.Data.ProfileRules.
    /// </summary>
    public class CloudSaveManager : Singleton<CloudSaveManager>
    {
        private const string PROFILE_KEY = "player_profile";
        private const string HISTORY_KEY = "game_history";

        // Las partidas se agregan una tras otra: dos agregados a la vez (salir y terminar otra
        // partida enseguida) leerian la misma lista y uno borraria al otro.
        private Task _lastHistoryWrite = Task.CompletedTask;

        public async Task SaveProfileAsync(PlayerProfileData profile)
        {
            var data = new Dictionary<string, object> { { PROFILE_KEY, profile } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }

        /// <summary>Agrega una partida al final del historial del jugador.</summary>
        public Task AppendGameRecordAsync(GameRecord record)
        {
            _lastHistoryWrite = AppendAfterAsync(_lastHistoryWrite, record);
            return _lastHistoryWrite;
        }

        private async Task AppendAfterAsync(Task previous, GameRecord record)
        {
            try { await previous; }
            catch (Exception) { /* el error del anterior ya lo recibio quien lo pidio */ }

            var query = new HashSet<string> { HISTORY_KEY };
            Dictionary<string, Item> result = await CloudSaveService.Instance.Data.Player.LoadAsync(query);
            var history = result.TryGetValue(HISTORY_KEY, out var item)
                ? item.Value.GetAs<List<GameRecord>>() ?? new List<GameRecord>()
                : new List<GameRecord>();

            history.Add(record);
            var data = new Dictionary<string, object> { { HISTORY_KEY, history } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }

        /// <summary>
        /// Devuelve el perfil guardado, o null si el jugador todavia no tiene uno
        /// (por ejemplo, si el registro creo la cuenta pero fallo al guardar el perfil).
        /// </summary>
        public async Task<PlayerProfileData> LoadProfileAsync()
        {
            var query = new HashSet<string> { PROFILE_KEY };
            Dictionary<string, Item> result = await CloudSaveService.Instance.Data.Player.LoadAsync(query);

            if (result.TryGetValue(PROFILE_KEY, out var item))
            {
                var profile = item.Value.GetAs<PlayerProfileData>();
                profile.minigameStats ??= new Dictionary<string, MiniGameStat>();
                return profile;
            }
            return null;
        }

        /// <summary>
        /// Fecha y hora del jugador. Los dias de la racha y las semanas del reseteo se cuentan
        /// con la hora local: con UTC, en Peru (UTC-5) el dia cambiaba a las 19:00 y jugar a las
        /// 18:00 y a las 20:00 del mismo dia contaba como dos dias seguidos.
        /// </summary>
        public static DateTime PlayerNow => DateTime.Now;

        /// <summary>
        /// Calcula la racha de dias consecutivos de inicio de sesion.
        /// Llamar justo despues de un SignIn exitoso.
        /// </summary>
        public PlayerProfileData UpdateLoginStreak(PlayerProfileData profile)
        {
            ProfileRules.UpdateLoginStreak(profile, PlayerNow);
            return profile;
        }

        public void ResetWeeklyStatsIfNeeded(PlayerProfileData profile)
        {
            ProfileRules.ResetWeeklyStatsIfNeeded(profile, PlayerNow);
        }

        public void RegisterNormalGameResult(
            PlayerProfileData profile,
            MiniGameCategory category,
            Difficulty difficulty,
            int correctAnswers,
            int totalQuestions)
        {
            ProfileRules.RegisterNormalGameResult(
                profile, category, difficulty, correctAnswers, totalQuestions, PlayerNow);
        }
    }
}

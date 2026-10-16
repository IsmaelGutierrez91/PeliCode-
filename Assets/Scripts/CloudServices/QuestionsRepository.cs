using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using UnityEngine;

namespace PeliCode.CloudServices
{
    /// <summary>
    /// Lee el banco de preguntas desde Unity Cloud Save - Custom Data (datos no
    /// ligados a un jugador especifico, administrables desde el Dashboard o via
    /// Cloud Code por el equipo de contenido).
    ///
    /// Convencion de "Custom ID" en Cloud Save:
    ///   "questions_{Categoria}_{Dificultad}"  ej: "questions_Condicionales_Facil"
    ///   cada entrada guarda un QuestionSet (lista de QuestionData) en JSON.
    ///
    /// Se cachea en memoria solo lo que se cargo bien: si la red falla, la proxima
    /// partida vuelve a intentarlo en vez de quedarse sin preguntas toda la sesion.
    ///
    /// Banco local: Resources/Questions/questions_{Categoria}_{Dificultad}.json, con el
    /// mismo formato que el valor en Cloud Save. Se usa cuando Cloud Save falla o no tiene
    /// preguntas para esa categoria y dificultad (y siempre que se abre el minijuego en el
    /// editor sin iniciar sesion), asi el juego se puede probar sin el Dashboard.
    /// Las preguntas de Cloud Save, si existen, tienen prioridad.
    /// </summary>
    public class QuestionsRepository : Singleton<QuestionsRepository>
    {
        private readonly Dictionary<string, List<QuestionData>> _cache = new Dictionary<string, List<QuestionData>>();

        public const string LocalBankFolder = "Questions";

        /// <summary>True si la ultima carga fallo por un error (red, servicio) y tampoco habia banco local.</summary>
        public bool LastLoadFailed { get; private set; }

        public static string BuildCustomIdFor(MiniGameCategory category, Difficulty difficulty)
            => BuildCustomId(category, difficulty);

        private static string BuildCustomId(MiniGameCategory category, Difficulty difficulty)
            => $"questions_{category}_{difficulty}";

        /// <summary>
        /// Carga (con cache) todas las preguntas de una categoria+dificultad.
        /// </summary>
        public async Task<List<QuestionData>> GetQuestionsAsync(MiniGameCategory category, Difficulty difficulty)
        {
            LastLoadFailed = false;
            var (questions, failed) = await LoadAsync(category, difficulty);
            LastLoadFailed = failed;
            return questions;
        }

        /// <summary>
        /// Carga TODAS las preguntas (todas las categorias y dificultades), usado
        /// por el modo Ranked, que mezcla todo. Las 15 cargas van en paralelo.
        /// </summary>
        public async Task<List<QuestionData>> GetAllQuestionsAsync()
        {
            var loads = new List<Task<(List<QuestionData> questions, bool failed)>>();
            foreach (MiniGameCategory category in Enum.GetValues(typeof(MiniGameCategory)))
            {
                foreach (Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
                {
                    loads.Add(LoadAsync(category, difficulty));
                }
            }

            var results = await Task.WhenAll(loads);
            LastLoadFailed = results.Any(r => r.failed);
            return results.SelectMany(r => r.questions).ToList();
        }

        private async Task<(List<QuestionData> questions, bool failed)> LoadAsync(
            MiniGameCategory category, Difficulty difficulty)
        {
            string customId = BuildCustomId(category, difficulty);

            if (_cache.TryGetValue(customId, out var cached))
                return (cached, false);

            bool cloudFailed = false;
            try
            {
                var query = new HashSet<string> { customId };
                var result = await CloudSaveService.Instance.Data.Custom.LoadAsync(customId, query);

                var questions = result.TryGetValue(customId, out var item)
                    ? item.Value.GetAs<QuestionSet>()?.questions ?? new List<QuestionData>()
                    : new List<QuestionData>();

                questions.RemoveAll(q => q == null);
                if (questions.Count > 0)
                {
                    _cache[customId] = questions;
                    return (questions, false);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QuestionsRepository] No se pudo leer '{customId}' de Cloud Save: {e.Message}");
                cloudFailed = true;
            }

            var local = LoadLocalBank(category, difficulty);
            if (local.Count > 0)
            {
                Debug.Log($"[QuestionsRepository] '{customId}': se usan las {local.Count} preguntas del banco local.");
                if (!cloudFailed) _cache[customId] = local; // si fallo la red, la proxima vez se reintenta Cloud Save
                return (local, false);
            }
            return (new List<QuestionData>(), cloudFailed);
        }

        /// <summary>Lee el banco local de una categoria y dificultad (vacio si no hay archivo).</summary>
        public static List<QuestionData> LoadLocalBank(MiniGameCategory category, Difficulty difficulty)
        {
            string customId = BuildCustomId(category, difficulty);
            var asset = Resources.Load<TextAsset>($"{LocalBankFolder}/{customId}");
            if (asset == null) return new List<QuestionData>();

            try
            {
                var set = JsonConvert.DeserializeObject<QuestionSet>(asset.text);
                var questions = set?.questions ?? new List<QuestionData>();
                questions.RemoveAll(q => q == null);
                return questions;
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestionsRepository] El banco local '{customId}' no se pudo leer: {e.Message}");
                return new List<QuestionData>();
            }
        }

        public void ClearCache() => _cache.Clear();
    }
}

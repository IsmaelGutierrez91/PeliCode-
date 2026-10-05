using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PeliCode.Core;
using PeliCode.Data;
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
    /// Se cachea en memoria tras la primera carga de cada escena de minijuego.
    /// </summary>
    public class QuestionsRepository : Singleton<QuestionsRepository>
    {
        private readonly Dictionary<string, List<QuestionData>> _cache = new Dictionary<string, List<QuestionData>>();

        private static string BuildCustomId(MiniGameCategory category, Difficulty difficulty)
            => $"questions_{category}_{difficulty}";

        /// <summary>
        /// Carga (con cache) todas las preguntas de una categoria+dificultad.
        /// </summary>
        public async Task<List<QuestionData>> GetQuestionsAsync(MiniGameCategory category, Difficulty difficulty)
        {
            string customId = BuildCustomId(category, difficulty);

            if (_cache.TryGetValue(customId, out var cached))
                return cached;

            try
            {
                var query = new HashSet<string> { customId };
                var result = await CloudSaveService.Instance.Data.Custom.LoadAsync(customId, query);

                if (result.TryGetValue(customId, out var item))
                {
                    var set = item.Value.GetAs<QuestionSet>();
                    _cache[customId] = set.questions;
                    return set.questions;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuestionsRepository] Error cargando '{customId}': {e.Message}");
            }

            _cache[customId] = new List<QuestionData>();
            return _cache[customId];
        }

        /// <summary>
        /// Carga TODAS las preguntas (todas las categorias y dificultades), usado
        /// por el modo Ranked, que mezcla todo.
        /// </summary>
        public async Task<List<QuestionData>> GetAllQuestionsAsync()
        {
            var all = new List<QuestionData>();
            foreach (MiniGameCategory category in Enum.GetValues(typeof(MiniGameCategory)))
            {
                foreach (Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
                {
                    all.AddRange(await GetQuestionsAsync(category, difficulty));
                }
            }
            return all;
        }

        public void ClearCache() => _cache.Clear();
    }
}

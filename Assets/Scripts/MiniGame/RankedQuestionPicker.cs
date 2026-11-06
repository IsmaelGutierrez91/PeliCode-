using System;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;

namespace PeliCode.MiniGame
{
    /// <summary>
    /// Elige la siguiente pregunta del modo ranked: la dificultad sube con las respuestas
    /// correctas. Al empezar salen sobre todo faciles; hacia las <see cref="RampLength"/>
    /// aciertos salen sobre todo dificiles. Ninguna dificultad llega a probabilidad cero.
    /// Dentro de una dificultad no se repite una pregunta hasta haber visto todas las demas.
    /// </summary>
    public class RankedQuestionPicker
    {
        /// <summary>Aciertos a los que se llega al reparto final.</summary>
        public const int RampLength = 20;

        // Probabilidad (Facil, Medio, Dificil) al empezar y al final de la rampa. Cada fila suma 1.
        private static readonly float[] Start = { 0.70f, 0.25f, 0.05f };
        private static readonly float[] End = { 0.10f, 0.35f, 0.55f };

        private readonly Random _random;
        private readonly Dictionary<Difficulty, List<QuestionData>> _all = new Dictionary<Difficulty, List<QuestionData>>();
        private readonly Dictionary<Difficulty, Queue<QuestionData>> _queues = new Dictionary<Difficulty, Queue<QuestionData>>();

        public int Count { get; }

        public RankedQuestionPicker(IEnumerable<QuestionData> questions, Random random = null)
        {
            _random = random ?? new Random();
            foreach (var group in questions.Where(q => q != null).GroupBy(q => q.difficulty))
            {
                _all[group.Key] = group.ToList();
                _queues[group.Key] = new Queue<QuestionData>();
                Count += _all[group.Key].Count;
            }
        }

        /// <summary>Probabilidad de cada dificultad (0 si no hay preguntas de esa dificultad), normalizada a 1.</summary>
        public static float[] Weights(int correctAnswers, Func<Difficulty, bool> available = null)
        {
            float t = Math.Min(1f, Math.Max(0f, correctAnswers / (float)RampLength));
            var weights = new float[3];
            float sum = 0f;
            foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
            {
                int i = (int)d;
                weights[i] = available != null && !available(d) ? 0f : Start[i] + (End[i] - Start[i]) * t;
                sum += weights[i];
            }
            if (sum > 0f)
                for (int i = 0; i < weights.Length; i++) weights[i] /= sum;
            return weights;
        }

        /// <summary>La siguiente pregunta, o null si no hay ninguna.</summary>
        public QuestionData Next(int correctAnswers)
        {
            if (Count == 0) return null;

            var weights = Weights(correctAnswers, d => _all.ContainsKey(d));
            float roll = (float)_random.NextDouble();
            Difficulty chosen = _all.Keys.First();
            float acc = 0f;
            foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
            {
                if (weights[(int)d] <= 0f) continue;
                chosen = d;
                acc += weights[(int)d];
                if (roll < acc) break;
            }

            var queue = _queues[chosen];
            if (queue.Count == 0)
            {
                // Se acabo el mazo de esa dificultad: se baraja de nuevo
                foreach (var q in _all[chosen].OrderBy(_ => _random.Next())) queue.Enqueue(q);
            }
            return queue.Dequeue();
        }
    }
}

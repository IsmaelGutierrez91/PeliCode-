using System;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;
using PeliCode.Localization;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Arma las opciones de una pregunta de seleccion multiple:
    ///   1. Se eligen hasta (optionsCount - 1) incorrectas al azar del pool, sin repetir
    ///      y sin la respuesta correcta.
    ///   2. Se suma la correcta.
    ///   3. Se baraja el ORDEN para que la correcta no quede siempre en la misma posicion.
    /// Si el pool trae menos incorrectas de las necesarias, devuelve menos opciones
    /// (la vista oculta los botones que sobran).
    /// </summary>
    public static class MultipleChoiceOptions
    {
        public static List<string> Build(QuestionData question, int optionsCount, Random rng, out int correctIndex,
            Language language = Language.Spanish)
        {
            var (correctAnswer, pool) = question.ChoicesFor(language);
            string correct = correctAnswer?.Trim() ?? string.Empty;

            List<string> incorrect = (pool ?? new List<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Where(a => !string.Equals(a, correct, StringComparison.Ordinal))
                .Distinct()
                .ToList();

            Shuffle(incorrect, rng);

            var options = new List<string> { correct };
            options.AddRange(incorrect.Take(Math.Max(0, optionsCount - 1)));
            Shuffle(options, rng);

            correctIndex = options.IndexOf(correct);
            return options;
        }

        // Fisher-Yates
        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

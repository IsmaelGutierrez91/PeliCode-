using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PeliCode.CloudServices;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.MiniGame.Questions;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PeliCode.Tests
{
    /// <summary>
    /// El banco de preguntas de ejemplo (Resources/Questions): que exista para cada categoria y
    /// dificultad, que cada pregunta se pueda jugar y que traiga el ingles.
    /// </summary>
    public class QuestionBankTests
    {
        private static IEnumerable<(MiniGameCategory, Difficulty)> AllSets()
        {
            foreach (MiniGameCategory c in Enum.GetValues(typeof(MiniGameCategory)))
                foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
                    yield return (c, d);
        }

        // Lo que pide cada partida normal (MiniGameManager): 5 en Facil, 10 en Medio y 15 en Dificil
        [Test]
        public void EveryCategoryHasEnoughQuestionsForAFullGame()
        {
            foreach (var (c, d) in AllSets())
            {
                int needed = d == Difficulty.Facil ? 5 : d == Difficulty.Medio ? 10 : 15;
                Assert.GreaterOrEqual(QuestionsRepository.LoadLocalBank(c, d).Count, needed, $"{c} {d}");
            }
        }

        [Test]
        public void EveryQuestionIsPlayableAndMatchesItsFile()
        {
            var problems = new List<string>();
            foreach (var (c, d) in AllSets())
            {
                foreach (var q in QuestionsRepository.LoadLocalBank(c, d))
                {
                    if (!QuestionChecks.IsPlayable(q, out string problem)) problems.Add($"{q.id}: {problem}");
                    if (q.category != c || q.difficulty != d) problems.Add($"{q.id}: esta en el archivo de {c} {d}");
                }
            }
            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void IdsAreUnique()
        {
            var ids = AllSets().SelectMany(s => QuestionsRepository.LoadLocalBank(s.Item1, s.Item2)).Select(q => q.id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void EveryQuestionHasEnglishText()
        {
            var missing = AllSets().SelectMany(s => QuestionsRepository.LoadLocalBank(s.Item1, s.Item2))
                .Where(q => string.IsNullOrWhiteSpace(q.questionTextEn)).Select(q => q.id).ToList();
            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void MultipleChoiceHasThreeWrongAnswersAndAnUnambiguousDragOrder()
        {
            var problems = new List<string>();
            foreach (var q in AllSets().SelectMany(s => QuestionsRepository.LoadLocalBank(s.Item1, s.Item2)))
            {
                if (q.type == QuestionType.SeleccionMultiple)
                {
                    foreach (Language lang in Enum.GetValues(typeof(Language)))
                    {
                        var options = MultipleChoiceOptions.Build(q, 4, new System.Random(1), out int correct, lang);
                        if (options.Count != 4) problems.Add($"{q.id} ({lang}): {options.Count} opciones");
                    }
                }
                if (q.type == QuestionType.ArrastrarCodigo)
                {
                    if (q.codeBlocks.Distinct().Count() != q.codeBlocks.Count) problems.Add($"{q.id}: bloques repetidos");
                    if (!q.correctOrder.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, q.codeBlocks.Count)))
                        problems.Add($"{q.id}: correctOrder no usa cada bloque una vez");
                }
            }
            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void EnglishChoicesAreUsedOnlyWhenComplete()
        {
            var q = new QuestionData
            {
                correctAnswer = "Nada", incorrectAnswerPool = new List<string> { "1", "2", "3" },
                correctAnswerEn = "Nothing", incorrectAnswerPoolEn = new List<string>()
            };
            // El ingles esta incompleto (sin incorrectas): se usa todo en espanol, sin mezclar
            Assert.AreEqual("Nada", q.ChoicesFor(Language.English).correct);
            q.incorrectAnswerPoolEn = new List<string> { "1", "2", "3" };
            Assert.AreEqual("Nothing", q.ChoicesFor(Language.English).correct);
            Assert.AreEqual("Nada", q.ChoicesFor(Language.Spanish).correct);
        }
    }

    /// <summary>
    /// Una partida de verdad, en modo juego: la escena MiniGame abierta sin iniciar sesion
    /// carga el banco local y muestra la primera pregunta.
    /// </summary>
    public class MiniGameSmokeTest
    {
        [UnityTest]
        public IEnumerator MiniGameShowsALocalQuestionWithoutLogin()
        {
            yield return new EnterPlayMode();
            // Sin Unity Services inicializado, Cloud Save falla y se usa el banco local (es un aviso, no un error)
            MiniGameSessionData.SetNormalSession(MiniGameCategory.Condicionales, Difficulty.Facil);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MiniGame.unity",
                new LoadSceneParameters(LoadSceneMode.Single));

            GameObject active = null;
            for (int frame = 0; frame < 600 && active == null; frame++)
            {
                yield return null;
                foreach (var view in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (view is IQuestionView && view.gameObject.activeInHierarchy) active = view.gameObject;
            }

            Assert.IsNotNull(active, "No se mostro ninguna pregunta");
            var progress = GameObject.Find("Text_ProgressText").GetComponent<TMP_Text>().text;
            Assert.AreEqual("1 / 5", progress);
            yield return new ExitPlayMode();
        }
    }
}

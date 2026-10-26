using System;
using System.Linq;
using NUnit.Framework;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.MiniGame;
using PeliCode.MiniGame.Questions;

namespace PeliCode.Tests
{
    /// <summary>Arreglos de la revision: respuestas, limites de las vistas y mensajes al fallar.</summary>
    public class RevisionTests
    {
        [TestCase("int", "int", true)]
        [TestCase("  int ", "int", true)]
        [TestCase("Int", "int", false)]      // C# distingue mayusculas
        [TestCase("WHILE", "while", false)]
        [TestCase("else  if", "else if", true)] // varios espacios cuentan como uno
        [TestCase("elseif", "else if", false)]
        [TestCase("", "int", false)]
        [TestCase("int", "", false)]
        public void CompleteCodeAnswers(string typed, string expected, bool ok)
        {
            Assert.AreEqual(ok, AnswerMatcher.Matches(typed, expected));
        }

        [Test]
        public void EveryBankAnswerStillMatchesItself()
        {
            // Con la comparacion exacta, ninguna respuesta del banco puede dejar de coincidir consigo misma
            foreach (MiniGameCategory c in Enum.GetValues(typeof(MiniGameCategory)))
                foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
                    foreach (var q in CloudServices.QuestionsRepository.LoadLocalBank(c, d)
                                 .Where(q => q.type == QuestionType.CompletarCodigo))
                        Assert.IsTrue(AnswerMatcher.Matches(q.expectedAnswer, q.expectedAnswer), q.id);
        }

        private static QuestionData FindError(int snippets, int buggy) => new QuestionData
        {
            type = QuestionType.DetectarError,
            codeSnippetsOptions = Enumerable.Range(0, snippets).Select(i => $"x{i};").ToList(),
            buggyIndex = buggy
        };

        [Test]
        public void FindErrorAllowsUpToFiveSnippets()
        {
            Assert.IsTrue(QuestionChecks.IsPlayable(FindError(QuestionChecks.MaxErrorSnippets, 4), out _));
            // Con 6, el sexto no tendria boton: se descarta aunque el error no sea el sexto
            Assert.IsFalse(QuestionChecks.IsPlayable(FindError(QuestionChecks.MaxErrorSnippets + 1, 0), out _));
        }

        [Test]
        public void MistakeMessageShowsTheAnswerAndTheExplanation()
        {
            Loc.SetLanguage(Language.Spanish);
            string text = MiniGameManager.MistakeFeedback(false, "int", "Porque es un entero.");
            StringAssert.StartsWith(Loc.Get("game.incorrect"), text);
            StringAssert.Contains("int", text);
            StringAssert.EndsWith("Porque es un entero.</noparse>", text);

            Assert.AreEqual(Loc.Get("game.timeUp"), MiniGameManager.MistakeFeedback(true, null, null));
        }

        [Test]
        public void ExplanationUsesTheEnglishVersionWhenThereIsOne()
        {
            var q = new QuestionData { explanation = "Porque si.", explanationEn = "Because." };
            Assert.AreEqual("Because.", q.ExplanationFor(Language.English));
            Assert.AreEqual("Porque si.", q.ExplanationFor(Language.Spanish));
            q.explanationEn = null;
            Assert.AreEqual("Porque si.", q.ExplanationFor(Language.English));
        }

        [Test]
        public void StreakDaysFollowThePlayersClock()
        {
            // 18:00 y 20:00 del mismo dia local son el mismo dia (con UTC-5 en UTC eran dos)
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            p.lastLoginDateUtc = "2026-10-20";
            p.currentStreak = 3;
            ProfileRules.UpdateLoginStreak(p, new DateTime(2026, 10, 20, 20, 0, 0, DateTimeKind.Local));
            Assert.AreEqual(3, p.currentStreak);
            Assert.AreEqual(DateTimeKind.Local, CloudServices.CloudSaveManager.PlayerNow.Kind);
        }
    }
}

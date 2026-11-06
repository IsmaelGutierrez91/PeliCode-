using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PeliCode.CloudServices;
using PeliCode.Data;
using PeliCode.MiniGame;

namespace PeliCode.Tests
{
    /// <summary>El modo ranked sube la dificultad con los aciertos, sin dejar ninguna en cero.</summary>
    public class RankedPickerTests
    {
        [TestCase(0)]
        [TestCase(5)]
        [TestCase(12)]
        [TestCase(20)]
        [TestCase(500)]
        public void WeightsSumToOneAndNeverReachZero(int correct)
        {
            var w = RankedQuestionPicker.Weights(correct);
            Assert.AreEqual(1f, w.Sum(), 0.0001f);
            Assert.That(w, Has.All.GreaterThan(0f));
        }

        [Test]
        public void EasyFadesAndHardGrowsAsThePlayerAdvances()
        {
            var start = RankedQuestionPicker.Weights(0);
            var mid = RankedQuestionPicker.Weights(RankedQuestionPicker.RampLength / 2);
            var end = RankedQuestionPicker.Weights(RankedQuestionPicker.RampLength);
            int e = (int)Difficulty.Facil, h = (int)Difficulty.Dificil;

            Assert.Greater(start[e], mid[e]);
            Assert.Greater(mid[e], end[e]);
            Assert.Less(start[h], mid[h]);
            Assert.Less(mid[h], end[h]);
            Assert.Greater(start[e], start[h]);  // al empezar dominan las faciles
            Assert.Greater(end[h], end[e]);      // al final dominan las dificiles
            CollectionAssert.AreEqual(end, RankedQuestionPicker.Weights(100)); // la rampa se detiene
        }

        [Test]
        public void MissingDifficultyIsSkippedAndTheRestStillSumToOne()
        {
            var w = RankedQuestionPicker.Weights(3, d => d != Difficulty.Medio);
            Assert.AreEqual(0f, w[(int)Difficulty.Medio]);
            Assert.AreEqual(1f, w.Sum(), 0.0001f);
        }

        private static List<QuestionData> Bank()
        {
            var all = new List<QuestionData>();
            foreach (MiniGameCategory c in Enum.GetValues(typeof(MiniGameCategory)))
                foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
                    all.AddRange(QuestionsRepository.LoadLocalBank(c, d));
            return all;
        }

        private static double Share(RankedQuestionPicker picker, int correct, Difficulty d, int draws = 3000)
        {
            int hits = 0;
            for (int i = 0; i < draws; i++)
                if (picker.Next(correct).difficulty == d) hits++;
            return hits / (double)draws;
        }

        [Test]
        public void EarlyQuestionsAreMostlyEasyAndLateOnesMostlyHard()
        {
            var picker = new RankedQuestionPicker(Bank(), new System.Random(7));
            Assert.Greater(Share(picker, 0, Difficulty.Facil), 0.55);
            Assert.Greater(Share(picker, 30, Difficulty.Dificil), 0.40);
            // Nunca desaparecen del todo
            Assert.Greater(Share(picker, 30, Difficulty.Facil), 0.03);
            Assert.Greater(Share(picker, 0, Difficulty.Dificil), 0.01);
        }

        [Test]
        public void ADifficultyDoesNotRepeatUntilItsDeckIsUsedUp()
        {
            var bank = Bank();
            var picker = new RankedQuestionPicker(bank, new System.Random(3));
            var seen = new HashSet<string>();
            int hardCount = bank.Count(q => q.difficulty == Difficulty.Dificil);
            int hardDrawn = 0;
            while (hardDrawn < hardCount)
            {
                var q = picker.Next(100);
                if (q.difficulty != Difficulty.Dificil) continue;
                Assert.IsTrue(seen.Add(q.id), $"{q.id} se repitio antes de ver todas las dificiles");
                hardDrawn++;
            }
        }

        [Test]
        public void EmptyPoolGivesNull()
        {
            Assert.IsNull(new RankedQuestionPicker(new List<QuestionData>()).Next(0));
        }

        [Test]
        public void SingleDifficultyBankStillWorks()
        {
            var only = new List<QuestionData> { new QuestionData { id = "a", difficulty = Difficulty.Medio } };
            var picker = new RankedQuestionPicker(only, new System.Random(1));
            Assert.AreEqual("a", picker.Next(0).id);
            Assert.AreEqual("a", picker.Next(50).id);
        }
    }
}

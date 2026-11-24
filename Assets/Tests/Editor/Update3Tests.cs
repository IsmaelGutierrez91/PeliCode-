using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PeliCode.CloudServices;
using PeliCode.Core;
using PeliCode.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PeliCode.Tests
{
    /// <summary>Ajustes del tercer documento de Ismael: porcentaje acumulado, historial, textos y logo.</summary>
    public class Update3Tests
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 24, 18, 0, 0);

        private static MiniGameStat Stat(PlayerProfileData p) => p.minigameStats[MiniGameCategory.Condicionales.ToString()];

        // El ejemplo del documento: una partida perfecta y otra con 0 % se ven como 50 %
        [Test]
        public void ThePercentageCombinesTheGamesOfTheWeek()
        {
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Facil, 5, 5, Today);
            Assert.AreEqual(100f, Stat(p).easyPercentage, 0.001f);

            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Facil, 0, 5, Today);
            Assert.AreEqual(50f, Stat(p).easyPercentage, 0.001f);
            Assert.AreEqual(2, Stat(p).easyGames);
            Assert.AreEqual(5, Stat(p).easyCorrect);
            Assert.AreEqual(10, Stat(p).easyAnswered);

            // Las otras dificultades no se mezclan
            Assert.AreEqual(0f, Stat(p).mediumPercentage);
            Assert.AreEqual(0, Stat(p).mediumGames);
        }

        [Test]
        public void TheNewWeekStartsTheCountFromZero()
        {
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Dificil, 15, 15, Today);

            // Martes 24/11 y sabado 28/11 son semanas distintas
            DateTime saturday = new DateTime(2026, 11, 28, 0, 5, 0);
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Dificil, 3, 15, saturday);
            Assert.AreEqual(20f, Stat(p).hardPercentage, 0.001f);
            Assert.AreEqual(1, Stat(p).hardGames);
            Assert.AreEqual(15, Stat(p).hardAnswered);
        }

        [Test]
        public void TheWeeklyResetClearsTheCounters()
        {
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Medio, 7, 10, Today);

            Assert.IsTrue(ProfileRules.ResetWeeklyStatsIfNeeded(p, Today.AddDays(7)));
            Assert.AreEqual(0f, Stat(p).mediumPercentage);
            Assert.AreEqual(0, Stat(p).mediumGames);
            Assert.AreEqual(0, Stat(p).mediumCorrect);
            Assert.AreEqual(0, Stat(p).mediumAnswered);
        }

        [Test]
        public void ANormalGameRecordKeepsWhatWasPlayed()
        {
            var r = GameRecord.Normal(MiniGameCategory.SwitchEstados, Difficulty.Medio, 7, 9, 10, 83.6f, false, Today);
            Assert.AreEqual(GameRecord.NormalMode, r.mode);
            Assert.AreEqual("SwitchEstados", r.category);
            Assert.AreEqual("Medio", r.difficulty);
            Assert.AreEqual(7, r.correct);
            Assert.AreEqual(9, r.answered);
            Assert.AreEqual(10, r.questions);
            Assert.AreEqual(84, r.seconds);
            Assert.IsFalse(r.completed);
            StringAssert.StartsWith("2026-11-24T18:00:00", r.date); // y despues la zona, ej. -05:00
        }

        [Test]
        public void ARankedRecordHasNoCategory()
        {
            var r = GameRecord.Ranked(12, 13, 240f, true, Today);
            Assert.AreEqual(GameRecord.RankedMode, r.mode);
            Assert.AreEqual(string.Empty, r.category);
            Assert.AreEqual(string.Empty, r.difficulty);
            Assert.AreEqual(13, r.questions);
            Assert.IsTrue(r.completed);
        }

        [TestCase("", 73, 0)]
        [TestCase("Hola", 73, 1)]
        [TestCase("abcde fghij", 5, 2)]   // la segunda palabra no cabe: otra fila
        [TestCase("abc de", 6, 1)]        // 6 justos
        [TestCase("uno\ndos", 73, 2)]     // el salto de linea empieza otra fila
        [TestCase("abcdefghijk", 5, 3)]   // una palabra mas larga que la fila se corta
        public void RowsCountsWrappedRows(string text, int rowChars, int rows)
        {
            Assert.AreEqual(rows, QuestionChecks.Rows(text, rowChars));
        }

        [Test]
        public void EveryBankQuestionFitsItsTextLimits()
        {
            var problems = new List<string>();
            foreach (MiniGameCategory c in Enum.GetValues(typeof(MiniGameCategory)))
                foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
                    foreach (var q in QuestionsRepository.LoadLocalBank(c, d))
                        if (!QuestionChecks.FitsTextLimits(q, out string problem))
                            problems.Add($"{q.id}: {problem}");
            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void TheLoadingScreenUsesTheLogo()
        {
            Assert.IsNotNull(Resources.Load<Sprite>(SceneTransition.LogoResource), "Falta el sprite Resources/PeliCode_Logo");
            Assert.AreEqual(1f, SceneTransition.LogoScale(0f), 0.0001f);
            Assert.AreEqual(1.08f, SceneTransition.LogoScale(0.5f), 0.0001f); // a mitad del latido, lo mas grande
            Assert.AreEqual(1f, SceneTransition.LogoScale(1f), 0.0001f);
        }

        [Test]
        public void TheLoginShowsTheAppNameInTwoColors()
        {
            // Se lee el YAML de la escena, sin abrirla (como en LocalizationTests)
            string login = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes", "Login.unity"));
            StringAssert.Contains("<color=#5499CF>Peli</color><color=#CBDDEF>Code#</color>", login);
        }

        [Test]
        public void TheLogoIsTheGameIcon()
        {
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.IsNotEmpty(icons);
            Assert.AreEqual("Assets/Resources/PeliCode_Logo.png", AssetDatabase.GetAssetPath(icons.First()));
        }
    }
}

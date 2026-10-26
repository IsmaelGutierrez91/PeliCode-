using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.Menu;

namespace PeliCode.Tests
{
    /// <summary>Ajustes pedidos por Ismael: perfil, arrastrar bloques y pantalla de carga.</summary>
    public class AjustesTests
    {
        [TestCase(0, false)]
        [TestCase(4, false)]
        [TestCase(5, true)]
        [TestCase(30, true)]
        public void StreakBadgeShowsFromFiveDays(int streak, bool shown)
        {
            Assert.AreEqual(shown, ProfileRules.ShowsStreakBadge(streak));
        }

        [Test]
        public void StatShowsTheNameAndABigCenteredPercentageBelow()
        {
            string text = ProfilePanelUI.FormatStat("Condicionales", 66.6f);
            StringAssert.StartsWith("Condicionales:\n", text);
            StringAssert.Contains("<align=center>", text);
            StringAssert.Contains("<b>67%</b>", text);
        }

        private static QuestionData Drag(int blocks) => new QuestionData
        {
            type = QuestionType.ArrastrarCodigo,
            codeBlocks = Enumerable.Range(0, blocks).Select(i => $"linea{i};").ToList(),
            correctOrder = Enumerable.Range(0, blocks).ToList()
        };

        [Test]
        public void DragQuestionsAllowUpToEightBlocks()
        {
            Assert.IsTrue(QuestionChecks.IsPlayable(Drag(QuestionChecks.MaxDragBlocks), out _));
            Assert.IsFalse(QuestionChecks.IsPlayable(Drag(QuestionChecks.MaxDragBlocks + 1), out string problem));
            StringAssert.Contains("8", problem);
        }

        [Test]
        public void LoadingDotsCycleFromOneToThree()
        {
            var labels = new List<string>();
            for (float t = 0f; t < 2f; t += 1f / 3f + 0.001f)
                labels.Add(SceneTransition.LoadingLabel("CARGANDO", t));
            CollectionAssert.IsSubsetOf(new[] { "CARGANDO.", "CARGANDO..", "CARGANDO..." }, labels);
            Assert.IsTrue(labels.All(l => l.Count(c => c == '.') >= 1 && l.Count(c => c == '.') <= 3));
        }
    }
}

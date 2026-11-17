using System;
using NUnit.Framework;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.MiniGame.Questions;
using PeliCode.UI;
using TMPro;
using UnityEngine;

namespace PeliCode.Tests
{
    /// <summary>Ajustes del segundo documento de Ismael: racha, semana, colores y contrasena.</summary>
    public class Update2Tests
    {
        [TestCase(0, false, false)]
        [TestCase(1, true, false)]
        [TestCase(2, true, false)]
        [TestCase(4, true, false)]
        [TestCase(5, true, true)]
        [TestCase(30, true, true)]
        public void StreakNumberShowsForAnyStreakButTheImageNeedsFiveDays(int streak, bool number, bool image)
        {
            Assert.AreEqual(number, ProfileRules.ShowsStreakNumber(streak));
            Assert.AreEqual(image, ProfileRules.ShowsStreakBadge(streak));
        }

        // Semana que empieza el sabado: viernes 6/11/2026 y sabado 7/11/2026 son semanas distintas
        [Test]
        public void TheWeekChangesOnSaturday()
        {
            string friday = ProfileRules.GetIsoWeekStamp(new DateTime(2026, 11, 6, 23, 59, 0));
            string saturday = ProfileRules.GetIsoWeekStamp(new DateTime(2026, 11, 7, 0, 0, 0));
            string nextFriday = ProfileRules.GetIsoWeekStamp(new DateTime(2026, 11, 13, 23, 59, 0));
            string nextSaturday = ProfileRules.GetIsoWeekStamp(new DateTime(2026, 11, 14, 0, 0, 0));

            Assert.AreNotEqual(friday, saturday);
            Assert.AreEqual(saturday, nextFriday);        // sabado a viernes es una sola semana
            Assert.AreNotEqual(nextFriday, nextSaturday);
            Assert.AreEqual(saturday, ProfileRules.GetIsoWeekStamp(new DateTime(2026, 11, 9))); // el lunes sigue en ella
        }

        [Test]
        public void TheWeekStampAlsoWorksAcrossTheNewYear()
        {
            // Viernes 1/1/2027 y sabado 2/1/2027: otra semana; el 2/1 y el 8/1 comparten
            Assert.AreNotEqual(ProfileRules.GetIsoWeekStamp(new DateTime(2027, 1, 1)), ProfileRules.GetIsoWeekStamp(new DateTime(2027, 1, 2)));
            Assert.AreEqual(ProfileRules.GetIsoWeekStamp(new DateTime(2027, 1, 2)), ProfileRules.GetIsoWeekStamp(new DateTime(2027, 1, 8)));
        }

        [Test]
        public void OldIsoStampsResetOnce()
        {
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            var stat = new MiniGameStat { easyPercentage = 90f, weekStamp = "2026-W45" }; // marca de antes de este cambio
            p.minigameStats[MiniGameCategory.Condicionales.ToString()] = stat;

            Assert.IsTrue(ProfileRules.ResetWeeklyStatsIfNeeded(p, new DateTime(2026, 11, 7)));
            Assert.AreEqual(0f, stat.easyPercentage);
            Assert.IsFalse(ProfileRules.ResetWeeklyStatsIfNeeded(p, new DateTime(2026, 11, 8)));
        }

        [Test]
        public void ThePaletteIsBuiltFromTheFourTones()
        {
            Assert.AreEqual(0x00, Mathf.RoundToInt(UITheme.Dark.r * 255));
            Assert.AreEqual(0x07, Mathf.RoundToInt(UITheme.Dark.g * 255));
            Assert.AreEqual(0x0D, Mathf.RoundToInt(UITheme.Dark.b * 255));
            Assert.AreEqual(UITheme.Dark, UITheme.Background);
            Assert.AreEqual(UITheme.Mid, UITheme.Surface);
            Assert.AreEqual(UITheme.Light, UITheme.Accent);
            Assert.AreEqual(UITheme.White, UITheme.Text);
            Assert.AreEqual("032D50", ColorUtility.ToHtmlStringRGB(UITheme.Mid));
            Assert.AreEqual("5499CF", ColorUtility.ToHtmlStringRGB(UITheme.Light));
            Assert.AreEqual("CBDDEF", ColorUtility.ToHtmlStringRGB(UITheme.White));
        }

        [Test]
        public void TextStaysReadableOnTheThemeSurfaces()
        {
            // Contraste minimo 4.5:1 (WCAG AA) del texto sobre cada superficie
            foreach (var surface in new[] { UITheme.Background, UITheme.Surface, UITheme.SurfaceRaised })
                Assert.GreaterOrEqual(Contrast(UITheme.Text, surface), 4.5f, ColorUtility.ToHtmlStringRGB(surface));
            Assert.GreaterOrEqual(Contrast(UITheme.TextMuted, UITheme.Background), 4.5f);
            Assert.GreaterOrEqual(Contrast(UITheme.TextOnAccent, UITheme.Accent), 4.5f);
            Assert.GreaterOrEqual(Contrast(UITheme.TextOnAccent, UITheme.Success), 4.5f);
            Assert.GreaterOrEqual(Contrast(UITheme.TextOnAccent, UITheme.Error), 4.5f);
        }

        private static float Luminance(Color c) =>
            0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;

        private static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        [Test]
        public void TheCorrectAnswerFillsTheBlankInGreen()
        {
            string text = CodeText.FormatFilled("int x = ____;\nreturn x;", "5", UITheme.Success);
            StringAssert.Contains("<color=#" + ColorUtility.ToHtmlStringRGB(UITheme.Success) + "><b><noparse>5</noparse></b></color>", text);
            StringAssert.Contains("<noparse>int x = </noparse>", text);
            StringAssert.Contains("<noparse>;\nreturn x;</noparse>", text);
            StringAssert.DoesNotContain("____", text);
        }

        [Test]
        public void AnAnswerThatLooksLikeATagCannotBreakTheText()
        {
            string text = CodeText.FormatFilled("if (a ____ b)", "<=", UITheme.Success);
            StringAssert.Contains("<noparse><=</noparse>", text);
        }

        [Test]
        public void WithoutABlankTheCodeIsShownAsItIs()
        {
            Assert.AreEqual(CodeText.Format("var a = 1;"), CodeText.FormatFilled("var a = 1;", "x", UITheme.Success));
            Assert.AreEqual(CodeText.Format("a ____ b"), CodeText.FormatFilled("a ____ b", null, UITheme.Success));
        }

        [Test]
        public void OrderingCodeAllowsAtLeastFourSecondsToStudyTheAnswer()
        {
            Assert.GreaterOrEqual(DragDropCodeView.StudySeconds, 4f);
            Assert.LessOrEqual(DragDropCodeView.StudySeconds, 5f);
            Assert.AreEqual(2f, DragDropCodeView.MarkSeconds);
            Assert.Greater(DragDropCodeView.MarkSeconds + DragDropCodeView.StudySeconds, IQuestionView.DefaultMistakeDelay);
        }
    }

    public class PasswordToggleTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        private TMP_InputField Input()
        {
            _go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            var input = _go.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.Password;
            return input;
        }

        [Test]
        public void TogglingShowsAndHidesTheText()
        {
            Loc.SetLanguage(Language.Spanish);
            var input = Input();
            var toggle = PasswordToggle.Attach(input);

            Assert.IsFalse(toggle.Visible);
            Assert.AreEqual(TMP_InputField.ContentType.Password, input.contentType);

            toggle.Apply(true);
            Assert.AreEqual(TMP_InputField.ContentType.Standard, input.contentType);
            Assert.AreEqual(Loc.Get("password.hide"), toggle.GetComponentInChildren<TMP_Text>().text);

            toggle.Apply(false);
            Assert.AreEqual(TMP_InputField.ContentType.Password, input.contentType);
            Assert.AreEqual(Loc.Get("password.show"), toggle.GetComponentInChildren<TMP_Text>().text);
        }

        [Test]
        public void AttachingTwiceKeepsASingleButton()
        {
            var input = Input();
            var first = PasswordToggle.Attach(input);
            var second = PasswordToggle.Attach(input);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, input.GetComponentsInChildren<PasswordToggle>(true).Length);
        }

        [Test]
        public void TheButtonLeavesRoomForTheTypedText()
        {
            var input = Input();
            float before = input.textViewport.offsetMax.x;
            PasswordToggle.Attach(input);
            Assert.Less(input.textViewport.offsetMax.x, before);
        }
    }
}

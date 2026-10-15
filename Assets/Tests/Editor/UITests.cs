using NUnit.Framework;
using PeliCode.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Tests
{
    public class CodeTextTests
    {
        [Test]
        public void CodeIsMonospacedAndNotParsedAsTags()
        {
            string formatted = CodeText.Format("List<b> x = new List<b>();");
            StringAssert.StartsWith("<mspace=", formatted);
            StringAssert.Contains("<noparse>List<b> x = new List<b>();</noparse>", formatted);
        }

        [Test]
        public void ClosingNoparseInsideTheCodeCannotEscape()
        {
            string formatted = CodeText.Plain("a</noparse><color=red>b");
            // Un solo cierre real: el del final
            Assert.AreEqual(formatted.Length - "</noparse>".Length, formatted.IndexOf("</noparse>", System.StringComparison.Ordinal));
        }

        [Test]
        public void EmptyStaysEmpty()
        {
            Assert.AreEqual(string.Empty, CodeText.Format(null));
            Assert.AreEqual(string.Empty, CodeText.Plain(""));
        }
    }

    public class UIThemeTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void PaintLightensOnHoverAndDarkensOnPress()
        {
            _go = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
            var button = _go.GetComponent<Button>();
            button.targetGraphic = _go.GetComponent<Image>();

            UITheme.Paint(button, UITheme.SurfaceRaised);

            Assert.AreEqual(Color.white, button.targetGraphic.color);
            Assert.AreEqual(UITheme.SurfaceRaised, button.colors.normalColor);
            Assert.Greater(button.colors.highlightedColor.grayscale, UITheme.SurfaceRaised.grayscale);
            Assert.Less(button.colors.pressedColor.grayscale, UITheme.SurfaceRaised.grayscale);
        }

        [Test]
        public void SelectedButtonUsesTheAccent()
        {
            _go = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
            var button = _go.GetComponent<Button>();
            button.targetGraphic = _go.GetComponent<Image>();

            UITheme.SetSelected(button, true);
            Assert.AreEqual(UITheme.Accent, button.colors.normalColor);
            UITheme.SetSelected(button, false);
            Assert.AreEqual(UITheme.SurfaceRaised, button.colors.normalColor);
        }
    }
}

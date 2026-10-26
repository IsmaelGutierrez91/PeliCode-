using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.UI
{
    /// <summary>
    /// Paleta del juego (tema oscuro, tipo editor de codigo). La usan los scripts que
    /// cambian colores en tiempo de juego (seleccion, aciertos y errores) para que
    /// coincidan con los de las escenas.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = Hex(0x0F172A);
        public static readonly Color Surface = Hex(0x1E293B);
        public static readonly Color SurfaceRaised = Hex(0x334155);
        public static readonly Color CodeBackground = Hex(0x0B1120);
        public static readonly Color Text = Hex(0xE2E8F0);
        public static readonly Color TextMuted = Hex(0x94A3B8);
        public static readonly Color TextOnAccent = Hex(0x0F172A);
        public static readonly Color Accent = Hex(0x38BDF8);
        public static readonly Color Success = Hex(0x4ADE80);
        public static readonly Color Error = Hex(0xF87171);
        public static readonly Color Warning = Hex(0xFBBF24);

        /// <summary>Marca un boton de un grupo (dificultad, panel del menu) como elegido o no.</summary>
        public static void SetSelected(Selectable button, bool selected)
        {
            if (button == null) return;
            Paint(button, selected ? Accent : SurfaceRaised);

            var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
                label.color = selected ? TextOnAccent : Text;
        }

        /// <summary>
        /// Pinta un boton (o cualquier Selectable) con un color base. El grafico queda blanco
        /// y los colores van en el ColorBlock: asi al pasar el mouse se aclara (el tinte de
        /// Unity multiplica, y sobre un color oscuro solo podria oscurecer).
        /// </summary>
        public static void Paint(Selectable selectable, Color baseColor)
        {
            if (selectable == null) return;
            if (selectable.targetGraphic != null) selectable.targetGraphic.color = Color.white;

            var colors = selectable.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.2f);
            colors.selectedColor = baseColor; // sin "quedarse marcado" despues del clic
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            selectable.colors = colors;
            selectable.transition = Selectable.Transition.ColorTint;
        }

        /// <summary>
        /// Marca un boton ya respondido (correcto en verde, error en rojo). A diferencia de Paint,
        /// el color se ve igual aunque el boton este deshabilitado, que es como queda al responder.
        /// </summary>
        public static void Highlight(Selectable selectable, Color color, Color? textColor = null)
        {
            if (selectable == null) return;
            Paint(selectable, color);
            var colors = selectable.colors;
            colors.disabledColor = color;
            colors.fadeDuration = 0f;
            selectable.colors = colors;
            if (textColor.HasValue)
            {
                var label = selectable.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label != null) label.color = textColor.Value;
            }
        }

        public static Color Hex(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}

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
        // Los cuatro tonos del diseño: oscuro, medio, claro y blanco. Todo el tema sale de ellos.
        public static readonly Color Dark = Hex(0x00070D);
        public static readonly Color Mid = Hex(0x032D50);
        public static readonly Color Light = Hex(0x5499CF);
        public static readonly Color White = Hex(0xCBDDEF);

        public static readonly Color Background = Dark;
        public static readonly Color Surface = Mid;
        public static readonly Color SurfaceRaised = Color.Lerp(Mid, Light, 0.35f); // botones sobre un panel medio
        public static readonly Color CodeBackground = Dark;
        public static readonly Color Text = White;
        public static readonly Color TextMuted = Light;
        public static readonly Color TextOnAccent = Dark;
        public static readonly Color Accent = Light;
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

using UnityEngine;

namespace PeliCode.UI
{
    /// <summary>
    /// Prepara un fragmento de codigo para mostrarlo en un TMP_Text:
    ///  - &lt;mspace&gt; lo dibuja con ancho fijo por caracter (como una fuente monoespaciada)
    ///    sin tener que agregar otra fuente al proyecto.
    ///  - &lt;noparse&gt; evita que TMP interprete como etiquetas cosas como List&lt;b&gt; o &lt;color&gt;
    ///    que aparecen en el codigo de las preguntas.
    /// </summary>
    public static class CodeText
    {
        public const string MonoSpacing = "0.6em";

        /// <summary>
        /// El codigo con el hueco "____" rellenado con la respuesta, en negrita y del color
        /// pedido (por ejemplo el verde de "correcto"). Sin hueco, se muestra el codigo igual.
        /// </summary>
        public static string FormatFilled(string template, string answer, Color color)
        {
            int gap = template == null ? -1 : template.IndexOf("____", System.StringComparison.Ordinal);
            if (gap < 0 || string.IsNullOrEmpty(answer)) return Format(template);

            string hex = ColorUtility.ToHtmlStringRGB(color);
            return Format(template.Substring(0, gap))
                   + $"<mspace={MonoSpacing}><color=#{hex}><b>" + Plain(answer) + "</b></color></mspace>"
                   + Format(template.Substring(gap + 4));
        }

        public static string Format(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            // Un "</noparse>" dentro del codigo cerraria la proteccion antes de tiempo.
            string safe = code.Replace("</noparse>", "</​noparse>");
            return $"<mspace={MonoSpacing}><noparse>{safe}</noparse></mspace>";
        }

        /// <summary>Solo protege de etiquetas, sin cambiar la fuente (para opciones de respuesta).</summary>
        public static string Plain(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return $"<noparse>{text.Replace("</noparse>", "</​noparse>")}</noparse>";
        }
    }
}

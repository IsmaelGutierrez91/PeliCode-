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

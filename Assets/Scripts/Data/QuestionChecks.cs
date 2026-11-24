using System.Linq;

namespace PeliCode.Data
{
    /// <summary>
    /// Revisa que una pregunta traiga los datos que necesita su vista. Las preguntas
    /// llegan desde Cloud Save (editadas a mano en el Dashboard), asi que una pregunta
    /// mal cargada se descarta con un aviso en vez de colgar la partida.
    /// </summary>
    public static class QuestionChecks
    {
        /// <summary>
        /// Bloques que caben en "arrastrar codigo": 2 filas de 4 en cada contenedor.
        /// Una pregunta con mas bloques se descarta en vez de salirse de la pantalla.
        /// </summary>
        public const int MaxDragBlocks = 8;

        /// <summary>
        /// Fragmentos que caben en "detectar el error" (la vista tiene 5 botones). Con mas, el
        /// fragmento con el error podia quedar sin boton y la pregunta no tenia respuesta.
        /// </summary>
        public const int MaxErrorSnippets = 5;

        /// <summary>
        /// Medidas de los textos (tercer documento de Ismael), para que no se descuadren:
        /// el enunciado de opcion multiple cabe en 2 filas de 73 caracteres; el de las otras
        /// vistas, en 2 filas de 83; y el codigo de completar, en 11 filas de 94.
        /// </summary>
        public const int MultipleChoiceRowChars = 73;
        public const int QuestionRowChars = 83;
        public const int QuestionRows = 2;
        public const int CodeRowChars = 94;
        public const int CodeRows = 11;

        /// <summary>
        /// Filas que ocupa un texto con un ancho de <paramref name="rowChars"/> caracteres: cada
        /// salto de linea empieza una fila y las palabras que no caben pasan a la siguiente
        /// (una palabra mas larga que la fila se corta).
        /// </summary>
        public static int Rows(string text, int rowChars)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int rows = 0;
            foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
            {
                rows++;
                int used = 0;
                foreach (string piece in line.Split(' '))
                {
                    string word = piece;
                    while (word.Length > rowChars)
                    {
                        if (used > 0) rows++;
                        word = word.Substring(rowChars);
                        used = 0;
                        rows++;
                    }
                    int needed = used == 0 ? word.Length : used + 1 + word.Length;
                    if (needed <= rowChars) used = needed;
                    else { rows++; used = word.Length; }
                }
            }
            return rows;
        }

        /// <summary>True si el enunciado (en los dos idiomas) y el codigo caben en sus medidas.</summary>
        public static bool FitsTextLimits(QuestionData q, out string problem)
        {
            problem = null;
            int rowChars = q.type == QuestionType.SeleccionMultiple ? MultipleChoiceRowChars : QuestionRowChars;
            if (Rows(q.questionText, rowChars) > QuestionRows)
                problem = $"el enunciado pasa de {QuestionRows} filas de {rowChars} caracteres";
            else if (Rows(q.questionTextEn, rowChars) > QuestionRows)
                problem = $"el enunciado en ingles pasa de {QuestionRows} filas de {rowChars} caracteres";
            else if (q.type == QuestionType.CompletarCodigo && Rows(q.codeTemplate, CodeRowChars) > CodeRows)
                problem = $"el codigo pasa de {CodeRows} filas de {CodeRowChars} caracteres";
            return problem == null;
        }

        public static bool IsPlayable(QuestionData q, out string problem)
        {
            problem = null;
            if (q == null)
            {
                problem = "pregunta vacia";
                return false;
            }

            switch (q.type)
            {
                case QuestionType.SeleccionMultiple:
                    if (string.IsNullOrWhiteSpace(q.correctAnswer)) problem = "falta correctAnswer";
                    else if (q.incorrectAnswerPool == null || q.incorrectAnswerPool.Count(a => !string.IsNullOrWhiteSpace(a)) == 0)
                        problem = "incorrectAnswerPool vacio";
                    break;

                case QuestionType.CompletarCodigo:
                    if (string.IsNullOrWhiteSpace(q.expectedAnswer)) problem = "falta expectedAnswer";
                    break;

                case QuestionType.ArrastrarCodigo:
                    if (q.codeBlocks == null || q.codeBlocks.Count == 0) problem = "codeBlocks vacio";
                    else if (q.codeBlocks.Count > MaxDragBlocks || (q.correctOrder != null && q.correctOrder.Count > MaxDragBlocks))
                        problem = $"tiene mas de {MaxDragBlocks} bloques y no caben en pantalla";
                    else if (q.correctOrder == null || q.correctOrder.Count == 0) problem = "correctOrder vacio";
                    else if (q.correctOrder.Any(i => i < 0 || i >= q.codeBlocks.Count))
                        problem = "correctOrder tiene indices fuera de codeBlocks";
                    break;

                case QuestionType.DetectarError:
                    if (q.codeSnippetsOptions == null || q.codeSnippetsOptions.Count < 2)
                        problem = "codeSnippetsOptions necesita al menos 2 bloques";
                    else if (q.codeSnippetsOptions.Count > MaxErrorSnippets)
                        problem = $"tiene mas de {MaxErrorSnippets} fragmentos y no caben en pantalla";
                    else if (q.buggyIndex < 0 || q.buggyIndex >= q.codeSnippetsOptions.Count)
                        problem = "buggyIndex fuera de codeSnippetsOptions";
                    break;

                default:
                    problem = $"tipo desconocido {q.type}";
                    break;
            }
            return problem == null;
        }
    }
}

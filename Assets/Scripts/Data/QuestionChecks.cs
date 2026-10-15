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
                    else if (q.correctOrder == null || q.correctOrder.Count == 0) problem = "correctOrder vacio";
                    else if (q.correctOrder.Any(i => i < 0 || i >= q.codeBlocks.Count))
                        problem = "correctOrder tiene indices fuera de codeBlocks";
                    break;

                case QuestionType.DetectarError:
                    if (q.codeSnippetsOptions == null || q.codeSnippetsOptions.Count < 2)
                        problem = "codeSnippetsOptions necesita al menos 2 bloques";
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

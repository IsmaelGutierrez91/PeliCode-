using System.Text.RegularExpressions;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Compara la respuesta escrita en "completar codigo" con la esperada.
    ///  - Distingue mayusculas, como C#: "Int" o "WHILE" no son "int" ni "while".
    ///  - Ignora espacios al principio y al final, y toma varios espacios seguidos como uno
    ///    ("else  if" es "else if").
    /// </summary>
    public static class AnswerMatcher
    {
        private static readonly Regex Spaces = new Regex(@"\s+");

        public static bool Matches(string typed, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected)) return false;
            return Normalize(typed) == Normalize(expected);
        }

        private static string Normalize(string text) =>
            Spaces.Replace((text ?? string.Empty).Trim(), " ");
    }
}

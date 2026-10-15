using System.Text.RegularExpressions;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Traduce entre el nombre de usuario del perfil y el "Player Name" de Unity
    /// Authentication, que es el que muestra el Leaderboard.
    ///  - Unity no acepta espacios en el Player Name: se cambian por "_".
    ///  - Unity le agrega un sufijo "#1234" al guardarlo: se quita al mostrarlo.
    /// </summary>
    public static class PlayerNames
    {
        private const int MaxServiceNameLength = 50;
        private static readonly Regex Whitespace = new Regex(@"\s+");
        private static readonly Regex DiscriminatorSuffix = new Regex(@"#\d+$");

        public static string ToServiceName(string userName)
        {
            string name = Whitespace.Replace((userName ?? string.Empty).Trim(), "_");
            if (name.Length > MaxServiceNameLength) name = name.Substring(0, MaxServiceNameLength);
            return name.Length == 0 ? "Jugador" : name;
        }

        public static string ToDisplayName(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return "Jugador";
            string name = DiscriminatorSuffix.Replace(serviceName.Trim(), string.Empty);
            return name.Length == 0 ? "Jugador" : name;
        }
    }
}

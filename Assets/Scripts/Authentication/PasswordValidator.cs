using System.Text.RegularExpressions;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Regla de negocio: la misma que exige Unity Authentication para usuario/contrasena
    /// (de 8 a 30 caracteres, 1 numero, 1 mayuscula, 1 minuscula y 1 caracter especial).
    /// Si la regla local fuera mas laxa, el registro fallaria en el servidor con un
    /// error tecnico en ingles.
    /// Centralizado aqui para que Login y Registro validen exactamente igual.
    /// </summary>
    public static class PasswordValidator
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 30;
        public const int MinUserNameLength = 3;
        public const int MaxUserNameLength = 20;

        // (?=.*\d) al menos un numero | (?=.*[a-z]) al menos una minuscula | (?=.*[A-Z]) al menos una mayuscula
        // (?=.*[^a-zA-Z0-9\s]) al menos un caracter especial | \S{8,30} de 8 a 30 caracteres, sin espacios
        private static readonly Regex PasswordRegex = new Regex(
            @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9\s])\S{8,30}$");

        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public static bool IsValid(string password)
        {
            return !string.IsNullOrEmpty(password) && PasswordRegex.IsMatch(password);
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email.Trim());
        }

        public static string GetUserNameValidationMessage(string userName)
        {
            string trimmed = userName?.Trim() ?? string.Empty;
            if (trimmed.Length == 0) return "El nombre de usuario no puede estar vacio.";
            if (trimmed.Length < MinUserNameLength || trimmed.Length > MaxUserNameLength)
                return $"El nombre de usuario debe tener entre {MinUserNameLength} y {MaxUserNameLength} caracteres.";
            return string.Empty;
        }

        public static string GetValidationMessage(string email, string password)
        {
            if (!IsValidEmail(email)) return "Correo electronico invalido.";
            if (!IsValid(password))
                return $"La contrasena debe tener de {MinPasswordLength} a {MaxPasswordLength} caracteres, sin espacios, " +
                       "con 1 numero, 1 mayuscula, 1 minuscula y 1 caracter especial.";
            return string.Empty;
        }
    }
}

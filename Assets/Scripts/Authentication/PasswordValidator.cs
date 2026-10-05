using System.Text.RegularExpressions;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Regla de negocio: minimo 8 caracteres, 1 numero, 1 caracter especial, 1 mayuscula.
    /// Centralizado aqui para que Login y Registro validen exactamente igual.
    /// </summary>
    public static class PasswordValidator
    {
        // (?=.*\d) al menos un numero | (?=.*[A-Z]) al menos una mayuscula
        // (?=.*[!@#$%^&*(),.?":{}|<>_\-+=]) al menos un caracter especial | .{8,} minimo 8 caracteres
        private static readonly Regex PasswordRegex = new Regex(
            @"^(?=.*\d)(?=.*[A-Z])(?=.*[!@#$%^&*(),.?"":{}|<>_\-+=]).{8,}$");

        public static bool IsValid(string password)
        {
            return !string.IsNullOrEmpty(password) && PasswordRegex.IsMatch(password);
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static string GetValidationMessage(string email, string password)
        {
            if (!IsValidEmail(email)) return "Correo electronico invalido.";
            if (!IsValid(password))
                return "La contrasena debe tener minimo 8 caracteres, 1 numero, 1 mayuscula y 1 caracter especial.";
            return string.Empty;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PeliCode.Localization
{
    public enum Language
    {
        Spanish = 0, // el indice coincide con el guardado en PlayerPrefs y con el Dropdown de Opciones
        English = 1
    }

    /// <summary>
    /// Textos del juego en espanol e ingles.
    ///
    /// La tabla vive en Resources/Localization/Textos.txt: una fila por texto, separada
    /// por tabuladores ("clave", "es", "en"), editable con cualquier hoja de calculo.
    /// "\n" dentro de un texto es un salto de linea; los textos con {0} se usan con Format.
    ///
    ///  - Textos fijos de las escenas: componente LocalizedText con la clave.
    ///  - Textos armados desde codigo: Loc.Get / Loc.Format, y si deben cambiar al vuelo,
    ///    suscribirse a LanguageChanged.
    ///
    /// El idioma elegido se guarda en PlayerPrefs; la primera vez se usa el del sistema.
    /// </summary>
    public static class Loc
    {
        public const string PrefKey = "pelicode_language";
        private const string TablePath = "Localization/Textos";

        private static Dictionary<string, string[]> _table;
        private static Language? _current;

        public static event Action LanguageChanged;

        public static Language Current
        {
            get
            {
                if (_current == null)
                {
                    int saved = PlayerPrefs.GetInt(PrefKey, -1);
                    _current = Enum.IsDefined(typeof(Language), saved)
                        ? (Language)saved
                        : Application.systemLanguage == SystemLanguage.Spanish ? Language.Spanish : Language.English;
                }
                return _current.Value;
            }
        }

        public static void SetLanguage(Language language)
        {
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            if (_current == language) return;
            _current = language;
            LanguageChanged?.Invoke();
        }

        /// <summary>Nombre del idioma escrito en ese mismo idioma (para el selector).</summary>
        public static string NativeName(Language language) =>
            language == Language.Spanish ? "Español" : "English";

        public static string Get(string key)
        {
            var table = Table;
            if (key != null && table.TryGetValue(key, out var texts))
            {
                string text = texts[(int)Current];
                // Si falta la traduccion se muestra el otro idioma antes que nada.
                return !string.IsNullOrEmpty(text) ? text : texts[1 - (int)Current];
            }

            Debug.LogWarning($"[Loc] Falta el texto '{key}' en {TablePath}.txt");
            return $"#{key}";
        }

        public static string Format(string key, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Get(key), args);

        public static bool HasKey(string key) => key != null && Table.ContainsKey(key);

        public static IReadOnlyDictionary<string, string[]> Entries => Table;

        private static Dictionary<string, string[]> Table
        {
            get
            {
                if (_table == null)
                {
                    var asset = Resources.Load<TextAsset>(TablePath);
                    if (asset == null)
                    {
                        Debug.LogError($"[Loc] No se encontro Resources/{TablePath}.txt");
                        _table = new Dictionary<string, string[]>();
                    }
                    else
                    {
                        _table = Parse(asset.text);
                    }
                }
                return _table;
            }
        }

        /// <summary>
        /// Lee la tabla: primera fila de encabezados, filas vacias o que empiezan con "#" se ignoran.
        /// </summary>
        public static Dictionary<string, string[]> Parse(string tsv)
        {
            var table = new Dictionary<string, string[]>();
            string[] lines = tsv.Replace("\r\n", "\n").Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                string[] cells = line.Split('\t');
                string key = cells[0].Trim();
                if (key.Length == 0) continue;

                string es = cells.Length > 1 ? Unescape(cells[1]) : string.Empty;
                string en = cells.Length > 2 ? Unescape(cells[2]) : string.Empty;
                if (table.ContainsKey(key))
                    Debug.LogWarning($"[Loc] Clave repetida '{key}' (fila {i + 1}); se usa la ultima.");
                table[key] = new[] { es, en };
            }
            return table;
        }

        private static string Unescape(string cell) => cell.Trim().Replace("\\n", "\n");
    }
}

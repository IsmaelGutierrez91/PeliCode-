using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PeliCode.Data;
using PeliCode.Localization;
using UnityEngine;

namespace PeliCode.Tests
{
    /// <summary>
    /// Revisa la tabla de idiomas (Resources/Localization/Textos.txt) contra el codigo y las escenas:
    /// que no falte ninguna clave ni ninguna traduccion.
    /// </summary>
    public class LocalizationTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{(\d+)(:[^}]*)?\}");

        [Test]
        public void EveryTextHasSpanishAndEnglish()
        {
            Assert.IsNotEmpty(Loc.Entries);
            foreach (var entry in Loc.Entries)
            {
                Assert.IsNotEmpty(entry.Value[(int)Language.Spanish], $"'{entry.Key}' no tiene espanol");
                Assert.IsNotEmpty(entry.Value[(int)Language.English], $"'{entry.Key}' no tiene ingles");
            }
        }

        [Test]
        public void BothLanguagesUseTheSamePlaceholders()
        {
            foreach (var entry in Loc.Entries)
            {
                var es = Placeholder.Matches(entry.Value[0]).Cast<Match>().Select(m => m.Groups[1].Value).OrderBy(x => x);
                var en = Placeholder.Matches(entry.Value[1]).Cast<Match>().Select(m => m.Groups[1].Value).OrderBy(x => x);
                CollectionAssert.AreEqual(es.ToList(), en.ToList(), $"'{entry.Key}' usa distintos {{n}} en cada idioma");
            }
        }

        [Test]
        public void EveryKeyUsedInCodeExists()
        {
            // Solo claves literales completas: Loc.Get("x") o Loc.Format("x", ...). Las armadas
            // como "category." + categoria se revisan abajo.
            var used = new Regex(@"Loc\.(?:Get|Format)\(\s*""([^""]+)""\s*[,)]");
            var missing = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match m in used.Matches(File.ReadAllText(file)))
                    if (!Loc.HasKey(m.Groups[1].Value)) missing.Add($"{Path.GetFileName(file)}: {m.Groups[1].Value}");
            }
            // Claves armadas en codigo: "category." + categoria y "difficulty." + dificultad
            foreach (MiniGameCategory c in Enum.GetValues(typeof(MiniGameCategory)))
                if (!Loc.HasKey("category." + c)) missing.Add("category." + c);
            foreach (Difficulty d in Enum.GetValues(typeof(Difficulty)))
                if (!Loc.HasKey("difficulty." + d)) missing.Add("difficulty." + d);

            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void EveryKeyUsedInScenesExists()
        {
            // Se lee el YAML de las escenas (sin abrirlas, para no tocar la escena abierta en el editor).
            string metaPath = Path.Combine(Application.dataPath, "Scripts", "Localization", "LocalizedText.cs.meta");
            string guid = Regex.Match(File.ReadAllText(metaPath), @"guid: (\w+)").Groups[1].Value;
            var block = new Regex(@"m_Script: \{fileID: 11500000, guid: " + guid + @", type: 3\}[\s\S]*?\n  key: (.*)");

            var missing = new List<string>();
            int found = 0;
            foreach (string scene in Directory.GetFiles(Path.Combine(Application.dataPath, "Scenes"), "*.unity"))
            {
                foreach (Match m in block.Matches(File.ReadAllText(scene)))
                {
                    found++;
                    string key = m.Groups[1].Value.Trim();
                    if (!Loc.HasKey(key)) missing.Add($"{Path.GetFileName(scene)}: '{key}'");
                }
            }
            Assert.Greater(found, 0, "No se encontro ningun LocalizedText en las escenas");
            CollectionAssert.IsEmpty(missing);
        }

        [Test]
        public void ParseIgnoresCommentsAndUnescapesNewLines()
        {
            var table = Loc.Parse("clave\tes\ten\n# comentario\n\nhola\tHola\\nmundo\tHello\r\nsolo\tSolo\n");
            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Hola\nmundo", table["hola"][0]);
            Assert.AreEqual("Hello", table["hola"][1]);
            Assert.AreEqual(string.Empty, table["solo"][1]);
        }
    }
}

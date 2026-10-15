using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PeliCode.Authentication;
using PeliCode.Data;
using PeliCode.MiniGame;
using PeliCode.MiniGame.Questions;

namespace PeliCode.Tests
{
    /// <summary>
    /// Pruebas EditMode de la logica que no depende de Unity Services.
    /// Window > General > Test Runner > EditMode, o por consola:
    ///   Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml
    /// </summary>
    public class PasswordValidatorTests
    {
        [TestCase("Abcdef1!", true)]
        [TestCase("abcdef1!", false)]   // sin mayuscula
        [TestCase("ABCDEF1!", false)]   // sin minuscula (Unity la exige)
        [TestCase("Abcdefg!", false)]   // sin numero
        [TestCase("Abcdefg1", false)]   // sin caracter especial
        [TestCase("Abc1!", false)]      // menos de 8
        [TestCase("Abcdef1! x", false)] // con espacio
        [TestCase("Abcdefghij1!Abcdefghij1!Abcdefg", false)] // 31, mas de 30
        [TestCase("Abcdefghij1!Abcdefghij1!Abcdef", true)]   // 30 justos
        [TestCase("Abcdef1~", true)]    // cualquier simbolo cuenta
        public void Password(string password, bool valid)
        {
            Assert.AreEqual(valid, PasswordValidator.IsValid(password));
        }

        [TestCase("ana@mail.com", true)]
        [TestCase("  ana@mail.com ", true)]
        [TestCase("ana@mail", false)]
        [TestCase("ana mail.com", false)]
        [TestCase("", false)]
        public void Email(string email, bool valid)
        {
            Assert.AreEqual(valid, PasswordValidator.IsValidEmail(email));
        }

        [TestCase("Ana", true)]
        [TestCase("  Ana  ", true)]
        [TestCase("An", false)]
        [TestCase("", false)]
        [TestCase("NombreDemasiadoLargoXX", false)]
        public void UserName(string userName, bool valid)
        {
            Assert.AreEqual(valid, string.IsNullOrEmpty(PasswordValidator.GetUserNameValidationMessage(userName)));
        }
    }

    public class PlayerNamesTests
    {
        [TestCase("Ana Maria", "Ana_Maria")]
        [TestCase("  Luis  ", "Luis")]
        [TestCase("", "Jugador")]
        public void ToServiceName(string userName, string expected)
        {
            Assert.AreEqual(expected, PlayerNames.ToServiceName(userName));
        }

        [TestCase("Ana_Maria#1234", "Ana_Maria")]
        [TestCase("Luis", "Luis")]
        [TestCase("#99", "Jugador")]
        [TestCase(null, "Jugador")]
        public void ToDisplayName(string serviceName, string expected)
        {
            Assert.AreEqual(expected, PlayerNames.ToDisplayName(serviceName));
        }
    }

    public class ProfileRulesTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 15, 18, 0, 0, DateTimeKind.Utc);

        private static PlayerProfileData Profile(string lastLogin, int current, int highest)
        {
            var p = PlayerProfileData.CreateNew("Ana", "ana@mail.com");
            p.lastLoginDateUtc = lastLogin;
            p.currentStreak = current;
            p.highestStreak = highest;
            return p;
        }

        [Test]
        public void StreakGrowsOnConsecutiveDay()
        {
            var p = Profile("2026-10-14", 3, 3);
            ProfileRules.UpdateLoginStreak(p, Today);
            Assert.AreEqual(4, p.currentStreak);
            Assert.AreEqual(4, p.highestStreak);
            Assert.AreEqual("2026-10-15", p.lastLoginDateUtc);
        }

        [Test]
        public void StreakStaysOnSameDay()
        {
            var p = Profile("2026-10-15", 3, 5);
            ProfileRules.UpdateLoginStreak(p, Today);
            Assert.AreEqual(3, p.currentStreak);
            Assert.AreEqual(5, p.highestStreak);
        }

        [Test]
        public void StreakResetsAfterAGap()
        {
            var p = Profile("2026-10-10", 7, 7);
            ProfileRules.UpdateLoginStreak(p, Today);
            Assert.AreEqual(1, p.currentStreak);
            Assert.AreEqual(7, p.highestStreak);
        }

        [Test]
        public void WeeklyResetClearsOldWeekOnly()
        {
            var p = Profile("2026-10-14", 1, 1);
            string thisWeek = ProfileRules.GetIsoWeekStamp(Today);
            var old = p.minigameStats[MiniGameCategory.Condicionales.ToString()];
            old.easyPercentage = 80f;
            old.weekStamp = "2026-W40";
            var current = p.minigameStats[MiniGameCategory.SwitchEstados.ToString()];
            current.hardPercentage = 60f;
            current.weekStamp = thisWeek;

            ProfileRules.ResetWeeklyStatsIfNeeded(p, Today);

            Assert.AreEqual(0f, old.easyPercentage);
            Assert.AreEqual(thisWeek, old.weekStamp);
            Assert.AreEqual(60f, current.hardPercentage);
        }

        [Test]
        public void GameResultStoresThatGamePercentage()
        {
            var p = Profile("2026-10-15", 1, 1);
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Medio, 7, 10, Today);
            Assert.AreEqual(70f, p.minigameStats[MiniGameCategory.Condicionales.ToString()].mediumPercentage, 0.001f);
        }

        [Test]
        public void GameWithoutQuestionsDoesNotOverwriteStats()
        {
            var p = Profile("2026-10-15", 1, 1);
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Facil, 4, 5, Today);
            ProfileRules.RegisterNormalGameResult(p, MiniGameCategory.Condicionales, Difficulty.Facil, 0, 0, Today);
            Assert.AreEqual(80f, p.minigameStats[MiniGameCategory.Condicionales.ToString()].easyPercentage, 0.001f);
        }
    }

    public class MultipleChoiceOptionsTests
    {
        private static QuestionData Question(params string[] incorrect) => new QuestionData
        {
            id = "q", type = QuestionType.SeleccionMultiple,
            correctAnswer = "float", incorrectAnswerPool = incorrect.ToList()
        };

        [Test]
        public void FourDistinctOptionsWithTheCorrectOne()
        {
            var q = Question("int", "bool", "string", "char", "byte", "long", "short", "double");
            for (int seed = 0; seed < 50; seed++)
            {
                var options = MultipleChoiceOptions.Build(q, 4, new Random(seed), out int correctIndex);
                Assert.AreEqual(4, options.Count);
                Assert.AreEqual(4, options.Distinct().Count());
                Assert.AreEqual("float", options[correctIndex]);
            }
        }

        [Test]
        public void CorrectAnswerMovesAround()
        {
            var q = Question("int", "bool", "string", "char");
            var positions = new HashSet<int>();
            for (int seed = 0; seed < 50; seed++)
            {
                MultipleChoiceOptions.Build(q, 4, new Random(seed), out int correctIndex);
                positions.Add(correctIndex);
            }
            Assert.AreEqual(4, positions.Count);
        }

        [Test]
        public void ShortPoolGivesFewerOptionsInsteadOfFailing()
        {
            var q = Question("int", "int", "float", " ");
            var options = MultipleChoiceOptions.Build(q, 4, new Random(1), out int correctIndex);
            CollectionAssert.AreEquivalent(new[] { "float", "int" }, options);
            Assert.AreEqual("float", options[correctIndex]);
        }
    }

    public class QuestionChecksTests
    {
        [Test]
        public void DragDropWithOrderOutOfRangeIsDiscarded()
        {
            var q = new QuestionData
            {
                type = QuestionType.ArrastrarCodigo,
                codeBlocks = new List<string> { "a", "b" },
                correctOrder = new List<int> { 0, 2 }
            };
            Assert.IsFalse(QuestionChecks.IsPlayable(q, out _));
        }

        [Test]
        public void FindErrorNeedsAValidBuggyIndex()
        {
            var q = new QuestionData
            {
                type = QuestionType.DetectarError,
                codeSnippetsOptions = new List<string> { "a", "b", "c" },
                buggyIndex = 3
            };
            Assert.IsFalse(QuestionChecks.IsPlayable(q, out _));
            q.buggyIndex = 2;
            Assert.IsTrue(QuestionChecks.IsPlayable(q, out _));
        }

        [Test]
        public void CompleteCodeNeedsAnExpectedAnswer()
        {
            var q = new QuestionData { type = QuestionType.CompletarCodigo, codeTemplate = "____ x = 1;" };
            Assert.IsFalse(QuestionChecks.IsPlayable(q, out _));
            q.expectedAnswer = "int";
            Assert.IsTrue(QuestionChecks.IsPlayable(q, out _));
        }
    }

    public class DragDropTests
    {
        private UnityEngine.GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        private T Spawn<T>(string prefabPath, UnityEngine.Transform parent) where T : UnityEngine.Component
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(prefabPath);
            return UnityEngine.Object.Instantiate(prefab, parent);
        }

        // Arrastra "block" hasta "slot" como lo haria el EventSystem: BeginDrag, Drop y EndDrag.
        private static void Drag(DragDropBlock block, DragDropSlot slot)
        {
            var data = new UnityEngine.EventSystems.PointerEventData(null) { pointerDrag = block.gameObject };
            block.OnBeginDrag(data);
            if (slot != null) slot.OnDrop(data);
            block.OnEndDrag(data);
        }

        [Test]
        public void DroppingOnAFullSlotSwapsOrReturnsTheOldBlock()
        {
            _root = new UnityEngine.GameObject("Canvas", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas));
            var blocks = new UnityEngine.GameObject("Blocks", typeof(UnityEngine.RectTransform)).transform;
            blocks.SetParent(_root.transform, false);
            var slotA = Spawn<DragDropSlot>("Assets/Prefabs/DragDropSlot.prefab", _root.transform);
            var slotB = Spawn<DragDropSlot>("Assets/Prefabs/DragDropSlot.prefab", _root.transform);
            var b0 = Spawn<DragDropBlock>("Assets/Prefabs/DragDropBlock.prefab", blocks);
            var b1 = Spawn<DragDropBlock>("Assets/Prefabs/DragDropBlock.prefab", blocks);
            var b2 = Spawn<DragDropBlock>("Assets/Prefabs/DragDropBlock.prefab", blocks);
            b0.Initialize(0, "a", blocks);
            b1.Initialize(1, "b", blocks);
            b2.Initialize(2, "c", blocks);

            Drag(b0, slotA);
            Drag(b1, slotB);
            Assert.AreEqual(slotA.transform, b0.transform.parent);
            Assert.AreEqual(slotB.transform, b1.transform.parent);

            // De casilla a casilla: se intercambian
            Drag(b0, slotB);
            Assert.AreEqual(slotB.transform, b0.transform.parent);
            Assert.AreEqual(slotA.transform, b1.transform.parent);

            // De la lista a una casilla ocupada: el que estaba vuelve a la lista (antes quedaba suelto)
            Drag(b2, slotA);
            Assert.AreEqual(slotA.transform, b2.transform.parent);
            Assert.AreEqual(blocks, b1.transform.parent);

            // Soltado fuera de una casilla: vuelve a la lista
            Drag(b0, null);
            Assert.AreEqual(blocks, b0.transform.parent);
        }
    }

    public class ScoreManagerTests
    {
        [Test]
        public void CountsAndPercentage()
        {
            var score = new ScoreManager();
            Assert.AreEqual(0f, score.GetPercentage());
            score.RegisterAnswer(true);
            score.RegisterAnswer(false);
            score.RegisterAnswer(true);
            score.RegisterAnswer(true);
            Assert.AreEqual(4, score.TotalAnswered);
            Assert.AreEqual(3, score.CorrectAnswers);
            Assert.AreEqual(75f, score.GetPercentage(), 0.001f);
        }
    }
}

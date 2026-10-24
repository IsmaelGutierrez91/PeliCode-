using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PeliCode.Audio;
using PeliCode.Authentication;
using PeliCode.CloudServices;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.MiniGame.Questions;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace PeliCode.MiniGame
{
    /// <summary>
    /// Orquestador de la escena MiniGame. Lee PeliCode.Core.MiniGameSessionData
    /// para saber si es partida normal o ranked, arma el pool de preguntas,
    /// controla el flujo de pregunta -> respuesta -> siguiente, el cronometro
    /// y guarda resultados al finalizar.
    /// </summary>
    public class MiniGameManager : MonoBehaviour, ISceneReadiness
    {
        [Header("Cantidad de preguntas por dificultad (modo normal)")]
        [SerializeField] private int easyQuestionCount = 5;
        [SerializeField] private int mediumQuestionCount = 10;
        [SerializeField] private int hardQuestionCount = 15;

        [Header("Referencias")]
        [SerializeField] private TimerController timer;
        [SerializeField] private TMP_Text feedbackText;   // "Correcto!" / "Incorrecto"
        [SerializeField] private TMP_Text progressText;   // "3 / 10"
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TMP_Text resultsText;
        [SerializeField] private Button exitButton;   // barra superior: abandonar la partida
        [SerializeField] private Button retryButton;  // panel de resultados: jugar otra vez

        [Header("Vistas de pregunta (una activa a la vez)")]
        [SerializeField] private MultipleChoiceView multipleChoiceView;
        [SerializeField] private CodeCompletionView codeCompletionView;
        [SerializeField] private DragDropCodeView dragDropCodeView;
        [SerializeField] private FindErrorView findErrorView;

        private const float NextQuestionDelay = 0.6f;
        // Tras un error se muestra la respuesta correcta: hace falta tiempo para leerla
        private const float AfterMistakeDelay = 2.5f;

        private readonly ScoreManager _scoreManager = new ScoreManager();
        private List<QuestionData> _pool = new List<QuestionData>();
        private int _poolIndex;
        private IQuestionView _activeView;
        private bool _isRanked;
        private bool _finished;
        private bool _poolReady;

        /// <summary>La pantalla de carga sigue hasta que las preguntas estan cargadas.</summary>
        public bool IsSceneReady => _poolReady;

        private void Awake()
        {
            if (exitButton != null) exitButton.onClick.AddListener(OnExitButtonPressed);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryButtonPressed);
        }

        private async void Start()
        {
            resultsPanel.SetActive(false);
            HideAllViews();
            _isRanked = MiniGameSessionData.IsRanked;
            SetFeedback(Loc.Get("game.loading"), UITheme.TextMuted);
            progressText.text = string.Empty;

            timer.OnTimerExpired += OnTimerExpired;

            _pool = _isRanked
                ? await BuildRankedPoolAsync()
                : await BuildNormalPoolAsync();
            if (this == null) return; // se salio de la escena mientras cargaba

            _scoreManager.Reset();
            _poolIndex = 0;
            _poolReady = true;

            // El cronometro arranca cuando ya se ve la pregunta, no detras de la pantalla de carga
            while (SceneTransition.IsTransitioning)
            {
                await System.Threading.Tasks.Task.Yield();
                if (this == null) return;
            }

            if (_pool.Count == 0)
            {
                ShowNoQuestions();
                return;
            }

            ShowNextQuestion();
        }

        private void OnDestroy()
        {
            timer.OnTimerExpired -= OnTimerExpired;
        }

        private async Task<List<QuestionData>> BuildNormalPoolAsync()
        {
            var questions = await QuestionsRepository.Instance.GetQuestionsAsync(
                MiniGameSessionData.Category, MiniGameSessionData.Difficulty);

            int count = MiniGameSessionData.Difficulty switch
            {
                Difficulty.Facil => easyQuestionCount,
                Difficulty.Medio => mediumQuestionCount,
                Difficulty.Dificil => hardQuestionCount,
                _ => easyQuestionCount
            };

            return Shuffle(Playable(questions)).Take(count).ToList();
        }

        private async Task<List<QuestionData>> BuildRankedPoolAsync()
        {
            // Ranked combina las 5 categorias y las 3 dificultades.
            var all = await QuestionsRepository.Instance.GetAllQuestionsAsync();
            return Shuffle(Playable(all));
        }

        private static List<QuestionData> Playable(List<QuestionData> questions)
        {
            var playable = new List<QuestionData>();
            foreach (var q in questions)
            {
                if (QuestionChecks.IsPlayable(q, out string problem)) playable.Add(q);
                else Debug.LogWarning($"[MiniGameManager] Se descarta la pregunta '{q?.id}': {problem}.");
            }
            return playable;
        }

        private static List<QuestionData> Shuffle(List<QuestionData> source)
        {
            return source.OrderBy(_ => Random.value).ToList();
        }

        private void ShowNextQuestion()
        {
            if (_finished) return;

            // Ranked con pila "infinita": si se acaban las preguntas, se reinicia
            // el mazo (se vuelve a barajar) y se sigue jugando.
            if (_poolIndex >= _pool.Count)
            {
                if (_isRanked)
                {
                    _pool = Shuffle(_pool);
                    _poolIndex = 0;
                }
                else
                {
                    EndNormalGame();
                    return;
                }
            }

            SetFeedback(string.Empty, UITheme.Text);
            progressText.text = _isRanked
                ? $"{_scoreManager.CorrectAnswers}"
                : $"{_poolIndex + 1} / {_pool.Count}";

            QuestionData question = _pool[_poolIndex];
            _activeView = GetViewFor(question.type);

            HideAllViews();
            _activeView.GameObject.SetActive(true);
            _activeView.Setup(question);
            _activeView.OnAnswered += OnQuestionAnswered;

            timer.ResetTimer();
        }

        private IQuestionView GetViewFor(QuestionType type) => type switch
        {
            QuestionType.SeleccionMultiple => multipleChoiceView,
            QuestionType.CompletarCodigo => codeCompletionView,
            QuestionType.ArrastrarCodigo => dragDropCodeView,
            QuestionType.DetectarError => findErrorView,
            _ => multipleChoiceView
        };

        private void HideAllViews()
        {
            multipleChoiceView.GameObject.SetActive(false);
            codeCompletionView.GameObject.SetActive(false);
            dragDropCodeView.GameObject.SetActive(false);
            findErrorView.GameObject.SetActive(false);
        }

        private void OnTimerExpired()
        {
            // Sin respuesta a tiempo = error, en ambos modos.
            HandleAnswer(false, timedOut: true);
        }

        private void OnQuestionAnswered(bool correct)
        {
            timer.StopTimer();
            HandleAnswer(correct);
        }

        private void HandleAnswer(bool correct, bool timedOut = false)
        {
            if (_finished || _activeView == null) return;

            var view = _activeView;
            var question = _pool[_poolIndex];
            view.OnAnswered -= OnQuestionAnswered;
            view.Clear();
            _activeView = null;

            _scoreManager.RegisterAnswer(correct);
            SetFeedback(correct
                    ? Loc.Get("game.correct")
                    : MistakeFeedback(timedOut, view.RevealAnswer(), question.ExplanationFor(Loc.Current)),
                correct ? UITheme.Success : UITheme.Error);
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                if (correct) audio.PlayCorrect();
                else audio.PlayWrong();
            }

            _poolIndex++;

            if (!correct && _isRanked)
            {
                // En ranked, un fallo termina la partida (despues de ver la respuesta correcta).
                Invoke(nameof(EndRankedGame), AfterMistakeDelay);
            }
            else
            {
                // En modo normal, un fallo solo pasa a la siguiente pregunta.
                Invoke(nameof(ShowNextQuestion), correct ? NextQuestionDelay : AfterMistakeDelay);
            }
        }

        /// <summary>"Incorrecto. La respuesta era: int" y, si la pregunta la trae, la explicacion.</summary>
        public static string MistakeFeedback(bool timedOut, string revealedAnswer, string explanation)
        {
            string text = Loc.Get(timedOut ? "game.timeUp" : "game.incorrect");
            if (!string.IsNullOrWhiteSpace(revealedAnswer))
                text += ". " + Loc.Format("game.answerWas", CodeText.Plain(revealedAnswer));
            if (!string.IsNullOrWhiteSpace(explanation))
                text += "\n" + CodeText.Plain(explanation);
            return text;
        }

        private void ShowNoQuestions()
        {
            _finished = true;
            timer.StopTimer();
            SetFeedback(string.Empty, UITheme.Text);
            if (exitButton != null) exitButton.gameObject.SetActive(false);
            resultsPanel.SetActive(true);
            // No se guarda nada: una partida sin preguntas no cambia las estadisticas.
            resultsText.text = QuestionsRepository.Instance.LastLoadFailed
                ? Loc.Get("game.loadFailed")
                : Loc.Get("game.noQuestions");
        }

        private async void EndNormalGame()
        {
            if (!BeginEnd()) return;

            resultsText.text =
                Loc.Format("game.normalHeader",
                    Loc.Get("category." + MiniGameSessionData.Category),
                    Loc.Get("difficulty." + MiniGameSessionData.Difficulty)) +
                "\n\n" +
                Loc.Format("game.result",
                    _scoreManager.CorrectAnswers, _scoreManager.TotalAnswered, _scoreManager.GetPercentage());

            var profile = AuthManager.Instance.CurrentProfile;
            if (profile == null) return; // escena abierta sin iniciar sesion (solo en el editor)

            CloudSaveManager.Instance.RegisterNormalGameResult(
                profile,
                MiniGameSessionData.Category,
                MiniGameSessionData.Difficulty,
                _scoreManager.CorrectAnswers,
                _scoreManager.TotalAnswered);

            await SaveAsync(() => CloudSaveManager.Instance.SaveProfileAsync(profile));
        }

        private async void EndRankedGame()
        {
            if (!BeginEnd()) return;

            int finalScore = _scoreManager.CorrectAnswers;
            resultsText.text = Loc.Format("game.rankedResult", finalScore);

            var profile = AuthManager.Instance.CurrentProfile;
            if (profile == null) return;

            if (finalScore > profile.rankedHighScore)
            {
                profile.rankedHighScore = finalScore;
                resultsText.text += "\n\n" + Loc.Get("game.newRecord");
            }

            await SaveAsync(async () =>
            {
                await CloudSaveManager.Instance.SaveProfileAsync(profile);
                await LeaderboardManager.Instance.SubmitScoreAsync(finalScore);
            });
        }

        private bool BeginEnd()
        {
            if (_finished) return false;
            _finished = true;
            CancelInvoke();
            HideAllViews();
            timer.StopTimer();
            if (exitButton != null) exitButton.gameObject.SetActive(false);
            resultsPanel.SetActive(true);
            return true;
        }

        /// <summary>Guarda y, si falla, lo dice en el panel de resultados en vez de perderlo en silencio.</summary>
        private async Task SaveAsync(Func<Task> save)
        {
            try
            {
                await save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MiniGameManager] No se pudo guardar el resultado: {e}");
                if (resultsText != null)
                    resultsText.text += "\n\n" + Loc.Get("game.saveFailed");
            }
        }

        private void SetFeedback(string text, Color color)
        {
            feedbackText.text = text;
            feedbackText.color = color;
        }

        /// <summary>Volver al menu. A mitad de partida es abandonar: no se guarda nada.</summary>
        public void OnExitButtonPressed()
        {
            SceneLoader.LoadMenu();
        }

        /// <summary>Otra partida igual (misma categoria y dificultad, o ranked).</summary>
        public void OnRetryButtonPressed()
        {
            SceneLoader.LoadMiniGame();
        }
    }
}

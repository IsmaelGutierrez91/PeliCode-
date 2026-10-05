using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PeliCode.Authentication;
using PeliCode.CloudServices;
using PeliCode.Core;
using PeliCode.Data;
using PeliCode.MiniGame.Questions;
using TMPro;
using UnityEngine;

namespace PeliCode.MiniGame
{
    /// <summary>
    /// Orquestador de la escena MiniGame. Lee PeliCode.Core.MiniGameSessionData
    /// para saber si es partida normal o ranked, arma el pool de preguntas,
    /// controla el flujo de pregunta -> respuesta -> siguiente, el cronometro
    /// y guarda resultados al finalizar.
    /// </summary>
    public class MiniGameManager : MonoBehaviour
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

        [Header("Vistas de pregunta (una activa a la vez)")]
        [SerializeField] private MultipleChoiceView multipleChoiceView;
        [SerializeField] private CodeCompletionView codeCompletionView;
        [SerializeField] private DragDropCodeView dragDropCodeView;
        [SerializeField] private FindErrorView findErrorView;

        private readonly ScoreManager _scoreManager = new ScoreManager();
        private List<QuestionData> _pool = new List<QuestionData>();
        private int _poolIndex;
        private IQuestionView _activeView;
        private bool _isRanked;

        private async void Start()
        {
            resultsPanel.SetActive(false);
            _isRanked = MiniGameSessionData.IsRanked;

            _pool = _isRanked
                ? await BuildRankedPoolAsync()
                : await BuildNormalPoolAsync();

            _scoreManager.Reset();
            _poolIndex = 0;

            timer.OnTimerExpired += OnTimerExpired;

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

            return Shuffle(questions).Take(count).ToList();
        }

        private async Task<List<QuestionData>> BuildRankedPoolAsync()
        {
            // Ranked combina las 5 categorias y las 3 dificultades.
            var all = await QuestionsRepository.Instance.GetAllQuestionsAsync();
            return Shuffle(all);
        }

        private static List<QuestionData> Shuffle(List<QuestionData> source)
        {
            return source.OrderBy(_ => Random.value).ToList();
        }

        private void ShowNextQuestion()
        {
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

            feedbackText.text = string.Empty;
            progressText.text = _isRanked
                ? $"{_scoreManager.TotalAnswered}"
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
            HandleAnswer(false);
        }

        private void OnQuestionAnswered(bool correct)
        {
            timer.StopTimer();
            HandleAnswer(correct);
        }

        private void HandleAnswer(bool correct)
        {
            if (_activeView != null)
            {
                _activeView.OnAnswered -= OnQuestionAnswered;
                _activeView.Clear();
            }

            _scoreManager.RegisterAnswer(correct);
            feedbackText.text = correct ? "Correcto!" : "Incorrecto";

            _poolIndex++;

            if (correct)
            {
                Invoke(nameof(ShowNextQuestion), 0.6f);
            }
            else if (_isRanked)
            {
                // En ranked, un fallo termina la partida.
                EndRankedGame();
            }
            else
            {
                // En modo normal, un fallo solo pasa a la siguiente pregunta.
                Invoke(nameof(ShowNextQuestion), 0.6f);
            }
        }

        private async void EndNormalGame()
        {
            HideAllViews();
            timer.StopTimer();
            resultsPanel.SetActive(true);
            resultsText.text =
                $"Resultado: {_scoreManager.CorrectAnswers}/{_scoreManager.TotalAnswered} " +
                $"({_scoreManager.GetPercentage():0}%)";

            var profile = AuthManager.Instance.CurrentProfile;
            CloudSaveManager.Instance.RegisterNormalGameResult(
                profile,
                MiniGameSessionData.Category,
                MiniGameSessionData.Difficulty,
                _scoreManager.CorrectAnswers,
                _scoreManager.TotalAnswered);

            await CloudSaveManager.Instance.SaveProfileAsync(profile);
        }

        private async void EndRankedGame()
        {
            HideAllViews();
            timer.StopTimer();
            resultsPanel.SetActive(true);

            int finalScore = _scoreManager.CorrectAnswers;
            resultsText.text = $"Preguntas respondidas: {finalScore}";

            var profile = AuthManager.Instance.CurrentProfile;
            if (finalScore > profile.rankedHighScore)
            {
                profile.rankedHighScore = finalScore;
            }

            await CloudSaveManager.Instance.SaveProfileAsync(profile);
            await LeaderboardManager.Instance.SubmitScoreAsync(finalScore);
        }

        public void OnExitButtonPressed()
        {
            SceneLoader.LoadMenu();
        }
    }
}

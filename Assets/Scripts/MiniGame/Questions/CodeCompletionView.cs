using System;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Vista de "Completar el codigo": muestra un TMP con el codeTemplate
    /// (ej: "int a = ____;" o "Console.WriteLine(a); // salida: ____") y un
    /// input field donde el jugador escribe la respuesta.
    /// </summary>
    public class CodeCompletionView : MonoBehaviour, IQuestionView
    {
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text codeTemplateText;
        [SerializeField] private TMP_InputField answerInput;
        [SerializeField] private Button submitButton;

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        private string _expectedAnswer;
        private string _template;

        public float MistakeDelaySeconds => IQuestionView.DefaultMistakeDelay;

        public void Setup(QuestionData question)
        {
            questionText.text = CodeText.Plain(question.QuestionTextFor(Loc.Current));
            _template = question.codeTemplate;
            codeTemplateText.text = CodeText.Format(_template);
            _expectedAnswer = question.expectedAnswer?.Trim();

            answerInput.text = string.Empty;
            answerInput.textComponent.color = UITheme.Text; // sin el rojo o verde de la pregunta anterior
            answerInput.interactable = true;
            submitButton.interactable = true;

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitAnswer);

            // Enter responde; el campo queda listo para escribir sin hacer clic.
            answerInput.onSubmit.RemoveAllListeners();
            answerInput.onSubmit.AddListener(_ =>
            {
                if (submitButton.interactable && !string.IsNullOrWhiteSpace(answerInput.text)) SubmitAnswer();
            });
            if (Application.isPlaying)
            {
                answerInput.Select();
                answerInput.ActivateInputField();
            }
        }

        private void SubmitAnswer()
        {
            if (!submitButton.interactable) return;
            answerInput.interactable = false;
            submitButton.interactable = false;

            bool correct = AnswerMatcher.Matches(answerInput.text, _expectedAnswer);
            if (correct) answerInput.textComponent.color = UITheme.Success;

            OnAnswered?.Invoke(correct);
        }

        public void Clear()
        {
            // Tambien al acabarse el tiempo: que no parezca que todavia se puede responder.
            answerInput.interactable = false;
            submitButton.interactable = false;
            submitButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Lo que escribio el jugador queda en rojo y la respuesta correcta aparece en verde,
        /// dentro del codigo. Tambien va al mensaje de abajo ("La respuesta era: int").
        /// </summary>
        public string RevealAnswer()
        {
            if (!string.IsNullOrWhiteSpace(answerInput.text)) answerInput.textComponent.color = UITheme.Error;
            codeTemplateText.text = CodeText.FormatFilled(_template, _expectedAnswer, UITheme.Success);
            return _expectedAnswer;
        }
    }
}

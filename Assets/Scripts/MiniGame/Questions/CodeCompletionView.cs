using System;
using PeliCode.Data;
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

        public void Setup(QuestionData question)
        {
            questionText.text = question.questionText;
            codeTemplateText.text = question.codeTemplate;
            _expectedAnswer = question.expectedAnswer?.Trim();

            answerInput.text = string.Empty;
            answerInput.interactable = true;
            submitButton.interactable = true;

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitAnswer);
        }

        private void SubmitAnswer()
        {
            answerInput.interactable = false;
            submitButton.interactable = false;

            bool correct = string.Equals(
                answerInput.text.Trim(),
                _expectedAnswer,
                StringComparison.OrdinalIgnoreCase);

            OnAnswered?.Invoke(correct);
        }

        public void Clear()
        {
            submitButton.onClick.RemoveAllListeners();
        }
    }
}

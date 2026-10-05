using System;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Vista de "Seleccionar la respuesta correcta": 1 pregunta + 4 botones.
    ///
    /// Cada QuestionData de este tipo trae 1 respuesta correcta y un pool de
    /// (idealmente) 8 respuestas incorrectas. En cada Setup():
    ///   1. Se eligen 3 incorrectas al azar del pool (sin repetir entre si).
    ///   2. Se arma la lista de 4 opciones (correcta + 3 incorrectas).
    ///   3. Se baraja el ORDEN de esas 4 opciones antes de asignarlas a los botones,
    ///      para que la respuesta correcta no quede siempre en la misma posicion.
    /// </summary>
    public class MultipleChoiceView : MonoBehaviour, IQuestionView
    {
        private const int OPTIONS_COUNT = 4;
        private const int INCORRECT_OPTIONS_NEEDED = OPTIONS_COUNT - 1; // 3

        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] optionButtons = new Button[OPTIONS_COUNT];
        [SerializeField] private TMP_Text[] optionTexts = new TMP_Text[OPTIONS_COUNT];

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        public void Setup(QuestionData question)
        {
            questionText.text = question.questionText;

            List<string> shuffledOptions = BuildShuffledOptions(question, out int correctIndex);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionTexts[i].text = shuffledOptions[i];
                optionButtons[i].interactable = true;

                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() =>
                {
                    SetAllInteractable(false);
                    OnAnswered?.Invoke(index == correctIndex);
                });
            }
        }

        /// <summary>
        /// Arma las 4 opciones a mostrar (1 correcta + 3 incorrectas elegidas al
        /// azar del pool, sin repetir), y devuelve el orden ya barajado junto con
        /// el indice donde quedo la respuesta correcta.
        /// </summary>
        private List<string> BuildShuffledOptions(QuestionData question, out int correctIndex)
        {
            var pool = question.incorrectAnswerPool ?? new List<string>();

            if (pool.Count < INCORRECT_OPTIONS_NEEDED)
            {
                Debug.LogWarning(
                    $"[MultipleChoiceView] La pregunta '{question.id}' tiene menos de " +
                    $"{INCORRECT_OPTIONS_NEEDED} respuestas incorrectas en su pool ({pool.Count}). " +
                    "Se usaran todas las disponibles.");
            }

            // Selecciona hasta 3 indices unicos al azar del pool (Fisher-Yates parcial).
            List<string> chosenIncorrect = pool
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(INCORRECT_OPTIONS_NEEDED)
                .ToList();

            var options = new List<string> { question.correctAnswer };
            options.AddRange(chosenIncorrect);

            // Baraja el ORDEN final para que la correcta no quede siempre primera.
            var shuffled = options
                .Select((text, originalIndex) => new { text, originalIndex })
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            correctIndex = shuffled.FindIndex(o => o.originalIndex == 0);
            return shuffled.Select(o => o.text).ToList();
        }

        private void SetAllInteractable(bool value)
        {
            foreach (var button in optionButtons)
                button.interactable = value;
        }

        public void Clear()
        {
            foreach (var button in optionButtons)
                button.onClick.RemoveAllListeners();
        }
    }
}

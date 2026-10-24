using System;
using System.Collections.Generic;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Vista de "Seleccionar la respuesta correcta": 1 pregunta + 4 botones.
    ///
    /// Cada QuestionData de este tipo trae 1 respuesta correcta y un pool de
    /// (idealmente) 8 respuestas incorrectas. MultipleChoiceOptions arma las 4
    /// opciones (correcta + 3 incorrectas al azar) y baraja su orden. Si el pool
    /// trae menos de 3 incorrectas, los botones que sobran se ocultan.
    /// </summary>
    public class MultipleChoiceView : MonoBehaviour, IQuestionView
    {
        private const int OPTIONS_COUNT = 4;
        private static readonly System.Random Rng = new System.Random();

        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] optionButtons = new Button[OPTIONS_COUNT];
        [SerializeField] private TMP_Text[] optionTexts = new TMP_Text[OPTIONS_COUNT];

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        private int _correctIndex = -1;
        private int _chosenIndex = -1;

        public void Setup(QuestionData question)
        {
            _chosenIndex = -1;
            questionText.text = CodeText.Plain(question.QuestionTextFor(Loc.Current));

            List<string> options = MultipleChoiceOptions.Build(question, optionButtons.Length, Rng, out int correctIndex, Loc.Current);
            if (options.Count < optionButtons.Length)
            {
                Debug.LogWarning(
                    $"[MultipleChoiceView] La pregunta '{question.id}' trae {options.Count - 1} respuestas " +
                    $"incorrectas validas; se muestran {options.Count} opciones.");
            }
            _correctIndex = correctIndex;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool active = i < options.Count;
                optionButtons[i].gameObject.SetActive(active);
                optionButtons[i].onClick.RemoveAllListeners();
                if (!active) continue;

                int index = i;
                optionTexts[i].text = CodeText.Plain(options[i]);
                UITheme.SetSelected(optionButtons[i], false); // sin el verde/rojo de la pregunta anterior
                optionButtons[i].interactable = true;
                optionButtons[i].onClick.AddListener(() =>
                {
                    _chosenIndex = index;
                    SetAllInteractable(false);
                    OnAnswered?.Invoke(index == correctIndex);
                });
            }
        }

        private void SetAllInteractable(bool value)
        {
            foreach (var button in optionButtons)
                button.interactable = value;
        }

        public void Clear()
        {
            SetAllInteractable(false);
            foreach (var button in optionButtons)
                button.onClick.RemoveAllListeners();
        }

        /// <summary>La opcion correcta en verde y la elegida (si se eligio una) en rojo.</summary>
        public string RevealAnswer()
        {
            if (_chosenIndex >= 0 && _chosenIndex != _correctIndex)
                UITheme.Highlight(optionButtons[_chosenIndex], UITheme.Error, UITheme.TextOnAccent);
            if (_correctIndex >= 0 && _correctIndex < optionButtons.Length)
                UITheme.Highlight(optionButtons[_correctIndex], UITheme.Success, UITheme.TextOnAccent);
            return null;
        }
    }
}

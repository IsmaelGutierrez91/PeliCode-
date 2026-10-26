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
    /// Vista de "Detectar el error": varios bloques de codigo mostrados como
    /// botones; el jugador selecciona el que contiene el error
    /// (sintactico, ej. falta ";", o logico).
    /// </summary>
    public class FindErrorView : MonoBehaviour, IQuestionView
    {
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] codeButtons;      // tamano dinamico segun la pregunta
        [SerializeField] private TMP_Text[] codeButtonTexts;

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        private int _buggyIndex = -1;
        private int _chosenIndex = -1;

        public void Setup(QuestionData question)
        {
            _buggyIndex = question.buggyIndex;
            _chosenIndex = -1;
            questionText.text = CodeText.Plain(question.QuestionTextFor(Loc.Current));

            int count = Mathf.Min(question.codeSnippetsOptions.Count, codeButtons.Length);
            if (question.codeSnippetsOptions.Count > codeButtons.Length)
            {
                Debug.LogWarning($"[FindErrorView] La pregunta '{question.id}' trae " +
                                 $"{question.codeSnippetsOptions.Count} bloques y solo hay {codeButtons.Length} botones.");
            }

            for (int i = 0; i < codeButtons.Length; i++)
            {
                bool active = i < count;
                codeButtons[i].gameObject.SetActive(active);
                if (!active) continue;

                int index = i;
                codeButtonTexts[i].text = CodeText.Format(question.codeSnippetsOptions[i]);
                UITheme.Paint(codeButtons[i], UITheme.CodeBackground); // sin el verde/rojo anterior
                codeButtonTexts[i].color = UITheme.Text;
                codeButtons[i].interactable = true;

                codeButtons[i].onClick.RemoveAllListeners();
                codeButtons[i].onClick.AddListener(() =>
                {
                    _chosenIndex = index;
                    SetAllInteractable(false);
                    OnAnswered?.Invoke(index == question.buggyIndex);
                });
            }
        }

        private void SetAllInteractable(bool value)
        {
            foreach (var button in codeButtons)
                if (button.gameObject.activeSelf) button.interactable = value;
        }

        public void Clear()
        {
            SetAllInteractable(false);
            foreach (var button in codeButtons)
                button.onClick.RemoveAllListeners();
        }

        /// <summary>El fragmento con el error en verde (era la respuesta) y el elegido en rojo.</summary>
        public string RevealAnswer()
        {
            if (_chosenIndex >= 0 && _chosenIndex != _buggyIndex)
                UITheme.Highlight(codeButtons[_chosenIndex], UITheme.Error, UITheme.TextOnAccent);
            if (_buggyIndex >= 0 && _buggyIndex < codeButtons.Length)
                UITheme.Highlight(codeButtons[_buggyIndex], UITheme.Success, UITheme.TextOnAccent);
            return null;
        }
    }
}

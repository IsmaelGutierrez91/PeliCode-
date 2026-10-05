using System;
using PeliCode.Data;
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

        public void Setup(QuestionData question)
        {
            questionText.text = question.questionText;

            int count = Mathf.Min(question.codeSnippetsOptions.Count, codeButtons.Length);

            for (int i = 0; i < codeButtons.Length; i++)
            {
                bool active = i < count;
                codeButtons[i].gameObject.SetActive(active);
                if (!active) continue;

                int index = i;
                codeButtonTexts[i].text = question.codeSnippetsOptions[i];
                codeButtons[i].interactable = true;

                codeButtons[i].onClick.RemoveAllListeners();
                codeButtons[i].onClick.AddListener(() =>
                {
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
            foreach (var button in codeButtons)
                button.onClick.RemoveAllListeners();
        }
    }
}

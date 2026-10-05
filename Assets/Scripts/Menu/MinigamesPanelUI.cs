using PeliCode.Core;
using PeliCode.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel principal de Minijuegos: 5 botones de categoria + 3 botones de
    /// dificultad. Al pulsar una categoria, se guarda la sesion y se carga
    /// la escena MiniGame.
    /// </summary>
    public class MinigamesPanelUI : MonoBehaviour
    {
        [Header("Botones de categoria (orden segun MiniGameCategory)")]
        [SerializeField] private Button[] categoryButtons = new Button[5];

        [Header("Botones de dificultad")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Indicador visual de dificultad seleccionada (opcional)")]
        [SerializeField] private GameObject easySelectedMark;
        [SerializeField] private GameObject normalSelectedMark;
        [SerializeField] private GameObject hardSelectedMark;

        private Difficulty _selectedDifficulty = Difficulty.Facil;

        private void Awake()
        {
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                var category = (MiniGameCategory)i;
                categoryButtons[i].onClick.AddListener(() => StartMiniGame(category));
            }

            easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Facil));
            normalButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Medio));
            hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Dificil));

            SelectDifficulty(_selectedDifficulty);
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            if (easySelectedMark) easySelectedMark.SetActive(difficulty == Difficulty.Facil);
            if (normalSelectedMark) normalSelectedMark.SetActive(difficulty == Difficulty.Medio);
            if (hardSelectedMark) hardSelectedMark.SetActive(difficulty == Difficulty.Dificil);
        }

        private void StartMiniGame(MiniGameCategory category)
        {
            MiniGameSessionData.SetNormalSession(category, _selectedDifficulty);
            SceneLoader.LoadMiniGame();
        }
    }
}

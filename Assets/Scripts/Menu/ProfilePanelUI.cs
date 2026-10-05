using PeliCode.Authentication;
using PeliCode.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Usuario/Perfil. Muestra nombre, top ranked, rachas y, segun la
    /// dificultad seleccionada (Button1/2/3), el porcentaje de acierto de las
    /// 5 categorias de minijuego.
    /// </summary>
    public class ProfilePanelUI : MonoBehaviour
    {
        [Header("Datos generales")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text rankedTopText;
        [SerializeField] private TMP_Text currentStreakText;
        [SerializeField] private TMP_Text highestStreakText;

        [Header("Botones de dificultad")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Panel de informacion (5 TMP, uno por minijuego, en orden 1-5)")]
        [SerializeField] private TMP_Text[] minigameInfoTexts = new TMP_Text[5];

        private Difficulty _selectedDifficulty = Difficulty.Facil;
        private PlayerProfileData _profile;

        private void Awake()
        {
            easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Facil));
            normalButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Medio));
            hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Dificil));
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            _profile = AuthManager.Instance.CurrentProfile;
            if (_profile == null) return;

            playerNameText.text = _profile.userName;
            rankedTopText.text = _profile.rankedHighScore.ToString();
            currentStreakText.text = _profile.currentStreak.ToString();
            highestStreakText.text = _profile.highestStreak.ToString();

            SelectDifficulty(_selectedDifficulty);
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            if (_profile == null) return;

            int i = 0;
            foreach (MiniGameCategory category in System.Enum.GetValues(typeof(MiniGameCategory)))
            {
                if (i >= minigameInfoTexts.Length) break;

                float percentage = 0f;
                if (_profile.minigameStats.TryGetValue(category.ToString(), out var stat))
                {
                    percentage = difficulty switch
                    {
                        Difficulty.Facil => stat.easyPercentage,
                        Difficulty.Medio => stat.mediumPercentage,
                        Difficulty.Dificil => stat.hardPercentage,
                        _ => 0f
                    };
                }

                minigameInfoTexts[i].text = $"{category}: {percentage:0}%";
                i++;
            }
        }
    }
}

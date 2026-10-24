using PeliCode.Authentication;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Usuario/Perfil. Muestra el nombre (grande), el top ranked y la mejor racha,
    /// la racha actual como un numero dentro de una insignia (solo desde
    /// ProfileRules.StreakBadgeMinDays dias seguidos) y, segun la dificultad seleccionada,
    /// el porcentaje de acierto de las 5 categorias: el nombre arriba y el porcentaje
    /// grande y centrado debajo.
    /// </summary>
    public class ProfilePanelUI : MonoBehaviour
    {
        [Header("Datos generales")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text rankedTopText;
        [SerializeField] private TMP_Text highestStreakText;

        [Header("Insignia de racha (se muestra con racha activa de 5 dias o mas)")]
        [SerializeField] private GameObject streakBadge;
        [SerializeField] private TMP_Text currentStreakText; // solo el numero

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
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            _profile = AuthManager.Instance.CurrentProfile;
            if (_profile == null) return;

            playerNameText.text = _profile.userName;
            rankedTopText.text = Loc.Format("profile.rankedTop", _profile.rankedHighScore);
            highestStreakText.text = Loc.Format("profile.highestStreak", _profile.highestStreak);

            bool badge = ProfileRules.ShowsStreakBadge(_profile.currentStreak);
            if (streakBadge != null) streakBadge.SetActive(badge);
            currentStreakText.text = _profile.currentStreak.ToString();

            SelectDifficulty(_selectedDifficulty);
        }

        /// <summary>"Condicionales:" arriba y el porcentaje grande, en negrita y centrado debajo.</summary>
        public static string FormatStat(string categoryName, float percentage) =>
            $"{categoryName}:\n<align=center><size=170%><b>{percentage:0}%</b></size></align>";

        private void SelectDifficulty(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            UITheme.SetSelected(easyButton, difficulty == Difficulty.Facil);
            UITheme.SetSelected(normalButton, difficulty == Difficulty.Medio);
            UITheme.SetSelected(hardButton, difficulty == Difficulty.Dificil);
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

                minigameInfoTexts[i].text = FormatStat(Loc.Get("category." + category), percentage);
                i++;
            }
        }
    }
}

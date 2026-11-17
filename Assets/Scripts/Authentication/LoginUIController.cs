using PeliCode.Core;
using PeliCode.Localization;
using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Controla el panel principal de "Iniciar sesion".
    /// Colocalo en el GameObject raiz del panel de Login dentro de la escena Login.
    /// </summary>
    public class LoginUIController : MonoBehaviour, ISceneReadiness
    {
        // La pantalla de carga espera a saber si hay una sesion guardada: asi, al abrir el juego
        // con la cuenta ya iniciada no se ve el login un instante antes de pasar al menu.
        private bool _sessionChecked;
        public bool IsSceneReady => _sessionChecked;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Botones")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button openCreateAccountButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackText;

        [Header("Panel de creacion de cuenta")]
        [SerializeField] private GameObject createAccountPanel;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            PasswordToggle.Attach(passwordInput);
            openCreateAccountButton.onClick.AddListener(() => createAccountPanel.SetActive(true));
            feedbackText.text = string.Empty;
        }

        // Un mensaje en el idioma anterior confunde mas de lo que ayuda: se borra.
        private void OnEnable() => Loc.LanguageChanged += ClearFeedback;
        private void OnDisable() => Loc.LanguageChanged -= ClearFeedback;
        private void ClearFeedback() => feedbackText.text = string.Empty;

        // Si quedo una sesion guardada en este dispositivo, se entra directo al menu
        private async void Start()
        {
            SetInteractable(false);
            bool resumed = await AuthManager.Instance.TryResumeSessionAsync();
            if (this == null) return;
            if (resumed)
            {
                SceneLoader.LoadMenu(); // sigue la pantalla de carga: no se llega a ver el login
                return;
            }
            _sessionChecked = true;
            SetInteractable(true);
        }

        private async void OnConfirmClicked()
        {
            SetInteractable(false);
            feedbackText.text = Loc.Get("login.signingIn");

            var (success, error) = await AuthManager.Instance.SignInAsync(
                emailInput.text.Trim(), passwordInput.text);

            if (success)
            {
                SceneLoader.LoadMenu();
            }
            else
            {
                feedbackText.text = error;
                SetInteractable(true);
            }
        }

        private void SetInteractable(bool value)
        {
            confirmButton.interactable = value;
            emailInput.interactable = value;
            passwordInput.interactable = value;
        }
    }
}

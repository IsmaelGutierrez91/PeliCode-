using PeliCode.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Controla el panel principal de "Iniciar sesion".
    /// Colocalo en el GameObject raiz del panel de Login dentro de la escena Login.
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
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
            openCreateAccountButton.onClick.AddListener(() => createAccountPanel.SetActive(true));
        }

        private async void OnConfirmClicked()
        {
            SetInteractable(false);
            feedbackText.text = string.Empty;

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

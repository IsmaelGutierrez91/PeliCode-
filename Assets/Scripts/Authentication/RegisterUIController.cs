using PeliCode.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Authentication
{
    /// <summary>
    /// Controla el pop-up de "Crear cuenta". Colocalo en el GameObject raiz
    /// del panel de registro (hijo o hermano del panel de Login).
    /// </summary>
    public class RegisterUIController : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField userNameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Botones")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackText;

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            closeButton.onClick.AddListener(ClosePanel);
        }

        private void OnEnable()
        {
            feedbackText.text = string.Empty;
        }

        private async void OnConfirmClicked()
        {
            SetInteractable(false);
            feedbackText.text = string.Empty;

            var (success, error) = await AuthManager.Instance.SignUpAsync(
                userNameInput.text.Trim(),
                emailInput.text.Trim(),
                passwordInput.text);

            if (success)
            {
                // Registro valido -> inicia sesion automaticamente (regla del documento de diseno)
                SceneLoader.LoadMenu();
            }
            else
            {
                feedbackText.text = error;
                SetInteractable(true);
            }
        }

        private void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        private void SetInteractable(bool value)
        {
            confirmButton.interactable = value;
            userNameInput.interactable = value;
            emailInput.interactable = value;
            passwordInput.interactable = value;
        }
    }
}

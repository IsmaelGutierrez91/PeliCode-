using PeliCode.Authentication;
using PeliCode.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Panel de Opciones: idioma, volumen de musica/SFX, cerrar sesion y salir.
    /// El volumen se controla via un AudioMixer con parametros expuestos
    /// "MusicVolume" y "SFXVolume" (en dB, por eso se usa Log10).
    /// </summary>
    public class OptionsPanelUI : MonoBehaviour
    {
        [Header("Idioma")]
        [SerializeField] private TMP_Dropdown languageDropdown; // 0 = Espanol, 1 = Ingles

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Botones")]
        [SerializeField] private Button closeSessionButton;
        [SerializeField] private Button quitButton;

        private const string MUSIC_PARAM = "MusicVolume";
        private const string SFX_PARAM = "SFXVolume";
        private const string LANGUAGE_PREF_KEY = "pelicode_language";

        private void Awake()
        {
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            closeSessionButton.onClick.AddListener(CloseSession);
            quitButton.onClick.AddListener(() => GameManager.Instance.QuitApplication());
        }

        private void OnEnable()
        {
            languageDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(LANGUAGE_PREF_KEY, 0));
            musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("music_volume", 0.8f));
            sfxVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("sfx_volume", 0.8f));
        }

        private void OnLanguageChanged(int index)
        {
            PlayerPrefs.SetInt(LANGUAGE_PREF_KEY, index);
            // Integrar aqui con Unity Localization Package si se agrega mas adelante:
            // LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        }

        private void OnMusicVolumeChanged(float linearValue)
        {
            SetMixerVolume(MUSIC_PARAM, linearValue);
            PlayerPrefs.SetFloat("music_volume", linearValue);
        }

        private void OnSfxVolumeChanged(float linearValue)
        {
            SetMixerVolume(SFX_PARAM, linearValue);
            PlayerPrefs.SetFloat("sfx_volume", linearValue);
        }

        private void SetMixerVolume(string param, float linearValue)
        {
            float clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
            float dB = Mathf.Log10(clamped) * 20f;
            audioMixer.SetFloat(param, dB);
        }

        private void CloseSession()
        {
            AuthManager.Instance.SignOut();
            SceneLoader.LoadLogin();
        }
    }
}

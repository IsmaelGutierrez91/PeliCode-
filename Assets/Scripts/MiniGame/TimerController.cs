using System;
using PeliCode.Localization;
using TMPro;
using UnityEngine;

namespace PeliCode.MiniGame
{
    /// <summary>
    /// Cronometro en segundos, compartido por el modo normal y ranked.
    /// Se reinicia cada vez que se responde una pregunta (ResetTimer).
    /// Si llega a 0, dispara OnTimerExpired (se interpreta como error en ambos modos).
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        [SerializeField] private int secondsPerQuestion = 60;
        [SerializeField] private TMP_Text timerText;

        public event Action OnTimerExpired;

        private float _remaining;
        private bool _running;

        private void Awake()
        {
            _remaining = secondsPerQuestion;
            UpdateText();
        }

        public void ResetTimer()
        {
            _remaining = secondsPerQuestion;
            _running = true;
            UpdateText();
        }

        public void StopTimer()
        {
            _running = false;
        }

        private void Update()
        {
            if (!_running) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _running = false;
                UpdateText();
                OnTimerExpired?.Invoke();
                return;
            }

            UpdateText();
        }

        private void UpdateText()
        {
            if (timerText != null)
                // "Tiempo: 42": la etiqueta no se pierde al actualizar el numero.
                timerText.text = Loc.Format("game.time", Mathf.CeilToInt(_remaining));
        }
    }
}

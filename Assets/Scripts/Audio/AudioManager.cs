using PeliCode.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace PeliCode.Audio
{
    /// <summary>
    /// Sonidos del juego y volumenes guardados.
    ///  - Los efectos (clic, acierto, error) se generan por codigo al arrancar: no hacen
    ///    falta archivos de audio. Salen por el grupo SFX del mezclador.
    ///  - Aplica al iniciar los volumenes guardados en Opciones (antes solo se aplicaban
    ///    al mover el slider).
    /// Vive en el GameObject "Managers" de la escena Login, con el resto de los managers.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        public const string MusicParam = "MusicVolume";
        public const string SfxParam = "SFXVolume";
        public const string MusicPrefKey = "music_volume";
        public const string SfxPrefKey = "sfx_volume";
        public const float DefaultVolume = 0.8f;

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;

        private const int SampleRate = 44100;
        private AudioSource _source;
        private AudioClip _click;
        private AudioClip _correct;
        private AudioClip _wrong;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.outputAudioMixerGroup = sfxGroup;

            _click = Tone("Clic", 0.035f, 1300f, 1300f, 0.25f);
            _correct = Sequence("Acierto", (660f, 0.08f), (990f, 0.14f));
            _wrong = Tone("Error", 0.28f, 240f, 150f, 0.35f);
        }

        // AudioMixer.SetFloat no tiene efecto en Awake: se aplica en Start.
        private void Start()
        {
            SetVolume(MusicParam, PlayerPrefs.GetFloat(MusicPrefKey, DefaultVolume));
            SetVolume(SfxParam, PlayerPrefs.GetFloat(SfxPrefKey, DefaultVolume));
        }

        public void PlayClick() => Play(_click);
        public void PlayCorrect() => Play(_correct);
        public void PlayWrong() => Play(_wrong);

        /// <summary>Volumen lineal (0 a 1) convertido a dB para el parametro expuesto del mezclador.</summary>
        public void SetVolume(string param, float linearValue)
        {
            if (mixer == null) return;
            mixer.SetFloat(param, ToDecibels(linearValue));
        }

        public static float ToDecibels(float linearValue) => Mathf.Log10(Mathf.Clamp(linearValue, 0.0001f, 1f)) * 20f;

        private void Play(AudioClip clip)
        {
            if (_source != null && clip != null) _source.PlayOneShot(clip);
        }

        // Onda senoidal con barrido de frecuencia y envolvente corta (sin clics al empezar ni terminar).
        private static float[] Samples(float seconds, float fromHz, float toHz, float volume)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[count];
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                phase += 2 * Mathf.PI * hz / SampleRate;
                float envelope = Mathf.Min(1f, t * 20f) * (1f - t) * (1f - t);
                data[i] = (float)System.Math.Sin(phase) * envelope * volume;
            }
            return data;
        }

        private static AudioClip Tone(string name, float seconds, float fromHz, float toHz, float volume)
        {
            var data = Samples(seconds, fromHz, toHz, volume);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sequence(string name, params (float hz, float seconds)[] notes)
        {
            var all = new System.Collections.Generic.List<float>();
            foreach (var (hz, seconds) in notes)
                all.AddRange(Samples(seconds, hz, hz, 0.3f));
            var clip = AudioClip.Create(name, all.Count, 1, SampleRate, false);
            clip.SetData(all.ToArray(), 0);
            return clip;
        }
    }
}

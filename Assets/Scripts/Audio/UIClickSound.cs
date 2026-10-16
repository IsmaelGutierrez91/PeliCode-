using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Audio
{
    /// <summary>Suena un clic al pulsar el boton de este GameObject.</summary>
    [RequireComponent(typeof(Button))]
    public class UIClickSound : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                var audio = AudioManager.Instance;
                if (audio != null) audio.PlayClick();
            });
        }
    }
}

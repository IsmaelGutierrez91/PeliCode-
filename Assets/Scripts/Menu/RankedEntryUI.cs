using TMPro;
using UnityEngine;

namespace PeliCode.Menu
{
    /// <summary>
    /// Prefab de una fila del panel Ranked: nombre de usuario + su top.
    /// </summary>
    public class RankedEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text scoreText;

        public void SetData(string playerName, int score)
        {
            playerNameText.text = playerName;
            scoreText.text = score.ToString();
        }
    }
}

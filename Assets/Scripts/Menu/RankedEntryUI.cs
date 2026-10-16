using PeliCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.Menu
{
    /// <summary>
    /// Prefab de una fila del panel Ranked: posicion, nombre de usuario y su top.
    /// La fila del jugador actual se resalta.
    /// </summary>
    public class RankedEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image background;

        public void SetData(int rank, string playerName, int score, bool isMe)
        {
            playerNameText.text = $"{rank}.  {CodeText.Plain(playerName)}";
            scoreText.text = score.ToString();

            Color text = isMe ? UITheme.TextOnAccent : UITheme.Text;
            playerNameText.color = text;
            scoreText.color = text;
            if (background != null) background.color = isMe ? UITheme.Accent : UITheme.SurfaceRaised;
        }
    }
}

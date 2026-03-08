using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiModeItem : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerText;
    public TextMeshProUGUI winChipsText;
    public TextMeshProUGUI winsText;
    public Image backgroundImage;
    public Button button;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetItem(string rank, string player, string winChips, string wins)
    {
        if (rankText != null)
        {
            rankText.text = rank;
        }

        if (playerText != null)
        {
            playerText.text = player;
        }

        if (winChipsText != null)
        {
            winChipsText.text = winChips;
        }

        Debug.Log(wins);
        if (winsText != null)
        {
            winsText.text = wins;
        }
    }
}

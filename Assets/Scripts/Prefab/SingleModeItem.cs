using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SingleModeItem : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI winChipsText;
    public TextMeshProUGUI winnerText;
    public Image backgroundImage;
    public Button button;

    public void SetItem(string date, string winChips, string winner)
    {
        if (dateText != null)
        {
            dateText.text = date;
        }

        if (winChipsText != null)
        {
            winChipsText.text = winChips;
        }

        if (winnerText != null)
        {
            winnerText.text = winner;
        }
    }
}

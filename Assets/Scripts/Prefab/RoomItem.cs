using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI hostNameText;
    public TextMeshProUGUI joinedNumberText;
    public Image backgroundImage;
    public Button button;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void setRoomInfo(string roomName, string hostName, int playerCount, int maxPlayers)
    {
        if (roomNameText != null)
        {
            roomNameText.text = roomName;
        }

        if (hostNameText != null)
        {
            hostNameText.text = hostName;
        }

        if (joinedNumberText != null)
        {
            int safeMax = Mathf.Max(1, maxPlayers);
            int safeCount = Mathf.Clamp(playerCount, 0, safeMax);
            joinedNumberText.text = $"{safeCount}/{safeMax}";
        }
    }
}

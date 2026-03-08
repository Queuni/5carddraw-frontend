using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player: MonoBehaviour
{
    
    private Coroutine messageCoroutine;

    public string playerName;
    public int playerIndex;
    public int tokenAmount;
    public bool isCPU;
    public List<Card> playerHand;
    public List<Card> selectedCards;
    public Transform handCenter;
    public Transform messageBox;
    public TextMeshProUGUI messageMeshPro;
    public Image avatarImage;
    public TextMeshProUGUI tokenMeshPro;
    public TextMeshProUGUI nameMeshPro;

    // Betting state
    public int currentBet;
    public bool hasFolded;
    public bool hasActedThisRound;
    public bool hasExchanged;
    
    // Hand evaluation
    public HandRank handRank;
    public int handValue;

    void Awake()
    {
        playerHand = new List<Card>();
        selectedCards = new List<Card>();
        isCPU = false;
        tokenAmount = Constants.STARTING_CHIPS; // Starting chips
        currentBet = 0;
        hasFolded = false;
        hasActedThisRound = false;
        hasExchanged = false;
        handRank = HandRank.HighCard;
        handValue = 0;

        if (messageBox != null)
        {
            messageBox.gameObject.SetActive(false);
        }
    }

    public void SetAvatar(Sprite avatarSprite)
    {
        if (avatarImage == null)
        {
            Debug.LogWarning($"Player {playerName}: Image component not found on AvatarButton.");
            return;
        }

        if (avatarSprite == null)
        {
            Debug.LogWarning("Avatar sprite is null.");
            return;
        }

        avatarImage.sprite = avatarSprite;
    }

    public void SetTokenAmount(int newAmount)
    {
        this.tokenAmount = newAmount;
        if (tokenMeshPro == null)
        {
            Debug.LogWarning($"Player {playerName}: Token TextMeshPro component not found.");
            return;
        }

        tokenMeshPro.text = newAmount.ToString();
    }

    public void SetPlayerName(string name)
    {
        this.playerName = name;
        if (nameMeshPro == null)
        {
            Debug.LogWarning($"Player {playerName}: Name TextMeshPro component not found.");
            return;
        }

        nameMeshPro.text = name;
    }

    public void AddCardToHand(Card card)
    {
        this.playerHand.Add(card);
    }

    public void RemoveCardFromHand(Card card)
    {
        if (playerHand.Contains(card))
        {
            playerHand.Remove(card);
            // Don't destroy cardObject here - let animation handle it
            // This allows for smooth exchange animations
        }
    }

    public void RemoveCardFromHandImmediate(Card card)
    {
        if (playerHand.Contains(card))
        {
            playerHand.Remove(card);
            if (card.cardObject != null)
            {
                Destroy(card.cardObject);
            }
        }
    }

    public void ResetForNewRound()
    {
        selectedCards.Clear();
        currentBet = 0;
        hasFolded = false;
        hasActedThisRound = false;
        hasExchanged = false;
        handRank = HandRank.HighCard;
        handValue = 0;
        
        // Clear hand
        foreach (Card card in playerHand)
        {
            if (card.cardObject != null)
            {
                Destroy(card.cardObject);
            }
        }
        playerHand.Clear();
    }

    public bool CanBet(int amount)
    {
        return tokenAmount >= amount && !hasFolded;
    }

    public void Bet(int amount)
    {
        int actualBet = Mathf.Min(amount, tokenAmount);
        SetTokenAmount(tokenAmount - actualBet); // Updates both field and UI
        currentBet += actualBet;
    }

    public void showMessage(string message)
    {
        if (messageBox == null)
        {
            Debug.LogWarning($"Player {playerName}: MessageBox is null. Cannot show message: {message}");
            return;
        }

        if (messageMeshPro == null)
        {
            Debug.LogWarning($"Player {playerName}: MessageMeshPro is null. Cannot show message: {message}");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"Player {playerName}: Empty message provided");
            return;
        }

        // Stop any existing message coroutine
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        // Start new message display coroutine
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message));
    }

    private IEnumerator ShowMessageCoroutine(string message)
    {
        // Set message text
        messageMeshPro.text = message;
        
        // Show message box
        messageBox.gameObject.SetActive(true);
        
        // Wait for 2 seconds
        yield return new WaitForSeconds(2.0f);
        
        // Hide message box
        messageBox.gameObject.SetActive(false);
        
        messageCoroutine = null;
    }
}
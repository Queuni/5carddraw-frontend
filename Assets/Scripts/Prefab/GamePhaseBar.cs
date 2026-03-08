using System.Collections.Generic;
using UnityEngine;

public class GamePhaseBar : MonoBehaviour
{
    [Header("Phase Text Components")]
    public TMPro.TextMeshProUGUI anteText;
    public TMPro.TextMeshProUGUI exchangeText;
    public TMPro.TextMeshProUGUI bettingText;
    public TMPro.TextMeshProUGUI showdownText;

    private Dictionary<GamePhase, TMPro.TextMeshProUGUI> phaseTextDic;
    private Color activeColor;   // F37E08 (orange)
    private Color inactiveColor; // FFFFFF (white)
    private GamePhase currentPhase = GamePhase.None;

    private void Awake()
    {
        // Parse colors from hex
        ColorUtility.TryParseHtmlString("#F37E08", out activeColor);
        ColorUtility.TryParseHtmlString("#FFFFFF", out inactiveColor);
        
        // Initialize dictionary
        phaseTextDic = new Dictionary<GamePhase, TMPro.TextMeshProUGUI>();
        
        if (anteText != null)
            phaseTextDic.Add(GamePhase.Ante, anteText);
        if (exchangeText != null)
            phaseTextDic.Add(GamePhase.Exchange, exchangeText);
        if (bettingText != null)
            phaseTextDic.Add(GamePhase.Betting, bettingText);
        if (showdownText != null)
            phaseTextDic.Add(GamePhase.Showdown, showdownText);
    }

    void Start()
    {
        // Initialize all phases to inactive
        UpdatePhaseDisplay(GamePhase.None);
        
        // Subscribe to GameFlowManager if available
        if (GameFlowManager.Instance != null)
        {
            // Get game state from GameFlowManager
            // We'll need to access it differently since GameState is private
            // Alternative: Subscribe to phase change events
        }
    }

    public void SetPhase(GamePhase phase)
    {
        // Map DealCards to previous phase or Ante (DealCards is not displayed)
        // Map GameOver/None to None (show all inactive)
        GamePhase displayPhase = phase;
        
        if (phase == GamePhase.DealCards)
        {
            // During DealCards, keep Ante highlighted
            displayPhase = GamePhase.Ante;
        }
        else if (phase == GamePhase.GameOver || phase == GamePhase.None)
        {
            // Show all inactive
            displayPhase = GamePhase.None;
        }
        
        if (currentPhase != displayPhase)
        {
            currentPhase = displayPhase;
            UpdatePhaseDisplay(displayPhase);
        }
    }

    private void UpdatePhaseDisplay(GamePhase activePhase)
    {
        // Update all phase texts
        foreach (var kvp in phaseTextDic)
        {
            if (kvp.Value != null)
            {
                // Set color based on whether this phase is active
                if (kvp.Key == activePhase)
                {
                    kvp.Value.color = activeColor;
                }
                else
                {
                    kvp.Value.color = inactiveColor;
                }
            }
        }
    }

    // Called from scene when phase changes
    public void OnPhaseChanged(GamePhase newPhase)
    {
        SetPhase(newPhase);
    }
}

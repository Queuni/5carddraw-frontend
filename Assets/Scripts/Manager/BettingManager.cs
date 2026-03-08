using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    private GameState gameState;

    public void Initialize(GameState state)
    {
        gameState = state;
    }

    public IEnumerator RunBettingRound()
    {
        // Start with the player after the dealer (or first player in exchange order)
        gameState.currentPlayerIndex = 0;
        
        int lastRaiseIndex = -1;
        int roundsWithoutRaise = 0;
        
        while (true)
        {
            Player currentPlayer = gameState.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.hasFolded)
            {
                gameState.GetNextActivePlayer();
                continue;
            }
            
            // Check if betting round is complete
            if (IsBettingRoundComplete())
            {
                break;
            }
            
            // Handle player action
            if (currentPlayer.isCPU)
            {
                yield return StartCoroutine(HandleCPUAction(currentPlayer));
            }
            else
            {
                yield return StartCoroutine(WaitForHumanAction(currentPlayer));
            }
            
            // Check if this was a raise
            if (currentPlayer.currentBet > gameState.currentBet)
            {
                gameState.currentBet = currentPlayer.currentBet;
                lastRaiseIndex = currentPlayer.playerIndex;
                roundsWithoutRaise = 0;
            }
            else
            {
                roundsWithoutRaise++;
            }
            
            // Move to next player
            gameState.GetNextActivePlayer();
        }
    }

    private IEnumerator WaitForHumanAction(Player player)
    {
        // Wait for human to call, raise, or fold
        while (!player.hasActedThisRound && !player.hasFolded)
        {
            yield return null;
        }
    }

    private IEnumerator HandleCPUAction(Player player)
    {
        yield return new WaitForSeconds(1.5f);
        
        CPUAI cpuAI = GameFlowManager.Instance.GetComponent<CPUAI>();
        if (cpuAI != null)
        {
            cpuAI.DecideBettingAction(player, gameState.currentBet);
        }
    }

    private bool IsBettingRoundComplete()
    {
        List<Player> activePlayers = gameState.GetActivePlayers();
        
        if (activePlayers.Count <= 1)
        {
            return true; // Only one player left
        }
        
        // Check if all active players have acted and bets are equal
        bool allActed = true;
        int commonBet = -1;
        
        foreach (Player player in activePlayers)
        {
            if (!player.hasActedThisRound)
            {
                allActed = false;
                break;
            }
            
            if (commonBet == -1)
            {
                commonBet = player.currentBet;
            }
            else if (player.currentBet != commonBet)
            {
                allActed = false;
                break;
            }
        }
        
        return allActed && commonBet >= 0;
    }
}


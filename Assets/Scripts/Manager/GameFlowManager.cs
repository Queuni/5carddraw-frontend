using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;
    
    private GameState gameState;
    private BettingManager bettingManager;
    private CPUAI cpuAI;
    private TableAnimator tableAnimator;
    private CardAnimator cardAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(GameState state, TableAnimator animator = null)
    {
        gameState = state;
        bettingManager = GetComponent<BettingManager>();
        if (bettingManager == null)
        {
            bettingManager = gameObject.AddComponent<BettingManager>();
        }
        bettingManager.Initialize(state);
        
        cpuAI = GetComponent<CPUAI>();
        if (cpuAI == null)
        {
            cpuAI = gameObject.AddComponent<CPUAI>();
        }
        cpuAI.Initialize(state);
        
        // Store TableAnimator reference
        tableAnimator = animator;
        
        // Cache CardAnimator reference for performance
        cardAnimator = FindObjectOfType<CardAnimator>();
    }

    public void StartGame()
    {
        StartCoroutine(GameFlowCoroutine());
    }

    private IEnumerator GameFlowCoroutine()
    {
        // Phase 1: Ante (BEFORE cards are dealt - as per requirements)
        yield return StartCoroutine(HandleAntePhase());
        
        // Phase 2: Deal Cards (signal to scene to deal cards)
        yield return StartCoroutine(HandleDealCardsPhase());
        
        // Phase 3: Exchange
        yield return StartCoroutine(HandleExchangePhase());
        
        // Phase 4: Betting
        yield return StartCoroutine(HandleBettingPhase());
        
        // Phase 5: Showdown
        yield return StartCoroutine(HandleShowdownPhase());
    }
    
    private IEnumerator HandleDealCardsPhase()
    {
        gameState.currentPhase = GamePhase.DealCards;
        NotifyPhaseChanged(GamePhase.DealCards);
        
        // Signal that cards should be dealt
        if (OnDealCardsRequested != null)
        {
            OnDealCardsRequested();
        }
        
        // Wait for cards to be dealt (scene will signal when done)
        while (gameState.currentPhase == GamePhase.DealCards)
        {
            yield return null;
        }
    }
    

    private IEnumerator HandleAntePhase()
    {
        gameState.currentPhase = GamePhase.Ante;
        NotifyPhaseChanged(GamePhase.Ante);
        
        // Show pot GameObject
        if (tableAnimator != null)
        {
            tableAnimator.ShowPot();
        }
        else
        {
            Debug.LogWarning("GameFlowManager: tableAnimator is null! Pot will not be shown.");
        }
        
        // Collect ante from all players
        foreach (Player player in gameState.playerList)
        {
            if (player.tokenAmount >= gameState.anteAmount)
            {
                player.Bet(gameState.anteAmount);
                gameState.pot += gameState.anteAmount;
            }
        }
        
        // Update UI
        if (OnPotUpdated != null)
        {
            OnPotUpdated(gameState.pot);
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HandleExchangePhase()
    {
        gameState.currentPhase = GamePhase.Exchange;
        NotifyPhaseChanged(GamePhase.Exchange);
        gameState.ResetBettingRound();
        
        // In single mode, human goes first, then CPU
        // In multi mode, start with left chair and rotate clockwise
        if (gameState.gameMode == GameMode.SingleMode)
        {
            // Human player exchange
            if (gameState.humanPlayer != null && !gameState.humanPlayer.hasFolded)
            {
                yield return StartCoroutine(WaitForHumanExchange());
            }
            
            // CPU player exchange
            if (gameState.cpuPlayer != null && !gameState.cpuPlayer.hasFolded)
            {
                yield return StartCoroutine(HandleCPUExchange());
            }
            
            // Wait for animations to complete (exchange animations can take ~1.5 seconds)
            yield return new WaitForSeconds(2.0f);
            
            // ExchangeCardsAnimation already handles sorting and setting sibling indices
            // This is just a final safety check
            EnsureSiblingOrderCorrect();
        }
        else
        {
            // Multiplayer: start with left chair (index 0) and rotate clockwise
            for (int i = 0; i < gameState.playerList.Count; i++)
            {
                Player player = gameState.playerList[i];
                if (player.hasFolded) continue;
                
                if (player.isCPU)
                {
                    yield return StartCoroutine(HandleCPUExchange());
                }
                else
                {
                    yield return StartCoroutine(WaitForHumanExchange());
                }
            }
            
            // Wait for animations to complete (exchange animations can take ~1.5 seconds)
            yield return new WaitForSeconds(2.0f);
            
            // ExchangeCardsAnimation already handles sorting and setting sibling indices
            // This is just a final safety check
            EnsureSiblingOrderCorrect();
        }
    }
    
    /// <summary>
    /// Ensure all players' cards have correct sibling indices based on sorted hand order (synchronous, no animation)
    /// </summary>
    private void EnsureSiblingOrderCorrect()
    {
        foreach (Player player in gameState.playerList)
        {
            if (player == null || player.hasFolded) continue;
            
            for (int i = 0; i < player.playerHand.Count; i++)
            {
                Card card = player.playerHand[i];
                if (card != null && card.cardObject != null)
                {
                    card.cardObject.transform.SetSiblingIndex(i);
                }
            }
        }
    }

    private IEnumerator WaitForHumanExchange()
    {
        // Wait for human to click exchange or skip
        if (gameState.humanPlayer == null)
        {
            Debug.LogError("GameFlowManager: humanPlayer is null in WaitForHumanExchange!");
            yield break;
        }
        
        while (!gameState.humanPlayer.hasExchanged)
        {
            yield return null;
        }
    }

    private IEnumerator HandleCPUExchange()
    {
        yield return new WaitForSeconds(1.0f); // Delay for realism
        
        if (gameState.cpuPlayer != null)
        {
            cpuAI.DecideExchange(gameState.cpuPlayer);
            gameState.cpuPlayer.hasExchanged = true;
        }
    }

    private IEnumerator HandleBettingPhase()
    {
        gameState.currentPhase = GamePhase.Betting;
        NotifyPhaseChanged(GamePhase.Betting);
        gameState.ResetBettingRound();
        
        // In single mode, human bets first, then CPU responds
        if (gameState.gameMode == GameMode.SingleMode)
        {
            // Human player bets first (initial bet: 5, 10, or 25)
            if (gameState.humanPlayer != null && !gameState.humanPlayer.hasFolded)
            {
                yield return StartCoroutine(WaitForHumanBet());
            }
            
            // CPU responds (call, raise, or fold)
            if (gameState.cpuPlayer != null && !gameState.cpuPlayer.hasFolded)
            {
                yield return StartCoroutine(HandleCPUBet());
                
                // If CPU raised, human can respond
                if (gameState.humanPlayer != null && !gameState.humanPlayer.hasFolded && gameState.cpuPlayer.currentBet > gameState.humanPlayer.currentBet)
                {
                    // Reset human's action flag so they can respond to CPU's raise
                    gameState.humanPlayer.hasActedThisRound = false;
                    yield return StartCoroutine(WaitForHumanBet());
                }
            }
        }
        else
        {
            // Multiplayer betting round
            yield return StartCoroutine(bettingManager.RunBettingRound());
        }
    }

    private IEnumerator WaitForHumanBet()
    {
        if (gameState.humanPlayer == null)
        {
            Debug.LogError("GameFlowManager: humanPlayer is null in WaitForHumanBet!");
            yield break;
        }
        
        // Notify UI that betting phase is active and buttons should be shown
        if (OnPotUpdated != null)
        {
            OnPotUpdated(gameState.pot);
        }
        
        // Wait for human to bet/call/raise/fold
        while (gameState.humanPlayer != null && !gameState.humanPlayer.hasActedThisRound && !gameState.humanPlayer.hasFolded)
        {
            yield return null;
        }
        
        // Update pot with human's bet
        if (gameState.humanPlayer != null && gameState.humanPlayer.currentBet > 0)
        {
            gameState.pot += gameState.humanPlayer.currentBet;
            if (gameState.humanPlayer.currentBet > gameState.currentBet)
            {
                gameState.currentBet = gameState.humanPlayer.currentBet;
                gameState.lastRaisePlayerIndex = gameState.humanPlayer.playerIndex;
            }
        }
        
        if (OnPotUpdated != null)
        {
            OnPotUpdated(gameState.pot);
        }
    }

    private IEnumerator HandleCPUBet()
    {
        yield return new WaitForSeconds(1.5f); // Delay for realism
        
        if (gameState.cpuPlayer != null)
        {
            int previousBet = gameState.currentBet;
            CPUAI.CPUBettingAction action = cpuAI.DecideBettingAction(gameState.cpuPlayer, gameState.currentBet);
            gameState.cpuPlayer.hasActedThisRound = true;
            
            // Update pot
            if (gameState.cpuPlayer.currentBet > 0)
            {
                gameState.pot += gameState.cpuPlayer.currentBet;
            }
            
            // Update current bet if CPU raised (before calculating message)
            if (gameState.cpuPlayer.currentBet > gameState.currentBet)
            {
                gameState.currentBet = gameState.cpuPlayer.currentBet;
                gameState.lastRaisePlayerIndex = gameState.cpuPlayer.playerIndex;
            }
            else if (!gameState.cpuPlayer.hasFolded)
            {
                // CPU called - match the current bet
                gameState.cpuPlayer.currentBet = gameState.currentBet;
            }
            
            // Determine action message for status text
            string actionMessage = "";
            string playerMessage = ""; // Message to show on player prefab
            
            if (action == CPUAI.CPUBettingAction.Fold)
            {
                actionMessage = "CPU folded!";
                playerMessage = "Fold";
            }
            else if (action == CPUAI.CPUBettingAction.Call)
            {
                // CPU called - they matched the bet, so cpuPlayer.currentBet equals previousBet
                int cpuPaid = gameState.cpuPlayer.currentBet;
                if (cpuPaid > 0)
                {
                    actionMessage = $"CPU called ({cpuPaid} chips)";
                    playerMessage = $"Call {cpuPaid}";
                }
                else
                {
                    actionMessage = "CPU checked";
                    playerMessage = "Check";
                }
            }
            else if (action == CPUAI.CPUBettingAction.Raise)
            {
                int newBetAmount = gameState.cpuPlayer.currentBet;
                int raiseAmount = newBetAmount - previousBet;
                actionMessage = $"CPU raised to {newBetAmount} chips! (+{raiseAmount})";
                playerMessage = $"Raise to {newBetAmount}";
            }
            
            // Show message on CPU player prefab
            if (gameState.cpuPlayer != null && !string.IsNullOrEmpty(playerMessage))
            {
                gameState.cpuPlayer.showMessage(playerMessage);
            }
            else
            {
                Debug.LogWarning($"Cannot show CPU message - cpuPlayer: {gameState.cpuPlayer != null}, playerMessage: '{playerMessage}'");
            }
            
            // Notify UI about CPU action (for status text)
            if (OnCPUAction != null && !string.IsNullOrEmpty(actionMessage))
            {
                OnCPUAction(actionMessage);
            }
            
            if (OnPotUpdated != null)
            {
                OnPotUpdated(gameState.pot);
            }
        }
    }

    private IEnumerator HandleShowdownPhase()
    {
        gameState.currentPhase = GamePhase.Showdown;
        NotifyPhaseChanged(GamePhase.Showdown);
        
        // Reveal all cards
        yield return StartCoroutine(RevealAllCards());
        
        // Evaluate all active players' hands
        List<Player> activePlayers = gameState.GetActivePlayers();
        
        // Show each player's hand type
        foreach (Player player in activePlayers)
        {
            HandEvaluator.EvaluateHand(player);
            string handType = FormatHandRank(player.handRank);
            player.showMessage(handType);
        }
        
        yield return new WaitForSeconds(2.0f); // Wait for players to see their hand types
        
        // Determine winner
        if (activePlayers.Count == 1)
        {
            // Only one player left, they win
            Player winner = activePlayers[0];
            
            // Animate pot to winner before awarding tokens
            if (tableAnimator != null)
            {
                yield return StartCoroutine(tableAnimator.AnimatePotToWinner(winner));
            }
            
            // Award pot to the winner (after animation completes)
            winner.SetTokenAmount(winner.tokenAmount + gameState.pot); // Updates both field and UI
            
            // Show winner message
            winner.showMessage("Wins!");
            
            if (OnGameOver != null)
            {
                OnGameOver(winner, "Won by default (others folded)");
            }
        }
        else
        {
            // Compare hands - find the player with the highest hand value
            // Evaluate all hands first to ensure they're up to date
            foreach (Player player in activePlayers)
            {
                HandEvaluator.EvaluateHand(player);
            }
            
            // Find the player with the highest handValue by directly comparing handValues
            Player winner = activePlayers[0];
            int highestHandValue = winner.handValue;
            
            // Compare all players to find the one with the highest hand value
            for (int i = 1; i < activePlayers.Count; i++)
            {
                // Directly compare handValues to find the maximum
                if (activePlayers[i].handValue > highestHandValue)
                {
                    winner = activePlayers[i];
                    highestHandValue = activePlayers[i].handValue;
                }
            }
            
            // Animate pot to winner before awarding tokens
            if (tableAnimator != null)
            {
                yield return StartCoroutine(tableAnimator.AnimatePotToWinner(winner));
            }
            
            // Award pot to the winner (after animation completes)
            winner.SetTokenAmount(winner.tokenAmount + gameState.pot); // Updates both field and UI
            
            if (OnGameOver != null)
            {
                OnGameOver(winner, winner.handRank.ToString());
            }
        }
        
        yield return new WaitForSeconds(3.0f);
        
        // Reset for next round or end game
        if (OnRoundComplete != null)
        {
            OnRoundComplete();
        }
    }
    
    private string FormatHandRank(HandRank handRank)
    {
        switch (handRank)
        {
            case HandRank.RoyalFlush:
                return "Royal Flush";
            case HandRank.StraightFlush:
                return "Straight Flush";
            case HandRank.FourOfAKind:
                return "Four of a Kind";
            case HandRank.FullHouse:
                return "Full House";
            case HandRank.Flush:
                return "Flush";
            case HandRank.Straight:
                return "Straight";
            case HandRank.ThreeOfAKind:
                return "Three of a Kind";
            case HandRank.TwoPair:
                return "Two Pair";
            case HandRank.OnePair:
                return "One Pair";
            case HandRank.HighCard:
                return "High Card";
            default:
                return handRank.ToString();
        }
    }

    private IEnumerator RevealAllCards()
    {
        if (cardAnimator == null)
        {
            Debug.LogWarning("GameFlowManager: CardAnimator not found! Cards will not be revealed.");
            yield break;
        }

        foreach (Player player in gameState.playerList)
        {
            if (player == null || player.hasFolded) continue;
            
            if (player.playerHand == null) continue;
            
            // For CPU players, flip all cards simultaneously
            if (player.isCPU)
            {
                List<Card> cpuCardsToFlip = new List<Card>();
                foreach (Card card in player.playerHand)
                {
                    if (card != null && card.cardObject != null)
                    {
                        cpuCardsToFlip.Add(card);
                    }
                }
                
                // Start all flip animations simultaneously
                int activeFlips = 0;
                foreach (Card card in cpuCardsToFlip)
                {
                    activeFlips++;
                    StartCoroutine(cardAnimator.FlipCardsAndCount(card, card.cardObject, () => activeFlips--));
                }
                
                // Wait for all flips to complete
                while (activeFlips > 0)
                {
                    yield return null;
                }
            }
        }
    }

    private void NotifyPhaseChanged(GamePhase phase)
    {
        if (OnPhaseChanged != null)
        {
            OnPhaseChanged(phase);
        }
    }

    // Events
    public System.Action<int> OnPotUpdated;
    public System.Action<Player, string> OnGameOver;
    public System.Action OnRoundComplete;
    public System.Action<string> OnCPUAction; // CPU betting decision message
    public System.Action OnDealCardsRequested; // Signal to deal cards
    public System.Action<GamePhase> OnPhaseChanged; // Phase change notification
}

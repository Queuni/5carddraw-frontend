using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CPUAI : MonoBehaviour
{
    private GameState gameState;
    
    // AI Personality Traits (can be randomized for variety)
    private float aggressionLevel = 0.6f; // 0.0 = tight, 1.0 = loose/aggressive
    private float bluffFrequency = 0.25f; // 25% chance to bluff (increased from 15%)
    private float potOddsThreshold = 0.15f; // Minimum pot odds to call (lowered from 0.3f for more calls)
    
    // Track game history for adaptive play
    private int roundsPlayed = 0;
    private int bluffsAttempted = 0;
    private int successfulBluffs = 0;

    public void Initialize(GameState state)
    {
        gameState = state;
        // Randomize AI personality slightly for variety
        aggressionLevel = Random.Range(0.55f, 0.8f); // Slightly more aggressive range
        bluffFrequency = Random.Range(0.2f, 0.3f); // Higher bluff frequency range
    }

    public void DecideExchange(Player cpuPlayer)
    {
        // Evaluate current hand strength
        HandEvaluator.EvaluateHand(cpuPlayer);
        
        List<Card> cardsToExchange = new List<Card>();
        
        // Smart exchange strategy
        if (cpuPlayer.handRank >= HandRank.ThreeOfAKind)
        {
            // Very strong hand - keep everything
            cardsToExchange.Clear();
        }
        else if (cpuPlayer.handRank == HandRank.TwoPair)
        {
            // Two pair - keep all, might exchange kicker if low
            var rankGroups = cpuPlayer.playerHand.GroupBy(c => c.rank);
            var pairs = rankGroups.Where(g => g.Count() == 2).Select(g => g.Key).ToList();
            var kicker = rankGroups.FirstOrDefault(g => g.Count() == 1);
            
            if (kicker != null && kicker.Key < 10) // Low kicker
            {
                cardsToExchange.Add(cpuPlayer.playerHand.First(c => c.rank == kicker.Key));
            }
        }
        else if (cpuPlayer.handRank == HandRank.OnePair)
        {
            // One pair - keep pair, evaluate kickers and potential draws
            var rankGroups = cpuPlayer.playerHand.GroupBy(c => c.rank);
            var pair = rankGroups.FirstOrDefault(g => g.Count() == 2);
            
            if (pair != null)
            {
                int pairRank = pair.Key;
                var kickers = rankGroups.Where(g => g.Count() == 1 && g.Key != pairRank).ToList();
                
                // Keep pair and high kickers (10+)
                foreach (var kickerGroup in kickers)
                {
                    if (kickerGroup.Key < 10) // Low kicker, exchange
                    {
                        cardsToExchange.Add(cpuPlayer.playerHand.First(c => c.rank == kickerGroup.Key));
                    }
                }
            }
        }
        else
        {
            // High card or weak hand - look for draws and keep high cards
            var potentialDraws = EvaluateDraws(cpuPlayer.playerHand);
            
            if (potentialDraws.straightDraw || potentialDraws.flushDraw)
            {
                // Keep cards that contribute to draw
                foreach (Card card in cpuPlayer.playerHand)
                {
                    bool keepCard = false;
                    
                    if (potentialDraws.straightDraw)
                    {
                        // Check if card is part of straight draw
                        keepCard = IsPartOfStraightDraw(card, cpuPlayer.playerHand, potentialDraws);
                    }
                    if (potentialDraws.flushDraw && !keepCard)
                    {
                        // Check if card is part of flush draw
                        keepCard = IsPartOfFlushDraw(card, cpuPlayer.playerHand);
                    }
                    
                    if (!keepCard && card.rank < 10) // Not part of draw and low rank
                    {
                        cardsToExchange.Add(card);
                    }
                }
            }
            else
            {
                // No draws, keep highest 1-2 cards, exchange rest
                List<Card> sorted = Rules.SortCards(cpuPlayer.playerHand);
                int cardsToKeep = sorted[sorted.Count - 1].rank >= 12 ? 1 : 2; // Keep 1 if Ace/King, else 2
                
                for (int i = 0; i < sorted.Count - cardsToKeep && cardsToExchange.Count < Constants.MAX_EXCHANGE_CARDS; i++)
                {
                    cardsToExchange.Add(sorted[i]);
                }
            }
        }
        
        // Limit to max exchange
        if (cardsToExchange.Count > Constants.MAX_EXCHANGE_CARDS)
        {
            cardsToExchange = cardsToExchange.Take(Constants.MAX_EXCHANGE_CARDS).ToList();
        }
        
        // Perform exchange
        if (cardsToExchange.Count > 0)
        {
            // Store card positions BEFORE exchange (cards are still in hand at this point)
            Dictionary<Card, Vector3> cardPositionsBeforeExchange = new Dictionary<Card, Vector3>();
            foreach (Card card in cpuPlayer.playerHand)
            {
                if (card != null && card.cardObject != null)
                {
                    cardPositionsBeforeExchange[card] = card.cardObject.transform.position;
                }
            }
            
            DealManager.ExchangeCards(cpuPlayer, cardsToExchange, gameState);
            
            // Animate exchange with stored positions
            CardAnimator cardAnimator = FindObjectOfType<CardAnimator>();
            if (cardAnimator != null && cpuPlayer.handCenter != null)
            {
                // Start animation coroutine (will complete asynchronously)
                StartCoroutine(WaitForExchangeAnimation(cardAnimator, cpuPlayer, cardsToExchange, cpuPlayer.handCenter, cardPositionsBeforeExchange));
            }
            else
            {
                Debug.LogWarning("CardAnimator or handCenter not found - cards exchanged but not animated");
                
                // Fallback: Ensure sibling indices are set correctly even without animation
                for (int i = 0; i < cpuPlayer.playerHand.Count; i++)
                {
                    Card card = cpuPlayer.playerHand[i];
                    if (card != null && card.cardObject != null)
                    {
                        card.cardObject.transform.SetSiblingIndex(i);
                    }
                }
            }
        }
        
        cpuPlayer.hasExchanged = true;
    }
    
    private System.Collections.IEnumerator WaitForExchangeAnimation(CardAnimator cardAnimator, Player player, List<Card> cardsToExchange, Transform handCenter, Dictionary<Card, Vector3> cardPositionsBeforeExchange)
    {
        // ExchangeCardsAnimation already handles sorting and setting sibling indices
        yield return cardAnimator.StartCoroutine(cardAnimator.ExchangeCardsAnimation(player, cardsToExchange, handCenter, cardPositionsBeforeExchange));
    }
    
    private struct DrawEvaluation
    {
        public bool straightDraw;
        public bool flushDraw;
    }
    
    private DrawEvaluation EvaluateDraws(List<Card> hand)
    {
        DrawEvaluation eval = new DrawEvaluation();
        
        // Check for flush draw (4 cards of same suit)
        var suitGroups = hand.GroupBy(c => c.suit);
        eval.flushDraw = suitGroups.Any(g => g.Count() == 4);
        
        // Check for straight draw (4 cards in sequence)
        List<int> ranks = hand.Select(c => c.rank).OrderBy(r => r).ToList();
        int consecutiveCount = 1;
        int maxConsecutive = 1;
        
        for (int i = 1; i < ranks.Count; i++)
        {
            if (ranks[i] == ranks[i - 1] + 1)
            {
                consecutiveCount++;
                maxConsecutive = Mathf.Max(maxConsecutive, consecutiveCount);
            }
            else if (ranks[i] != ranks[i - 1])
            {
                consecutiveCount = 1;
            }
        }
        
        eval.straightDraw = maxConsecutive >= 4;
        
        return eval;
    }
    
    private bool IsPartOfStraightDraw(Card card, List<Card> hand, DrawEvaluation eval)
    {
        if (!eval.straightDraw) return false;
        
        List<int> ranks = hand.Select(c => c.rank).OrderBy(r => r).ToList();
        
        // Check if removing this card breaks the straight draw
        List<int> otherRanks = ranks.Where(r => r != card.rank).OrderBy(r => r).ToList();
        int consecutiveCount = 1;
        int maxConsecutive = 1;
        
        for (int i = 1; i < otherRanks.Count; i++)
        {
            if (otherRanks[i] == otherRanks[i - 1] + 1)
            {
                consecutiveCount++;
                maxConsecutive = Mathf.Max(maxConsecutive, consecutiveCount);
            }
            else if (otherRanks[i] != otherRanks[i - 1])
            {
                consecutiveCount = 1;
            }
        }
        
        return maxConsecutive >= 3; // Still has potential with other cards
    }
    
    private bool IsPartOfFlushDraw(Card card, List<Card> hand)
    {
        var suitGroups = hand.GroupBy(c => c.suit);
        var flushSuit = suitGroups.FirstOrDefault(g => g.Count() >= 4);
        
        return flushSuit != null && card.suit == flushSuit.Key;
    }

    public enum CPUBettingAction
    {
        Call,
        Raise,
        Fold
    }

    public CPUBettingAction DecideBettingAction(Player cpuPlayer, int currentBet)
    {
        // Evaluate hand strength
        HandEvaluator.EvaluateHand(cpuPlayer);
        
        int callAmount = currentBet - cpuPlayer.currentBet;
        int raiseAmount = currentBet * 2 - cpuPlayer.currentBet;
        
        // Calculate pot odds
        float potOdds = CalculatePotOdds(callAmount, gameState.pot);
        
        // Calculate hand strength (0.0 to 1.0)
        float handStrength = CalculateHandStrength(cpuPlayer);
        
        // Determine if this is a good spot to bluff
        bool shouldBluff = ShouldBluff(cpuPlayer, currentBet, potOdds);
        
        CPUBettingAction action = CPUBettingAction.Fold;
        
        // Decision logic with pot odds, hand strength, and bluffing
        if (shouldBluff)
        {
            // Bluffing with weak hand
            bluffsAttempted++;
            if (Random.Range(0f, 1f) < aggressionLevel)
            {
                // Aggressive bluff - raise
                if (cpuPlayer.CanBet(raiseAmount) && raiseAmount <= cpuPlayer.tokenAmount * 0.4f)
                {
                    cpuPlayer.Bet(raiseAmount);
                    cpuPlayer.currentBet = raiseAmount;
                    action = CPUBettingAction.Raise;
                }
                else if (cpuPlayer.CanBet(callAmount))
                {
                    // Bluff call
                    cpuPlayer.Bet(callAmount);
                    cpuPlayer.currentBet = currentBet;
                    action = CPUBettingAction.Call;
                }
                else
                {
                    cpuPlayer.hasFolded = true;
                    action = CPUBettingAction.Fold;
                }
            }
            else
            {
                // Conservative bluff - just call small bets
                if (callAmount <= 5 && cpuPlayer.CanBet(callAmount))
                {
                    cpuPlayer.Bet(callAmount);
                    cpuPlayer.currentBet = currentBet;
                    action = CPUBettingAction.Call;
                }
                else
                {
                    cpuPlayer.hasFolded = true;
                    action = CPUBettingAction.Fold;
                }
            }
        }
        else if (handStrength >= 0.85f) // Very strong hand (Full House+)
        {
            // Premium hand - aggressive betting
            if (cpuPlayer.CanBet(raiseAmount))
            {
                cpuPlayer.Bet(raiseAmount);
                cpuPlayer.currentBet = raiseAmount;
                action = CPUBettingAction.Raise;
            }
            else if (cpuPlayer.CanBet(callAmount))
            {
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else
            {
                // All in
                cpuPlayer.Bet(cpuPlayer.tokenAmount);
                cpuPlayer.currentBet += cpuPlayer.tokenAmount;
                action = CPUBettingAction.Raise;
            }
        }
        else if (handStrength >= 0.65f) // Strong hand (Three of a Kind, Two Pair)
        {
            // Strong hand - value bet or call
            float raiseProbability = aggressionLevel * 0.7f;
            
            if (Random.Range(0f, 1f) < raiseProbability && cpuPlayer.CanBet(raiseAmount) && callAmount <= 15)
            {
                // Value raise
                cpuPlayer.Bet(raiseAmount);
                cpuPlayer.currentBet = raiseAmount;
                action = CPUBettingAction.Raise;
            }
            else if (cpuPlayer.CanBet(callAmount))
            {
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else
            {
                cpuPlayer.hasFolded = true;
                action = CPUBettingAction.Fold;
            }
        }
        else if (handStrength >= 0.35f) // Moderate hand (One Pair) - lowered threshold from 0.45f
        {
            // Moderate hand - more willing to call
            if (potOdds >= potOddsThreshold && cpuPlayer.CanBet(callAmount))
            {
                // Good pot odds - call
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else if (callAmount <= 15 && cpuPlayer.CanBet(callAmount)) // Increased from 5 to 15
            {
                // Reasonable bet size - call
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else if (callAmount <= cpuPlayer.tokenAmount * 0.3f && cpuPlayer.CanBet(callAmount))
            {
                // Bet is less than 30% of chips - call even with moderate pot odds
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else
            {
                cpuPlayer.hasFolded = true;
                action = CPUBettingAction.Fold;
            }
        }
        else // Weak hand (High Card)
        {
            // Weak hand - more willing to call reasonable bets
            if (callAmount == 0)
            {
                // Free to check - always call
                action = CPUBettingAction.Call;
            }
            else if (potOdds >= 0.25f && callAmount <= 15 && cpuPlayer.CanBet(callAmount)) // Lowered from 0.5f and increased from 10
            {
                // Decent pot odds with reasonable bet - call
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else if (callAmount <= 5 && cpuPlayer.CanBet(callAmount))
            {
                // Very small bet - call
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else if (callAmount <= cpuPlayer.tokenAmount * 0.2f && potOdds >= 0.15f && cpuPlayer.CanBet(callAmount))
            {
                // Small bet relative to chips with decent pot odds - call
                cpuPlayer.Bet(callAmount);
                cpuPlayer.currentBet = currentBet;
                action = CPUBettingAction.Call;
            }
            else
            {
                cpuPlayer.hasFolded = true;
                action = CPUBettingAction.Fold;
            }
        }
        
        cpuPlayer.hasActedThisRound = true;
        roundsPlayed++;
        return action;
    }
    
    private float CalculatePotOdds(int callAmount, int pot)
    {
        if (callAmount == 0) return 1.0f; // Free to check
        
        int totalPot = pot + callAmount;
        if (totalPot == 0) return 0f;
        
        return (float)callAmount / totalPot;
    }
    
    private float CalculateHandStrength(Player player)
    {
        // Normalize hand rank to 0.0-1.0 scale
        float baseStrength = (float)player.handRank / 9.0f; // 9 is highest (RoyalFlush)
        
        // Adjust based on hand value for same rank (e.g., pair of Aces vs pair of 3s)
        float valueModifier = 0f;
        
        if (player.handRank == HandRank.OnePair)
        {
            // Pair rank affects strength (Ace pair = 14, 3 pair = 3)
            int pairRank = (player.handValue / 1000000) % 100;
            valueModifier = (pairRank - 3) / 12.0f * 0.1f; // Up to 10% modifier
        }
        else if (player.handRank == HandRank.TwoPair)
        {
            // High pair rank
            int highPair = (player.handValue / 10000) % 100;
            valueModifier = (highPair - 3) / 12.0f * 0.1f;
        }
        else if (player.handRank == HandRank.ThreeOfAKind)
        {
            // Three of a kind rank
            int threeRank = (player.handValue / 10000) % 100;
            valueModifier = (threeRank - 3) / 12.0f * 0.15f;
        }
        else if (player.handRank == HandRank.HighCard)
        {
            // High card value
            int highCard = player.handValue / 100000000;
            valueModifier = (highCard - 3) / 12.0f * 0.05f;
        }
        
        return Mathf.Clamp01(baseStrength + valueModifier);
    }
    
    private bool ShouldBluff(Player cpuPlayer, int currentBet, float potOdds)
    {
        // Bluff conditions:
        // 1. Weak hand (High Card or weak pair)
        // 2. Small pot relative to bet
        // 3. Random chance based on bluff frequency
        // 4. Not too many chips at risk
        
        bool weakHand = cpuPlayer.handRank <= HandRank.OnePair;
        bool smallBet = currentBet <= 15; // Increased from 10
        bool goodBluffSpot = potOdds < 0.35f && currentBet <= cpuPlayer.tokenAmount * 0.35f; // More lenient
        bool randomBluff = Random.Range(0f, 1f) < bluffFrequency;
        
        // Adjust bluff frequency based on success rate
        float adjustedBluffFreq = bluffFrequency;
        if (roundsPlayed > 5 && bluffsAttempted > 0)
        {
            float successRate = (float)successfulBluffs / bluffsAttempted;
            if (successRate < 0.3f)
            {
                adjustedBluffFreq *= 0.8f; // Less reduction (was 0.7f)
            }
            else if (successRate > 0.6f)
            {
                adjustedBluffFreq *= 1.3f; // More increase if working (was 1.2f)
            }
        }
        
        return weakHand && (smallBet || goodBluffSpot) && Random.Range(0f, 1f) < adjustedBluffFreq;
    }
}


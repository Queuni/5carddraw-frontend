using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HandEvaluator
{
    public static void EvaluateHand(Player player)
    {
        if (player.playerHand == null || player.playerHand.Count != 5)
        {
            player.handRank = HandRank.HighCard;
            player.handValue = 0;
            return;
        }

        List<Card> sortedHand = Rules.SortCards(new List<Card>(player.playerHand));
        
        // Check for Royal Flush
        if (IsRoyalFlush(sortedHand))
        {
            player.handRank = HandRank.RoyalFlush;
            player.handValue = 9000000;
            return;
        }

        // Check for Straight Flush
        int straightFlushValue = GetStraightFlushValue(sortedHand);
        if (straightFlushValue > 0)
        {
            player.handRank = HandRank.StraightFlush;
            player.handValue = 8000000 + straightFlushValue;
            return;
        }

        // Check for Four of a Kind
        int fourOfAKindValue = GetFourOfAKindValue(sortedHand);
        if (fourOfAKindValue > 0)
        {
            player.handRank = HandRank.FourOfAKind;
            player.handValue = 7000000 + fourOfAKindValue;
            return;
        }

        // Check for Full House
        int fullHouseValue = GetFullHouseValue(sortedHand);
        if (fullHouseValue > 0)
        {
            player.handRank = HandRank.FullHouse;
            player.handValue = 6000000 + fullHouseValue;
            return;
        }

        // Check for Flush
        int flushValue = GetFlushValue(sortedHand);
        if (flushValue > 0)
        {
            player.handRank = HandRank.Flush;
            player.handValue = 5000000 + flushValue;
            return;
        }

        // Check for Straight
        int straightValue = GetStraightValue(sortedHand);
        if (straightValue > 0)
        {
            player.handRank = HandRank.Straight;
            player.handValue = 4000000 + straightValue;
            return;
        }

        // Check for Three of a Kind
        int threeOfAKindValue = GetThreeOfAKindValue(sortedHand);
        if (threeOfAKindValue > 0)
        {
            player.handRank = HandRank.ThreeOfAKind;
            player.handValue = 3000000 + threeOfAKindValue;
            return;
        }

        // Check for Two Pair
        int twoPairValue = GetTwoPairValue(sortedHand);
        if (twoPairValue > 0)
        {
            player.handRank = HandRank.TwoPair;
            player.handValue = 2000000 + twoPairValue;
            return;
        }

        // Check for One Pair
        int onePairValue = GetOnePairValue(sortedHand);
        if (onePairValue > 0)
        {
            player.handRank = HandRank.OnePair;
            player.handValue = 1000000 + onePairValue;
            return;
        }

        // High Card - use multiplier 15 to ensure value < 1,000,000 (One Pair base)
        player.handRank = HandRank.HighCard;
        player.handValue = GetHighCardValue(sortedHand);
    }

    private static bool IsRoyalFlush(List<Card> hand)
    {
        if (!IsFlush(hand)) return false;
        
        List<int> ranks = hand.Select(c => c.rank).OrderBy(r => r).ToList();
        // Check for 10, J, Q, K, A (14)
        return ranks[0] == 10 && ranks[1] == 11 && ranks[2] == 12 && 
               ranks[3] == 13 && ranks[4] == 14;
    }

    private static int GetStraightFlushValue(List<Card> hand)
    {
        if (!IsFlush(hand)) return 0;
        return GetStraightValue(hand);
    }

    private static int GetFourOfAKindValue(List<Card> hand)
    {
        var rankGroups = hand.GroupBy(c => c.rank);
        var fourOfAKind = rankGroups.FirstOrDefault(g => g.Count() == 4);
        if (fourOfAKind == null) return 0;
        
        int fourRank = fourOfAKind.Key;
        int kicker = rankGroups.First(g => g.Key != fourRank).Key;
        
        // Normalize ranks (Two = 15 becomes 2)
        int normalizedFourRank = NormalizeRankForComparison(fourRank);
        int normalizedKicker = NormalizeRankForComparison(kicker);
        
        return normalizedFourRank * 100 + normalizedKicker;
    }

    private static int GetFullHouseValue(List<Card> hand)
    {
        var rankGroups = hand.GroupBy(c => c.rank);
        var threeOfAKind = rankGroups.FirstOrDefault(g => g.Count() == 3);
        var pair = rankGroups.FirstOrDefault(g => g.Count() == 2);
        
        if (threeOfAKind == null || pair == null) return 0;
        
        // Normalize ranks (Two = 15 becomes 2)
        int normalizedThreeRank = NormalizeRankForComparison(threeOfAKind.Key);
        int normalizedPairRank = NormalizeRankForComparison(pair.Key);
        
        return normalizedThreeRank * 100 + normalizedPairRank;
    }

    private static bool IsFlush(List<Card> hand)
    {
        Suit firstSuit = hand[0].suit;
        return hand.All(c => c.suit == firstSuit);
    }

    private static int GetFlushValue(List<Card> hand)
    {
        if (!IsFlush(hand)) return 0;
        
        // Normalize ranks (Two = 15 becomes 2). Use multiplier 15 to ensure value < 1,000,000 (max: 755,510)
        List<int> normalizedRanks = hand.Select(c => NormalizeRankForComparison(c.rank))
                                        .OrderByDescending(r => r)
                                        .ToList();
        int value = 0;
        for (int i = 0; i < 5; i++)
        {
            value = value * 15 + normalizedRanks[i];
        }
        return value;
    }

    private static int GetStraightValue(List<Card> hand)
    {
        // Normalize ranks (Two = 15 becomes 2) for straight detection
        List<int> normalizedRanks = hand.Select(c => NormalizeRankForComparison(c.rank))
                                        .OrderBy(r => r)
                                        .ToList();
        
        bool isStraight = true;
        for (int i = 1; i < 5; i++)
        {
            if (normalizedRanks[i] != normalizedRanks[i - 1] + 1)
            {
                isStraight = false;
                break;
            }
        }
        
        // Check for wheel (A-2-3-4-5): normalized ranks 2,3,4,5,14
        bool isWheel = normalizedRanks[0] == 2 && normalizedRanks[1] == 3 && 
                       normalizedRanks[2] == 4 && normalizedRanks[3] == 5 && 
                       normalizedRanks[4] == 14;
        
        if (isStraight)
        {
            return normalizedRanks[4]; // Highest card
        }
        else if (isWheel)
        {
            return 5; // Wheel is lowest straight
        }
        
        return 0;
    }

    private static int GetThreeOfAKindValue(List<Card> hand)
    {
        var rankGroups = hand.GroupBy(c => c.rank);
        var threeOfAKind = rankGroups.FirstOrDefault(g => g.Count() == 3);
        if (threeOfAKind == null) return 0;
        
        int threeRank = threeOfAKind.Key;
        List<int> kickers = rankGroups.Where(g => g.Key != threeRank)
                                      .Select(g => g.Key)
                                      .OrderByDescending(r => r)
                                      .ToList();
        
        // Normalize ranks (Two = 15 becomes 2)
        int normalizedThreeRank = NormalizeRankForComparison(threeRank);
        int normalizedKicker1 = NormalizeRankForComparison(kickers[0]);
        int normalizedKicker2 = NormalizeRankForComparison(kickers[1]);
        
        return normalizedThreeRank * 10000 + normalizedKicker1 * 100 + normalizedKicker2;
    }

    // Normalize rank: Two (15) becomes 2 for poker comparison
    private static int NormalizeRankForComparison(int rank)
    {
        return rank == 15 ? 2 : rank;
    }

    private static int GetTwoPairValue(List<Card> hand)
    {
        var rankGroups = hand.GroupBy(c => c.rank);
        var pairs = rankGroups.Where(g => g.Count() == 2).ToList();
        
        if (pairs.Count != 2) return 0;
        
        // Normalize ranks (Two = 15 becomes 2), order by normalized rank descending
        var normalizedPairs = pairs.Select(g => new { OriginalRank = g.Key, NormalizedRank = NormalizeRankForComparison(g.Key), Group = g })
                                   .OrderByDescending(p => p.NormalizedRank)
                                   .ToList();
        
        int highPair = normalizedPairs[0].OriginalRank;
        int lowPair = normalizedPairs[1].OriginalRank;
        int kicker = rankGroups.First(g => g.Count() == 1).Key;
        
        // Normalize ranks (Two = 15 becomes 2)
        int normalizedHighPair = NormalizeRankForComparison(highPair);
        int normalizedLowPair = NormalizeRankForComparison(lowPair);
        int normalizedKicker = NormalizeRankForComparison(kicker);
        
        return normalizedHighPair * 10000 + normalizedLowPair * 100 + normalizedKicker;
    }

    private static int GetOnePairValue(List<Card> hand)
    {
        var rankGroups = hand.GroupBy(c => c.rank);
        var pair = rankGroups.FirstOrDefault(g => g.Count() == 2);
        if (pair == null) return 0;
        
        int pairRank = pair.Key;
        List<int> kickers = rankGroups.Where(g => g.Key != pairRank)
                                      .Select(g => g.Key)
                                      .OrderByDescending(r => r)
                                      .ToList();
        
        // Normalize ranks (Two = 15 becomes 2). Multipliers ensure value < 1,000,000 (max: ~140,000)
        int normalizedPairRank = NormalizeRankForComparison(pairRank);
        int normalizedKicker1 = NormalizeRankForComparison(kickers[0]);
        int normalizedKicker2 = NormalizeRankForComparison(kickers[1]);
        int normalizedKicker3 = NormalizeRankForComparison(kickers[2]);
        
        return normalizedPairRank * 10000 + normalizedKicker1 * 1000 + normalizedKicker2 * 100 + normalizedKicker3;
    }

    private static int GetHighCardValue(List<Card> hand)
    {
        // Normalize ranks (Two = 15 becomes 2). Multiplier 15 ensures value < 1,000,000 (max: 755,510)
        List<int> normalizedRanks = hand.Select(c => NormalizeRankForComparison(c.rank))
                                        .OrderByDescending(r => r)
                                        .ToList();
        int value = 0;
        for (int i = 0; i < 5; i++)
        {
            value = value * 15 + normalizedRanks[i];
        }
        return value;
    }

    public static int CompareHands(Player player1, Player player2)
    {
        EvaluateHand(player1);
        EvaluateHand(player2);
        
        if (player1.handValue > player2.handValue) return 1;
        if (player1.handValue < player2.handValue) return -1;
        return 0; // Tie
    }
}


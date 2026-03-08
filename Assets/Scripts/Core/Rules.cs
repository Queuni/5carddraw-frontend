using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Rules
{
    public static List<Card> SortCards(List<Card> cards)
    {
        if (cards == null)
        {
            Debug.LogError("[Rules.SortCards] ERROR - cards is null");
            return new List<Card>();
        }

        try
        {
            var sorted = cards.OrderBy(c => c.rank).ThenByDescending(c => Utils.SuitToInt(c.suit)).ToList();
            return sorted;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.SortCards] ERROR: {ex.Message}");
            return new List<Card>();
        }
    }
}
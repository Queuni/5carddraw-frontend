using System.Collections.Generic;
using UnityEngine;

public static class DealManager
{

    public static void Deal(GameState gameState)
    {
        switch (gameState.gameMode)
        {
            case GameMode.SingleMode: // SingleMode
                DealSingleMode(gameState);
                break;
            case GameMode.MultiMode: // MultiMode
                // Future implementation for MultiMode
                break;
            default:
                Debug.LogError("⚠ Unknown game mode: " + gameState.gameMode);
                break;
        }
    }

    private static void DealSingleMode(GameState gameState)
    {
        gameState.deck.Clear();

        foreach (var entry in ResourceManager.frontSpriteMap)
        {
            // Expect file names like "3_of_spades", "king_of_hearts"
            string[] parts = entry.Key.Split('_');

            int rank = Utils.ParseRank(parts[0]);
            Suit suit = Utils.ParseSuit(parts[2]);

            gameState.deck.Add(new Card(rank, suit, entry.Value, ResourceManager.backSprite));
        }

        Shuffle(gameState.deck);

        for (int i = 0; i < Constants.CARDS_PER_PLAYER; i++)
        {
            foreach (Player player in gameState.playerList)
            {
                if (gameState.deck.Count == 0)
                {
                    Debug.LogError("❌ Not enough cards to deal!");
                    return;
                }
                Card dealtCard = gameState.deck[0];
                gameState.deck.RemoveAt(0);
                player.AddCardToHand(dealtCard);
            }
        }

        // Sort each player's hand unless auto sort is disabled
        if (GameSettings.CardAutoSort)
        {
            foreach (Player player in gameState.playerList)
            {
                Rules.SortCards(player.playerHand);
            }
        }
    }

    public static void ExchangeCards(Player player, List<Card> cardsToExchange, GameState gameState)
    {
        if (cardsToExchange == null || cardsToExchange.Count == 0)
        {
            return;
        }

        // Limit to max exchange
        int exchangeCount = Mathf.Min(cardsToExchange.Count, Constants.MAX_EXCHANGE_CARDS);
        
        // Remove selected cards from hand
        for (int i = 0; i < exchangeCount; i++)
        {
            Card card = cardsToExchange[i];
            player.RemoveCardFromHand(card);
        }

        // Deal new cards
        for (int i = 0; i < exchangeCount; i++)
        {
            if (gameState.deck.Count == 0)
            {
                Debug.LogError("❌ Not enough cards in deck for exchange!");
                return;
            }
            Card newCard = gameState.deck[0];
            gameState.deck.RemoveAt(0);
            player.AddCardToHand(newCard);
        }

        // Note: Hand will be sorted by visual positions in CardAnimator after exchange animation
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

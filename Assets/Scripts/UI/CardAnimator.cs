using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardAnimator: MonoBehaviour
{
    public GameObject cardPrefab;
    public GameObject player0HandCenter;

    [Header("Card Size")]
    [Tooltip("Card scale factor. Automatically set from cardPrefab's scale.")]
    private float cardScale = 1.0f;

    [Header("Multiplayer - Other Players")]
    [Tooltip("Scale multiplier for other players' hands (local player uses cardScale * 1.3). e.g. 0.7 = smaller, 1.0 = same as base.")]
    [Range(0.3f, 1.5f)]
    public float otherPlayersHandScale = 1.2f;

    private float GetDuration(float baseDuration)
    {
        return GameSettings.GetAnimationDuration(baseDuration);
    }

    /// <summary>
    /// Hides the player's hand (e.g. when they fold). Card objects are deactivated so the hand is no longer visible.
    /// </summary>
    public void HidePlayerHand(Player player)
    {
        if (player == null || player.playerHand == null) return;
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                card.cardObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        // Read the scale from cardPrefab (Dealer object) and use it for all instantiated cards
        if (cardPrefab != null)
        {
            // Get the scale value (assuming uniform scaling, use x component)
            cardScale = cardPrefab.transform.localScale.x;
            Debug.Log($"📏 Card scale set to {cardScale} from Dealer object '{cardPrefab.name}'");
        }
        else
        {
            Debug.LogWarning("⚠️ cardPrefab is null - using default scale of 1.0");
        }
    }

    public IEnumerator DealAnimator(GameState state)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("❌ Card prefab is not assigned in CardAnimator!");
            yield break;
        }

        for (int i = 0; i < Constants.CARDS_PER_PLAYER; i++)
        {
            for (int j = 0; j < state.playerList.Count; j++)
            {
                Player player = state.playerList[j];
                Card card = player.playerHand[i];
                GameObject cardObject = Instantiate(cardPrefab, cardPrefab.transform.position, cardPrefab.transform.rotation);

                Image frontImage = cardObject.transform.Find("Front").GetComponent<Image>();
                frontImage.sprite = card.frontSprite;
                frontImage.enabled = false; // Initially hide front

                Image backImage = cardObject.transform.Find("Back").GetComponent<Image>();
                backImage.enabled = true;
                backImage.sprite = card.backSprite;

                card.cardObject = cardObject;

                Transform parentTransform = player.handCenter != null ? player.handCenter : player.gameObject.transform;
                cardObject.transform.SetParent(parentTransform, true);
                cardObject.transform.localScale = Vector3.one * cardScale; // Apply card scale from Dealer object
                cardObject.transform.DOMove(player.handCenter.position, GetDuration(0.5f)).SetEase(Ease.OutQuad);

                // Add animation logic here (e.g., move card to player's hand)
                yield return new WaitForSeconds(GetDuration(0.1f)); // Wait before dealing the next card
            }
        }


        // For multiplayer, scale other players' hands then move local player's cards to player0HandCenter and scale up
        if (state.gameMode == GameMode.MultiMode)
        {
            Player localPlayer = state.playerList.Find(p => p != null && !p.isCPU);
            float otherScale = cardScale * otherPlayersHandScale;
            foreach (Player player in state.playerList)
            {
                if (player == null || player == localPlayer) continue;
                foreach (Card card in player.playerHand)
                {
                    if (card?.cardObject != null)
                        card.cardObject.transform.localScale = Vector3.one * otherScale;
                }
            }
            if (player0HandCenter != null && localPlayer != null)
            {
                foreach (Card card in localPlayer.playerHand)
                {
                    if (card?.cardObject == null) continue;
                    card.cardObject.transform.localScale = Vector3.one * (cardScale * 1.3f);
                    card.cardObject.transform.DOMove(player0HandCenter.transform.position, GetDuration(0.4f)).SetEase(Ease.OutQuad);
                }
            }
        }

        yield return new WaitForSeconds(GetDuration(0.5f)); // Wait before spreading cards

        // After dealing all cards, spread them out
        StartCoroutine(SpreadCards(state));
    }

    public IEnumerator SpreadCards(GameState state)
    {
        for (int p = 0; p < state.playerList.Count; p++)
        {
            Player player = state.playerList[p];
            if (state.gameMode == GameMode.MultiMode && player != null && player.playerIndex != 0)
            {
                continue;
            }
            
            Transform spreadCenter = player.handCenter;
            if (state.gameMode == GameMode.MultiMode && player0HandCenter != null && player != null && !player.isCPU)
            {
                spreadCenter = player0HandCenter.transform;
            }

            // Cross-platform responsive spacing calculation
            float spacing = CalculateCardSpacing(spreadCenter);
            if (state.gameMode == GameMode.MultiMode && player.playerIndex != 0)
            {
                spacing *= 0.5f; // tighter spacing for other players in showdown
            }
            int count = player.playerHand.Count;
            float totalWidth = (count - 1) * spacing;
            Vector3 start = spreadCenter.position - new Vector3(totalWidth / 2f, 0, 0);

            for (int i = 0; i < count; i++)
            {
                Card card = player.playerHand[i];
                GameObject cardObject = card.cardObject;
                if (cardObject == null) continue;
                
                cardObject.transform.SetSiblingIndex(i);
                Vector3 pos = start + new Vector3(i * spacing, 0, 0);
                cardObject.transform.DOMove(pos, GetDuration(0.6f)).SetEase(Ease.OutQuad);

                if (player.isCPU == false)
                {
                    // Flip the card to show front for human player
                    StartCoroutine(FlipCards(card, cardObject));

                    // Add click handler for selection (works for both mouse and touch)
                    CardClickHandler clickHandler = cardObject.GetComponent<CardClickHandler>();
                    if (clickHandler == null)
                    {
                        clickHandler = cardObject.AddComponent<CardClickHandler>();
                    }
                    clickHandler.card = card;
                    clickHandler.player = player;
                }
            }

            yield return new WaitForSeconds(GetDuration(0.1f)); // Slight delay between each card animation
        }
    }

    public IEnumerator SpreadCardsForAll(GameState state)
    {
        for (int p = 0; p < state.playerList.Count; p++)
        {
            Player player = state.playerList[p];
            if (player == null || player.handCenter == null)
            {
                continue;
            }

            Transform spreadCenter = player.handCenter;
            if (state.gameMode == GameMode.MultiMode && player0HandCenter != null && player.playerIndex == 0)
            {
                spreadCenter = player0HandCenter.transform;
            }

            float spacing = CalculateCardSpacing(spreadCenter);
            if (state.gameMode == GameMode.MultiMode && player.playerIndex != 0)
            {
                spacing *= 0.5f; // tighter spacing for other players in showdown
            }
            int count = player.playerHand.Count;
            float totalWidth = (count - 1) * spacing;
            Vector3 start = spreadCenter.position - new Vector3(totalWidth / 2f, 0, 0);

            for (int i = 0; i < count; i++)
            {
                Card card = player.playerHand[i];
                GameObject cardObject = card.cardObject;
                if (cardObject == null) continue;

                cardObject.transform.SetSiblingIndex(i);
                Vector3 pos = start + new Vector3(i * spacing, 0, 0);
                cardObject.transform.DOMove(pos, GetDuration(0.6f)).SetEase(Ease.OutQuad);

                StartCoroutine(FlipCards(card, cardObject));
            }

            yield return new WaitForSeconds(GetDuration(0.1f));
        }
    }

    private float CalculateCardSpacing(Transform handCenter)
    { 
        // Try to get Canvas for responsive sizing
        Canvas canvas = handCenter.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                float canvasWidth = canvasRect.rect.width;
                // For 1080px panel with 5 cards, use smaller divisor for tighter spacing
                // Account for card scale
                return (canvasWidth / 70f) * cardScale;
            }
        }
        
        // Fallback: Use fixed spacing based on 1080px background panel
        // Account for card scale
        #if UNITY_ANDROID || UNITY_IOS
        return 60f * cardScale; // Fixed spacing for mobile (in pixels)
        #else
        return 70f * cardScale; // Fixed spacing for desktop/WebGL (works well with 1080px panel)
        #endif
    }

    public IEnumerator FlipCards(Card card, GameObject cardObject)
    {
        Image frontImage = cardObject.transform.Find("Front").GetComponent<Image>();
        Image backImage = cardObject.transform.Find("Back").GetComponent<Image>();

        Sequence seq = DOTween.Sequence();
        seq.Append(cardObject.transform.DORotate(new Vector3(0, 90, 0), GetDuration(0.15f)));

        // WebGL-safe callback
        Image safeBack = backImage;
        Image safeFront = frontImage;
        seq.AppendCallback(() =>
        {
            try
            {
                if (safeBack != null) safeBack.enabled = false;
                if (safeFront != null) safeFront.enabled = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WebGL: Error in card flip callback: {e.Message}");
            }
        });
        seq.Append(cardObject.transform.DORotate(Vector3.zero, GetDuration(0.15f)));

        seq.onComplete += () =>
        {
            // Ensure final rotation is set correctly
            cardObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            card.originalYPosition = cardObject.transform.position.y;
        };

        yield return seq.WaitForCompletion();
    }
    
    public IEnumerator FlipCardsAndCount(Card card, GameObject cardObject, System.Action onComplete)
    {
        yield return StartCoroutine(FlipCards(card, cardObject));
        if (onComplete != null)
        {
            onComplete();
        }
    }

    public IEnumerator ExchangeCardsAnimation(Player player, List<Card> cardsToExchange, Transform handCenter, Dictionary<Card, Vector3> cardPositionsBeforeExchange = null)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("❌ Card prefab is not assigned in CardAnimator!");
            yield break;
        }

        // Get dealer position (where cards are dealt from)
        Vector3 dealerPosition = cardPrefab.transform.position;

        // Step 1: Use stored positions or get current positions
        Dictionary<Card, Vector3> originalCardPositions = cardPositionsBeforeExchange ?? new Dictionary<Card, Vector3>();
        List<Vector3> exchangePositions = new List<Vector3>(); // Store positions where cards are being exchanged
        List<GameObject> cardsToRemove = new List<GameObject>();
        List<Card> cardsToRemoveData = new List<Card>();
        
        // If positions weren't provided, get them from current card objects
        if (cardPositionsBeforeExchange == null)
        {
            foreach (Card card in player.playerHand)
            {
                if (card != null && card.cardObject != null)
                {
                    originalCardPositions[card] = card.cardObject.transform.position;
                }
            }
        }
        
        // Find cards to exchange by their cardObject references
        foreach (Card exchangeCard in cardsToExchange)
        {
            if (exchangeCard != null && exchangeCard.cardObject != null)
            {
                // Use stored position if available, otherwise use current position
                Vector3 cardPos = originalCardPositions.ContainsKey(exchangeCard) 
                    ? originalCardPositions[exchangeCard] 
                    : exchangeCard.cardObject.transform.position;
                
                // If card was selected (raised), use originalYPosition for correct y coordinate
                // This ensures new cards are placed at the normal (unraised) position
                if (exchangeCard.originalYPosition > 0)
                {
                    cardPos.y = exchangeCard.originalYPosition;
                }
                
                exchangePositions.Add(cardPos);
                cardsToRemove.Add(exchangeCard.cardObject);
                cardsToRemoveData.Add(exchangeCard);
            }
        }
        
        // Update remaining card positions if not already stored
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null && !originalCardPositions.ContainsKey(card))
            {
                originalCardPositions[card] = card.cardObject.transform.position;
            }
        }

        // Step 2: Animate exchanging cards disappearing
        foreach (GameObject cardObj in cardsToRemove)
        {
            if (cardObj != null)
            {
                // Add CanvasGroup if it doesn't exist for fade effect
                CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = cardObj.AddComponent<CanvasGroup>();
                }

                // Fade out and move up/away
                Sequence disappearSeq = DOTween.Sequence();
                disappearSeq.Append(cardObj.transform.DOMoveY(cardObj.transform.position.y + 150f, GetDuration(0.4f)).SetEase(Ease.InBack));
                disappearSeq.Join(cardObj.transform.DOScale(0.3f, GetDuration(0.4f)).SetEase(Ease.InBack));
                disappearSeq.Join(canvasGroup.DOFade(0f, GetDuration(0.4f)));
            }
        }

        // Wait for disappear animation
        yield return new WaitForSeconds(GetDuration(0.4f));

        // Step 3: Destroy old card objects that were exchanged
        foreach (GameObject cardObj in cardsToRemove)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }

        // Clear card references for exchanged cards
        foreach (Card card in cardsToRemoveData)
        {
            card.cardObject = null;
        }

        yield return new WaitForSeconds(GetDuration(0.2f));

        // Step 4: After DealManager.ExchangeCards, the hand is re-sorted
        // Remaining cards should stay in their original positions
        // New cards need to be placed at the exchange positions
        
        // Get the correct y position from remaining cards or handCenter
        float correctYPosition = handCenter.position.y;
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                // Use y position from first remaining card (which wasn't raised)
                correctYPosition = card.cardObject.transform.position.y;
                break;
            }
        }
        
        // Ensure remaining cards stay in their original positions (they might have moved slightly)
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null && originalCardPositions.ContainsKey(card))
            {
                // Keep remaining card at its original position
                Vector3 originalPos = originalCardPositions[card];
                card.cardObject.transform.position = originalPos;
                card.originalYPosition = originalPos.y;
                // Update correctYPosition from remaining card
                correctYPosition = originalPos.y;
            }
        }

        // Step 5: Create and animate new cards to fill exchange positions
        List<Card> newCards = new List<Card>();
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject == null)
            {
                // This is a new card (no cardObject yet)
                newCards.Add(card);
            }
        }

        // Fallback: position extra new cards to the right of all current cards (avoid overlap)
        float spacing = handCenter != null ? CalculateCardSpacing(handCenter) : 70f * cardScale;
        float rightmostX = handCenter.position.x;
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                float x = card.cardObject.transform.position.x;
                if (x > rightmostX) rightmostX = x;
            }
        }

        // Animate new cards from dealer position to exchange positions (create object for every new card)
        for (int i = 0; i < newCards.Count; i++)
        {
            Card newCard = newCards[i];
            Vector3 targetPos;
            if (i < exchangePositions.Count)
            {
                targetPos = exchangePositions[i];
            }
            else
            {
                int extraIndex = i - exchangePositions.Count;
                targetPos = new Vector3(rightmostX + (extraIndex + 1) * spacing, correctYPosition, handCenter.position.z);
            }
            targetPos.y = correctYPosition;

            // Create card at dealer position
            GameObject cardObject = Instantiate(cardPrefab, dealerPosition, cardPrefab.transform.rotation);

            Transform frontT = cardObject.transform.Find("Front");
            if (frontT != null)
            {
                Image frontImage = frontT.GetComponent<Image>();
                if (frontImage != null)
                {
                    frontImage.sprite = newCard.frontSprite;
                    frontImage.enabled = false; // Start with back showing
                }
            }

            Transform backT = cardObject.transform.Find("Back");
            if (backT != null)
            {
                Image backImage = backT.GetComponent<Image>();
                if (backImage != null)
                {
                    backImage.enabled = true;
                    backImage.sprite = newCard.backSprite;
                }
            }

            newCard.cardObject = cardObject;
            newCard.originalYPosition = correctYPosition;

            Transform parentTransform = handCenter != null ? handCenter : player.gameObject.transform;
            cardObject.transform.SetParent(parentTransform, true);
            float exchangeScale = cardScale;
            if (player0HandCenter != null)
            {
                if (!player.isCPU)
                    exchangeScale = cardScale * 1.3f;
                else
                    exchangeScale = cardScale * otherPlayersHandScale;
            }
            cardObject.transform.localScale = Vector3.one * exchangeScale;

            // Animate card from dealer position to its target position
            cardObject.transform.DOMove(targetPos, GetDuration(0.5f)).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(GetDuration(0.1f));
        }

        // Wait for all new cards to reach their positions
        yield return new WaitForSeconds(GetDuration(0.4f));

        // Step 6: Flip new cards to front for human player (simultaneously)
        if (!player.isCPU && newCards.Count > 0)
        {
            // Start all flip animations simultaneously
            int activeFlips = 0;
            foreach (Card newCard in newCards)
            {
                if (newCard != null && newCard.cardObject != null)
                {
                    activeFlips++;
                    StartCoroutine(FlipCardsAndCount(newCard, newCard.cardObject, () => activeFlips--));
                }
            }
            
            // Wait for all flips to complete
            while (activeFlips > 0)
            {
                yield return null;
            }
        }
        
        // Add click handlers for all new cards
        foreach (Card newCard in newCards)
        {
            if (newCard != null && newCard.cardObject != null)
            {
                CardClickHandler clickHandler = newCard.cardObject.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = newCard.cardObject.AddComponent<CardClickHandler>();
                }
                clickHandler.card = newCard;
                clickHandler.player = player;
            }
        }
        
        // Final step: Sort hand to match visual card positions (left to right)
        SortHandByVisualPositions(player);
        
        // Set sibling indices to match sorted hand order
        for (int i = 0; i < player.playerHand.Count; i++)
        {
            Card card = player.playerHand[i];
            if (card != null && card.cardObject != null)
            {
                card.cardObject.transform.SetSiblingIndex(i);
            }
        }

        // Reposition all cards into a uniform spread to avoid overlap
        Transform spreadCenter = (player0HandCenter != null && !player.isCPU) ? player0HandCenter.transform : handCenter;
        float spreadSpacing = CalculateCardSpacing(spreadCenter);
        int handCount = player.playerHand.Count;
        float totalWidth = (handCount - 1) * spreadSpacing;
        Vector3 spreadStart = spreadCenter.position - new Vector3(totalWidth / 2f, 0, 0);
        for (int i = 0; i < handCount; i++)
        {
            Card card = player.playerHand[i];
            if (card != null && card.cardObject != null)
            {
                Vector3 pos = spreadStart + new Vector3(i * spreadSpacing, 0, 0);
                card.cardObject.transform.DOMove(pos, GetDuration(0.25f)).SetEase(Ease.OutQuad);
            }
        }
        yield return new WaitForSeconds(GetDuration(0.15f));
    }

    /// <summary>
    /// WebGL-safe: move a card from one position to another over time (no DOTween).
    /// </summary>
    private IEnumerator MoveCardLerp(GameObject cardObj, Vector3 fromPos, Vector3 toPos, float duration)
    {
        if (cardObj == null || duration <= 0f)
        {
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cardObj.transform.position = Vector3.Lerp(fromPos, toPos, t);
            yield return null;
        }
        cardObj.transform.position = toPos;
    }
    
    /// <summary>
    /// Sort player's hand to match visual card positions (left to right).
    /// Cards without a cardObject are kept in hand and sorted to the end (never dropped).
    /// </summary>
    private void SortHandByVisualPositions(Player player)
    {
        if (player == null || player.playerHand == null) return;
        
        List<System.Tuple<Card, float>> cardsWithPositions = new List<System.Tuple<Card, float>>();
        float maxX = float.MinValue;
        
        foreach (Card card in player.playerHand)
        {
            if (card == null) continue;
            if (card.cardObject != null)
            {
                float xPos = card.cardObject.transform.position.x;
                cardsWithPositions.Add(new System.Tuple<Card, float>(card, xPos));
                if (xPos > maxX) maxX = xPos;
            }
        }
        
        int noObjectIndex = 0;
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject == null)
            {
                cardsWithPositions.Add(new System.Tuple<Card, float>(card, maxX + 1f + noObjectIndex));
                noObjectIndex++;
            }
        }
        
        cardsWithPositions.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        
        player.playerHand.Clear();
        foreach (var tuple in cardsWithPositions)
        {
            player.playerHand.Add(tuple.Item1);
        }
    }

    private IEnumerator FlipToBack(Card card, GameObject cardObject)
    {
        Image frontImage = cardObject.transform.Find("Front").GetComponent<Image>();
        Image backImage = cardObject.transform.Find("Back").GetComponent<Image>();

        if (frontImage == null || backImage == null)
        {
            yield break;
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(cardObject.transform.DORotate(new Vector3(0, 90, 0), GetDuration(0.15f)));

        // WebGL-safe callback
        Image safeBack = backImage;
        Image safeFront = frontImage;
        seq.AppendCallback(() =>
        {
            try
            {
                if (safeFront != null) safeFront.enabled = false;
                if (safeBack != null) safeBack.enabled = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WebGL: Error in flip to back callback: {e.Message}");
            }
        });
        seq.Append(cardObject.transform.DORotate(Vector3.zero, GetDuration(0.15f)));

        seq.onComplete += () =>
        {
            // Ensure final rotation is set correctly
            cardObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        };

        yield return seq.WaitForCompletion();
    }

    /// <summary>
    /// WebGL-safe: exchange cards with no DOTween or waits to avoid engine crash (INVALID_ENUM / abort).
    /// Does destroy old objects, instantiate new ones at spread positions, set sprites and click handlers.
    /// </summary>
    public IEnumerator ExchangeCardsInstant(Player player, List<Card> cardsToExchange, Transform handCenter)
    {
        if (cardPrefab == null || player == null || handCenter == null)
        {
            yield break;
        }

        yield return null; // Run after current frame

        Vector3 dealerPosition = cardPrefab != null ? cardPrefab.transform.position : handCenter.position;
        Dictionary<Card, Vector3> originalCardPositions = new Dictionary<Card, Vector3>();
        List<Vector3> exchangePositions = new List<Vector3>();
        List<GameObject> cardsToRemove = new List<GameObject>();
        List<Card> cardsToRemoveData = new List<Card>();

        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                originalCardPositions[card] = card.cardObject.transform.position;
            }
        }

        foreach (Card exchangeCard in cardsToExchange)
        {
            if (exchangeCard != null && exchangeCard.cardObject != null)
            {
                Vector3 cardPos = exchangeCard.cardObject.transform.position;
                if (exchangeCard.originalYPosition > 0)
                {
                    cardPos.y = exchangeCard.originalYPosition;
                }
                exchangePositions.Add(cardPos);
                cardsToRemove.Add(exchangeCard.cardObject);
                cardsToRemoveData.Add(exchangeCard);
            }
        }

        foreach (GameObject cardObj in cardsToRemove)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        foreach (Card card in cardsToRemoveData)
        {
            card.cardObject = null;
        }

        float correctYPosition = handCenter.position.y;
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                correctYPosition = card.cardObject.transform.position.y;
                break;
            }
        }

        Transform spreadCenter = (player0HandCenter != null && !player.isCPU) ? player0HandCenter.transform : handCenter;
        float spacing = CalculateCardSpacing(spreadCenter);
        int count = player.playerHand.Count;
        float totalWidth = (count - 1) * spacing;
        Vector3 start = spreadCenter.position - new Vector3(totalWidth / 2f, 0, 0);

        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null && originalCardPositions.ContainsKey(card))
            {
                Vector3 p = originalCardPositions[card];
                p.y = correctYPosition;
                card.cardObject.transform.position = p;
                card.originalYPosition = correctYPosition;
            }
        }

        List<Card> newCards = new List<Card>();
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject == null)
            {
                newCards.Add(card);
            }
        }

        float dealDuration = 0.35f;
        for (int i = 0; i < newCards.Count; i++)
        {
            Card newCard = newCards[i];
            Vector3 targetPos;
            if (i < exchangePositions.Count)
            {
                targetPos = exchangePositions[i];
                targetPos.y = correctYPosition;
            }
            else
            {
                int placeIndex = count - newCards.Count + i;
                targetPos = start + new Vector3(placeIndex * spacing, 0, 0);
            }

            // Create at dealer position so they feel "dealt" when they move to the hand
            GameObject cardObject = Instantiate(cardPrefab, dealerPosition, cardPrefab.transform.rotation);

            Transform frontT = cardObject.transform.Find("Front");
            if (frontT != null)
            {
                Image frontImage = frontT.GetComponent<Image>();
                if (frontImage != null)
                {
                    frontImage.sprite = newCard.frontSprite;
                    frontImage.enabled = true;
                }
            }
            Transform backT = cardObject.transform.Find("Back");
            if (backT != null)
            {
                Image backImage = backT.GetComponent<Image>();
                if (backImage != null)
                {
                    backImage.sprite = newCard.backSprite;
                    backImage.enabled = false;
                }
            }

            newCard.cardObject = cardObject;
            newCard.originalYPosition = correctYPosition;

            Transform parentTransform = handCenter != null ? handCenter : player.gameObject.transform;
            cardObject.transform.SetParent(parentTransform, true);
            float exchangeScale = cardScale;
            if (player0HandCenter != null)
            {
                if (!player.isCPU)
                    exchangeScale = cardScale * 1.3f;
                else
                    exchangeScale = cardScale * otherPlayersHandScale;
            }
            cardObject.transform.localScale = Vector3.one * exchangeScale;

            CardClickHandler clickHandler = cardObject.GetComponent<CardClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = cardObject.AddComponent<CardClickHandler>();
            }
            clickHandler.card = newCard;
            clickHandler.player = player;

            yield return StartCoroutine(MoveCardLerp(cardObject, dealerPosition, targetPos, dealDuration));
        }

        SortHandByVisualPositions(player);
        for (int i = 0; i < player.playerHand.Count; i++)
        {
            Card card = player.playerHand[i];
            if (card != null && card.cardObject != null)
            {
                card.cardObject.transform.SetSiblingIndex(i);
            }
        }

        for (int i = 0; i < count; i++)
        {
            Card card = player.playerHand[i];
            GameObject cardObject = card != null ? card.cardObject : null;
            if (cardObject == null) continue;
            Vector3 pos = start + new Vector3(i * spacing, 0, 0);
            cardObject.transform.position = pos;
        }
    }

    public IEnumerator DealNewCards(Player player, int cardCount)
    {
        // Legacy method - use ExchangeCardsAnimation instead for better flow
        yield return StartCoroutine(ExchangeCardsAnimation(player, new List<Card>(), player.handCenter));
    }

    private IEnumerator SpreadCardsForPlayer(Player player)
    {
        float spacing = CalculateCardSpacing(player.handCenter);
        int count = player.playerHand.Count;
        float totalWidth = (count - 1) * spacing;
        Vector3 start = player.handCenter.position - new Vector3(totalWidth / 2f, 0, 0);

        for (int i = 0; i < count; i++)
        {
            Card card = player.playerHand[i];
            GameObject cardObject = card.cardObject;
            if (cardObject == null) continue;

            cardObject.transform.SetSiblingIndex(i);
            Vector3 pos = start + new Vector3(i * spacing, 0, 0);
            cardObject.transform.DOMove(pos, GetDuration(0.6f)).SetEase(Ease.OutQuad);

            if (!player.isCPU)
            {
                StartCoroutine(FlipCards(card, cardObject));
                
                CardClickHandler clickHandler = cardObject.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = cardObject.AddComponent<CardClickHandler>();
                }
                clickHandler.card = card;
                clickHandler.player = player;
            }
        }

        yield return new WaitForSeconds(GetDuration(0.2f));
    }
}

public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public Card card;
    public Player player;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (card == null || player == null)
        {
            Debug.LogError("Card or Player reference is missing in CardClickHandler.");
            return;
        }

        // Only allow selection for human players who haven't exchanged yet
        if (player.isCPU || player.hasExchanged)
        {
            return;
        }

        Vector3 position = card.cardObject.transform.position;
        if (card.isSelected)
        {
            // Deselect the card
            position.y = card.originalYPosition;
            card.isSelected = false;
            player.selectedCards.Remove(card);
        }
        else
        {
            // Check if we've reached the max selection limit
            if (player.selectedCards.Count >= Constants.MAX_EXCHANGE_CARDS)
            {
                return; // Can't select more cards
            }
            
            // Select the card
            position.y += Constants.CARD_RAISE_OFFSET;
            card.isSelected = true;
            player.selectedCards.Add(card);
        }
        card.cardObject.transform.DOMove(position, GameSettings.GetAnimationDuration(0.2f)).SetEase(Ease.OutQuad);
    }
}
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TableAnimator : MonoBehaviour
{
    [Header("Pot Animation")]
    public GameObject potGameObject;
    public Transform potCenterPosition; // Initial position where pot appears (center of table)
    
    [Header("Animation Settings")]
    public float potAnimationDuration = 1.0f;
    public Ease potAnimationEase = Ease.OutQuad;
    
    private Vector3 originalPotPosition;
    private bool potVisible = false;

    void Awake()
    {
        // Store original pot position
        if (potGameObject != null && potCenterPosition != null)
        {
            originalPotPosition = potCenterPosition.position;
        }
        
        // Initially hide pot (will be shown in ante phase)
        if (potGameObject != null)
        {
            potGameObject.SetActive(false);
            potVisible = false;
        }
    }
    
    void Start()
    {
        // Double-check pot is hidden at start
        if (potGameObject != null && potVisible)
        {
            potGameObject.SetActive(false);
            potVisible = false;
        }
    }

    /// <summary>
    /// Show the pot GameObject in the ante phase
    /// </summary>
    public void ShowPot()
    {
        if (potGameObject == null)
        {
            Debug.LogWarning("TableAnimator: potGameObject is not assigned!");
            return;
        }
        
        // Set position if potCenterPosition is provided
        if (potCenterPosition != null)
        {
            potGameObject.transform.position = potCenterPosition.position;
        }
        else
        {
            Debug.LogWarning("TableAnimator: potCenterPosition is not assigned! Pot will appear at current position.");
        }

        
        potGameObject.SetActive(true);
        potVisible = true;
    }

    /// <summary>
    /// Hide the pot GameObject (for new rounds)
    /// </summary>
    public void HidePot()
    {
        if (potGameObject != null)
        {
            potGameObject.SetActive(false);
            potVisible = false;
        }
    }

    /// <summary>
    /// Animate pot from center to winner's position, then hide it
    /// </summary>
    public IEnumerator AnimatePotToWinner(Player winner)
    {
        if (potGameObject == null || !potVisible)
        {
            yield break;
        }

        // No winner (e.g. all folded) — just hide the pot without animation.
        if (winner == null)
        {
            HidePot();
            yield break;
        }

        // Get winner's position (use avatar or a position marker on the player prefab)
        Vector3 targetPosition = GetPlayerPosition(winner);
        
        if (targetPosition == Vector3.zero)
        {
            // Fallback: use handCenter position
            if (winner.handCenter != null)
            {
                targetPosition = winner.handCenter.position;
            }
            else
            {
                Debug.LogWarning($"Cannot find position for winner {winner.playerName}");
                yield break;
            }
        }

        // Animate pot to winner's position
        float duration = GameSettings.GetAnimationDuration(potAnimationDuration);
        potGameObject.transform.DOMove(targetPosition, duration)
            .SetEase(potAnimationEase);

        // Wait for animation to complete
        yield return new WaitForSeconds(duration);

        // Hide pot at winner's position
        potGameObject.SetActive(false);
        potVisible = false;
    }

    /// <summary>
    /// Get the position where pot should move to for a player
    /// </summary>
    private Vector3 GetPlayerPosition(Player player)
    {
        if (player == null) return Vector3.zero;

        // Try to find a token position or avatar position on the player GameObject
        Transform playerTransform = player.transform;
        
        // Look for a "TokenPosition" or "AvatarPosition" child
        Transform tokenPos = playerTransform.Find("TokenPosition");
        if (tokenPos != null)
        {
            return tokenPos.position;
        }

        // Fallback: use avatar position if available
        if (player.avatarImage != null)
        {
            return player.avatarImage.transform.position;
        }

        // Final fallback: use player GameObject position
        return playerTransform.position;
    }
}

using UnityEngine;

public class Card
{
    public int rank;
    public Suit suit;
    public Sprite frontSprite;
    public Sprite backSprite;
    public GameObject cardObject;
    public bool isSelected = false;
    public float originalYPosition;

    public Card(int rank, Suit suit, Sprite frontSprite, Sprite backSprite)
    {
        this.rank = rank;
        this.suit = suit;
        this.frontSprite = frontSprite;
        this.backSprite = backSprite;
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}
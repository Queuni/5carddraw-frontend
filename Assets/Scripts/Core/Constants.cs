public enum GameMode { SingleMode, MultiMode }

public enum Suit { Spades = 0, Clubs, Diamonds, Hearts }
public enum Rank { Three = 3, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace, Two }

public enum HandRank
{
    HighCard = 0,
    OnePair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
    RoyalFlush = 9
}

public static class Constants
{
    public const int TOTAL_CARDS_NUM = 52;
    public const int CARDS_PER_PLAYER = 5;
    public const int CARD_NAME_PARTS = 3; // e.g., "3_of_spades"
    public const int CARD_RAISE_OFFSET = 15;
    public const int MAX_EXCHANGE_CARDS = 4;
    public const int STARTING_CHIPS = 100;
    public const int ANTE_AMOUNT = 5;
    public const int BET_OPTION_1 = 5;
    public const int BET_OPTION_2 = 10;
    public const int BET_OPTION_3 = 25;
}
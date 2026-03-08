using System.Collections.Generic;

public enum GamePhase
{
    None,
    Ante,
    DealCards,
    Exchange,
    Betting,
    Showdown,
    GameOver
}

public class GameState
{
    public int currentPlayerIndex;
    public List<Player> playerList = new();
    public GameMode gameMode; // 0: SingleMode, 1: MultiMode
    public GamePhase currentPhase;
    public int pot;
    public int currentBet;
    public int anteAmount;
    public int lastRaisePlayerIndex;
    public List<Card> deck;
    public Player humanPlayer;
    public Player cpuPlayer;

    public GameState(GameMode gameMode)
    {
        currentPlayerIndex = 0;
        playerList = new List<Player>();
        this.gameMode = gameMode;
        currentPhase = GamePhase.Ante;
        pot = 0;
        currentBet = 0;
        anteAmount = 5;
        lastRaisePlayerIndex = -1;
        deck = new List<Card>();
    }

    public void AddPlayer(Player player)
    {
        player.playerIndex = playerList.Count;
        playerList.Add(player);
        if (!player.isCPU)
        {
            humanPlayer = player;
        }
        else if (cpuPlayer == null)
        {
            cpuPlayer = player;
        }
    }

    public Player GetCurrentPlayer()
    {
        if (currentPlayerIndex >= 0 && currentPlayerIndex < playerList.Count)
        {
            return playerList[currentPlayerIndex];
        }
        return null;
    }

    public Player GetNextActivePlayer()
    {
        int startIndex = currentPlayerIndex;
        int attempts = 0;
        
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % playerList.Count;
            Player player = playerList[currentPlayerIndex];
            
            if (!player.hasFolded && player.tokenAmount > 0)
            {
                return player;
            }
            
            attempts++;
        } while (currentPlayerIndex != startIndex && attempts < playerList.Count);
        
        return null; // All players folded or out of chips
    }

    public List<Player> GetActivePlayers()
    {
        List<Player> active = new List<Player>();
        foreach (Player player in playerList)
        {
            if (!player.hasFolded && player.tokenAmount > 0)
            {
                active.Add(player);
            }
        }
        return active;
    }

    public void ResetBettingRound()
    {
        currentBet = 0;
        lastRaisePlayerIndex = -1;
        foreach (Player player in playerList)
        {
            player.currentBet = 0;
            player.hasActedThisRound = false;
        }
    }
}

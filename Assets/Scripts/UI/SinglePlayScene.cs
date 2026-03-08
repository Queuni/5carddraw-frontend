using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SinglePlayScene : MonoBehaviour
{
    [Header("Player Setup")]
    public Transform humanHandCenter;
    public Transform cpuHandCenter;
    public CardAnimator cardAnimator;
    public TableAnimator tableAnimator;

    public Player humanPlayer;
    public Player cpuPlayer;


    [Header("UI Buttons - Exchange Phase")]
    public Button exchangeButton;
    public Button skipExchangeButton;

    [Header("UI Buttons - Betting Phase")]
    public Button bet5Button;
    public Button bet10Button;
    public Button bet25Button;
    public Button callButton;
    public Button raiseButton;
    public Button foldButton;
    public Button checkButton;

    [Header("UI Display")]
    public TextMeshProUGUI potText;
    [Tooltip("Optional. Bet info also shown via AlertBar.ShowMessage when it changes.")]
    public TextMeshProUGUI betInfoText;
    public GamePhaseBar gamePhaseBar;
    public RoundFinished roundFinishedDialog;

    private GameState gameState;
    private string _lastBetInfoMessage;
    private GameFlowManager gameFlowManager;
    private Player lastRoundWinner;
    private int lastRoundPot;
    private bool pendingFoldConfirm;

    private void Awake()
    {
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
            // Setup button handlers
            if (roundFinishedDialog.continueButton != null)
            {
                roundFinishedDialog.continueButton.onClick.AddListener(OnContinueClicked);
            }
            if (roundFinishedDialog.quitButton != null)
            {
                roundFinishedDialog.quitButton.onClick.AddListener(OnQuitClicked);
            }
        }
    }

    void Start()
    {
        GameSettings.Load();
        gameState = new GameState(GameMode.SingleMode);

        // Initialize players
        if (humanPlayer == null)
        {
            Debug.LogError("SinglePlayScene: humanPlayer is not assigned in Inspector!");
            return;
        }
        
        if (cpuPlayer == null)
        {
            Debug.LogError("SinglePlayScene: cpuPlayer is not assigned in Inspector!");
            return;
        }
        
        humanPlayer.SetPlayerName("Player");
        humanPlayer.handCenter = humanHandCenter;
        humanPlayer.SetTokenAmount(Constants.STARTING_CHIPS);
        ApplyHumanProfileIfLoggedIn();

        cpuPlayer.SetPlayerName("CPU Player");
        cpuPlayer.handCenter = cpuHandCenter;
        cpuPlayer.isCPU = true;
        cpuPlayer.SetTokenAmount(Constants.STARTING_CHIPS);
        cpuPlayer.SetAvatar(ResourceManager.cpuAvatarSprite);

        gameState.AddPlayer(cpuPlayer);
        gameState.AddPlayer(humanPlayer);

        // Initialize GameFlowManager
        gameFlowManager = gameObject.AddComponent<GameFlowManager>();
        gameFlowManager.Initialize(gameState, tableAnimator);
        gameFlowManager.OnPotUpdated += UpdatePotDisplay;
        gameFlowManager.OnGameOver += OnGameOver;
        gameFlowManager.OnRoundComplete += OnRoundComplete;
        gameFlowManager.OnCPUAction += OnCPUAction;
        gameFlowManager.OnDealCardsRequested += OnDealCardsRequested;
        gameFlowManager.OnPhaseChanged += OnPhaseChanged;

        // Setup UI buttons
        SetupUIButtons();

        // Start game flow (ante is collected first, then cards are dealt)
        InitializeGame();
    }

    private void ApplyHumanProfileIfLoggedIn()
    {
        if (humanPlayer == null)
        {
            return;
        }

        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated() || AuthManager.Instance.IsGuestMode())
        {
            return;
        }

        UserSession session = AuthManager.Instance.GetCurrentSession();
        if (session == null)
        {
            return;
        }
        Utils.ApplySessionToPlayer(humanPlayer, session);
    }

    private void InitializeGame()
    {
        // Activate Ante phase in status bar at the beginning of game
        if (gamePhaseBar != null)
        {
            gamePhaseBar.SetPhase(GamePhase.Ante);
        }
        
        // Start game flow - ante phase happens first
        gameFlowManager.StartGame();

        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }
    }
    
    private void OnDealCardsRequested()
    {
        // Called when GameFlowManager requests cards to be dealt
        StartCoroutine(DealCardsCoroutine());
    }
    
    private IEnumerator DealCardsCoroutine()
    {
        // Deal cards after ante is collected (as per requirements)
        DealManager.Deal(gameState);
        yield return StartCoroutine(cardAnimator.DealAnimator(gameState));
        
        // Update UI
        UpdatePotDisplay(gameState.pot);
        
        // Signal that cards are dealt, continue to Exchange phase
        gameState.currentPhase = GamePhase.Exchange;
    }

    private void SetupUIButtons()
    {
        // Exchange buttons
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }
        if (skipExchangeButton != null)
        {
            skipExchangeButton.onClick.AddListener(OnSkipExchangeClicked);
        }

        // Betting buttons
        if (bet5Button != null)
        {
            bet5Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_1));
        }
        if (bet10Button != null)
        {
            bet10Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_2));
        }
        if (bet25Button != null)
        {
            bet25Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_3));
        }
        if (callButton != null)
        {
            callButton.onClick.AddListener(OnCallClicked);
        }
        if (raiseButton != null)
        {
            raiseButton.onClick.AddListener(OnRaiseClicked);
        }
        if (foldButton != null)
        {
            foldButton.onClick.AddListener(OnFoldClicked);
        }
        if (checkButton != null)
        {
            checkButton.onClick.AddListener(OnCheckClicked);
        }

        // Initially disable all buttons
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (gameState == null || humanPlayer == null) return;
        
        bool antePhase = gameState.currentPhase == GamePhase.Ante;
        bool exchangePhase = gameState.currentPhase == GamePhase.Exchange;
        bool bettingPhase = gameState.currentPhase == GamePhase.Betting;
        bool showdownPhase = gameState.currentPhase == GamePhase.Showdown;

        // Exchange phase
        if (exchangeButton != null)
        {
            exchangeButton.gameObject.SetActive(exchangePhase && !humanPlayer.hasExchanged);
        }
        if (skipExchangeButton != null)
        {
            skipExchangeButton.gameObject.SetActive(exchangePhase && !humanPlayer.hasExchanged);
        }

        // Betting phase
        int callAmount = gameState.currentBet - humanPlayer.currentBet;
        bool canBet = bettingPhase && !humanPlayer.hasActedThisRound && !humanPlayer.hasFolded && humanPlayer.tokenAmount > 0;
        bool isInitialBet = gameState.currentBet == 0 || gameState.currentBet == humanPlayer.currentBet;
        bool isResponseBet = gameState.currentBet > 0 && gameState.currentBet > humanPlayer.currentBet;
        bool canCheck = bettingPhase && !humanPlayer.hasActedThisRound && !humanPlayer.hasFolded && callAmount == 0;

        if (bettingPhase)
        {
            string toCallStr = callAmount <= 0 ? "Check" : $"To call: {callAmount}";
            string betInfoMessage = $"Current bet: {gameState.currentBet} | {toCallStr}";
            if (humanPlayer.tokenAmount <= 0 && !humanPlayer.hasFolded)
            {
                betInfoMessage += " | You're out of chips – waiting for others.";
            }
            if (betInfoText != null)
            {
                betInfoText.text = betInfoMessage;
                betInfoText.gameObject.SetActive(true);
            }
            if (AlertBar.Instance != null && betInfoMessage != _lastBetInfoMessage)
            {
                _lastBetInfoMessage = betInfoMessage;
                AlertBar.Instance.ShowMessage(betInfoMessage);
            }
        }
        else
        {
            _lastBetInfoMessage = null;
            if (betInfoText != null)
            {
                betInfoText.gameObject.SetActive(false);
            }
        }

        // Initial bet buttons (5, 10, 25) - show when human bets first
        if (bet5Button != null)
        {
            bool showBet = canBet && isInitialBet && humanPlayer.tokenAmount >= Constants.BET_OPTION_1;
            bet5Button.gameObject.SetActive(showBet);
            bet5Button.interactable = showBet;
        }
        if (bet10Button != null)
        {
            bool showBet = canBet && isInitialBet && humanPlayer.tokenAmount >= Constants.BET_OPTION_2;
            bet10Button.gameObject.SetActive(showBet);
            bet10Button.interactable = showBet;
        }
        if (bet25Button != null)
        {
            bool showBet = canBet && isInitialBet && humanPlayer.tokenAmount >= Constants.BET_OPTION_3;
            bet25Button.gameObject.SetActive(showBet);
            bet25Button.interactable = showBet;
        }
        
        // Call/Raise/Fold buttons - show when human responds to CPU's bet/raise
        if (callButton != null)
        {
            bool canCall = canBet && isResponseBet && callAmount > 0 && callAmount <= humanPlayer.tokenAmount;
            callButton.gameObject.SetActive(canCall);
            callButton.interactable = canCall;
        }
        if (checkButton != null)
        {
            checkButton.gameObject.SetActive(canCheck);
            checkButton.interactable = canCheck;
        }
        if (raiseButton != null)
        {
            int raiseAmount = gameState.currentBet * 2 - humanPlayer.currentBet;
            bool canRaise = canBet && isResponseBet && raiseAmount > 0 && raiseAmount <= humanPlayer.tokenAmount;
            raiseButton.gameObject.SetActive(canRaise);
            raiseButton.interactable = canRaise;
        }
        if (foldButton != null)
        {
            // Fold button shows in both initial bet and response scenarios
            foldButton.gameObject.SetActive(canBet);
            foldButton.interactable = canBet;
        }
    }

    public void OnExchangeClicked()
    {
        if (gameState.currentPhase == GamePhase.Exchange && !humanPlayer.hasExchanged)
        {
            if (humanPlayer.selectedCards.Count > Constants.MAX_EXCHANGE_CARDS)
            {
                return;
            }

            if (humanPlayer.selectedCards.Count > 0)
            {
                // Create a copy of selected cards list BEFORE exchange
                // This is critical because ExchangeCards removes them from the hand
                List<Card> cardsToExchange = new List<Card>(humanPlayer.selectedCards);
                
                // Clear selection before exchange (so cards aren't in selectedCards during animation)
                humanPlayer.selectedCards.Clear();
                
                // Animate the exchange FIRST (before cards are removed from hand)
                // The animation will handle removing cards and adding new ones
                StartCoroutine(PerformExchangeWithAnimation(humanPlayer, cardsToExchange, humanHandCenter));
                
                humanPlayer.hasExchanged = true;
                UpdateButtonStates();
            }
            else
            {
                // No cards selected, show warning message
                AlertBar.Instance.ShowMessage("No cards selected to exchange");
            }
        }
    }
    
    private IEnumerator PerformExchangeWithAnimation(Player player, List<Card> cardsToExchange, Transform handCenter)
    {
        // Store card positions BEFORE exchange (cards are still in hand at this point)
        // This allows the animation to know where cards were positioned
        Dictionary<Card, Vector3> cardPositionsBeforeExchange = new Dictionary<Card, Vector3>();
        foreach (Card card in player.playerHand)
        {
            if (card != null && card.cardObject != null)
            {
                cardPositionsBeforeExchange[card] = card.cardObject.transform.position;
            }
        }
        
        // Exchange cards (removes from hand and adds new ones, then sorts)
        DealManager.ExchangeCards(player, cardsToExchange, gameState);
        
        // Now animate the exchange with the stored positions
        yield return StartCoroutine(cardAnimator.ExchangeCardsAnimation(player, cardsToExchange, handCenter, cardPositionsBeforeExchange));

        if (GameSettings.CardAutoSort)
        {
            Rules.SortCards(player.playerHand);
            if (cardAnimator != null)
            {
                yield return StartCoroutine(cardAnimator.SpreadCards(gameState));
            }
        }
    }

    public void OnSkipExchangeClicked()
    {
        if (gameState.currentPhase == GamePhase.Exchange && !humanPlayer.hasExchanged)
        {
            humanPlayer.hasExchanged = true;
            UpdateButtonStates();
        }
    }

    public void OnBetClicked(int amount)
    {
        pendingFoldConfirm = false;
        if (gameState.currentPhase == GamePhase.Betting && !humanPlayer.hasActedThisRound)
        {
            if (humanPlayer.CanBet(amount))
            {
                humanPlayer.Bet(amount);
                humanPlayer.currentBet = amount;
                humanPlayer.hasActedThisRound = true;
                
                if (amount > gameState.currentBet)
                {
                    gameState.currentBet = amount;
                    gameState.lastRaisePlayerIndex = humanPlayer.playerIndex;
                }
                
                // Pot will be updated by GameFlowManager.WaitForHumanBet() to avoid double-counting
                UpdateButtonStates();
                
                // Show message
                humanPlayer.showMessage($"Bet {amount}");
            }
        }
    }

    public void OnCallClicked()
    {
        pendingFoldConfirm = false;
        if (gameState.currentPhase == GamePhase.Betting && !humanPlayer.hasActedThisRound)
        {
            int callAmount = gameState.currentBet - humanPlayer.currentBet;
            if (humanPlayer.CanBet(callAmount))
            {
                humanPlayer.Bet(callAmount);
                humanPlayer.currentBet = gameState.currentBet;
                humanPlayer.hasActedThisRound = true;
                
                // Pot will be updated by GameFlowManager.WaitForHumanBet() to avoid double-counting
                UpdateButtonStates();
                
                // Show message
                if (callAmount > 0)
                {
                    humanPlayer.showMessage($"Call {callAmount}");
                }
                else
                {
                    humanPlayer.showMessage("Check");
                }
            }
        }
    }

    public void OnRaiseClicked()
    {
        pendingFoldConfirm = false;
        if (gameState.currentPhase == GamePhase.Betting && !humanPlayer.hasActedThisRound)
        {
            int raiseAmount = gameState.currentBet * 2 - humanPlayer.currentBet;
            if (humanPlayer.CanBet(raiseAmount))
            {
                humanPlayer.Bet(raiseAmount);
                humanPlayer.currentBet = gameState.currentBet * 2;
                humanPlayer.hasActedThisRound = true;
                
                gameState.currentBet = humanPlayer.currentBet;
                gameState.lastRaisePlayerIndex = humanPlayer.playerIndex;
                
                // Pot will be updated by GameFlowManager.WaitForHumanBet() to avoid double-counting
                UpdateButtonStates();
                
                // Show message
                humanPlayer.showMessage($"Raise to {humanPlayer.currentBet}");
            }
        }
    }

    public void OnFoldClicked()
    {
        if (gameState.currentPhase == GamePhase.Betting && !humanPlayer.hasActedThisRound)
        {
            if (GameSettings.ConfirmBeforeFold && !pendingFoldConfirm)
            {
                pendingFoldConfirm = true;
                if (AlertBar.Instance != null)
                {
                    AlertBar.Instance.ShowMessage("Tap Fold again to confirm");
                }
                return;
            }

            pendingFoldConfirm = false;
            humanPlayer.hasFolded = true;
            humanPlayer.hasActedThisRound = true;
            UpdateButtonStates();
            if (cardAnimator != null)
            {
                cardAnimator.HidePlayerHand(humanPlayer);
            }
            humanPlayer.showMessage("Fold");
        }
    }

    public void OnCheckClicked()
    {
        if (gameState.currentPhase != GamePhase.Betting || humanPlayer.hasActedThisRound || humanPlayer.hasFolded)
        {
            return;
        }
        int callAmount = gameState.currentBet - humanPlayer.currentBet;
        if (callAmount != 0)
        {
            return;
        }
        humanPlayer.hasActedThisRound = true;
        humanPlayer.showMessage("Check");
        UpdateButtonStates();
    }

    private void UpdatePotDisplay(int potAmount)
    {
        if (potText != null)
        {
            potText.text = potAmount.ToString();
        }
    }


    private void OnGameOver(Player winner, string reason)
    {
        // Store winner info for RoundFinished dialog
        lastRoundWinner = winner;
        // Store pot amount that was won (before it gets reset)
        lastRoundPot = gameState.pot;

        SaveSingleModeRecord(winner, lastRoundPot);
    }

    private void OnCPUAction(string actionMessage)
    {
        // CPU action taken - message is displayed on CPU player prefab
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        pendingFoldConfirm = false;
        // Update phase bar display
        if (gamePhaseBar != null)
        {
            gamePhaseBar.SetPhase(phase);
        }
    }

    private void OnRoundComplete()
    {
        // Check if game is over (one player out of chips)
        if (humanPlayer == null || cpuPlayer == null)
        {
            Debug.LogError("SinglePlayScene: Players are null in OnRoundComplete!");
            return;
        }
        
        if (humanPlayer.tokenAmount <= 0 || cpuPlayer.tokenAmount <= 0)
        {
            // Game over - one player is out of chips
            string winner = humanPlayer.tokenAmount > 0 ? (humanPlayer.playerName ?? "Human Player") : (cpuPlayer.playerName ?? "CPU Player");
            
            // Show RoundFinished dialog with game over message
            if (roundFinishedDialog != null)
            {
                string title = "Game Over";
                string content = $"{winner} wins!";
                roundFinishedDialog.showDialog(title, content);
                // Hide continue button for game over
                if (roundFinishedDialog.continueButton != null)
                {
                    roundFinishedDialog.continueButton.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Round finished - show dialog to continue or quit
            if (roundFinishedDialog != null && lastRoundWinner != null)
            {
                string title = "Round Ended";
                string winnerName = lastRoundWinner.playerName ?? "Unknown Player";
                string content = $"{winnerName} wins!\n+{lastRoundPot} chips";
                roundFinishedDialog.showDialog(title, content);
                // Show continue button for next round
                if (roundFinishedDialog.continueButton != null)
                {
                    roundFinishedDialog.continueButton.gameObject.SetActive(true);
                }
            }
            else
            {
                // Fallback: if dialog not available, auto-continue
                StartNewRound();
            }
        }
    }
    
    private void OnContinueClicked()
    {
        // Hide dialog
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }
        
        // Start new round
        StartNewRound();
    }
    
    private void OnQuitClicked()
    {
        // Hide dialog
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }
        
        // Clean up before loading new scene to prevent errors
        CleanupBeforeSceneChange();
        
        // Return to main menu
        Utils.LoadScene("MainMenuScene");
    }

    public void OnFinishClicked()
    {
        // Clean up before loading new scene to prevent errors
        CleanupBeforeSceneChange();
        Utils.LoadScene("MainMenuScene");
    }
    
    private void CleanupBeforeSceneChange()
    {
        // Kill all DOTween animations to prevent errors when GameObjects are destroyed
        // This is safe because we're loading a new scene, so all current scene animations should stop
        DOTween.KillAll();
        
        // Stop all coroutines on this GameObject
        StopAllCoroutines();
        
        // Stop coroutines on GameFlowManager if it exists
        if (gameFlowManager != null)
        {
            gameFlowManager.StopAllCoroutines();
        }
        
        // Stop coroutines on CardAnimator if it exists
        if (cardAnimator != null)
        {
            cardAnimator.StopAllCoroutines();
        }
        
        // Stop coroutines on TableAnimator if it exists
        if (tableAnimator != null)
        {
            tableAnimator.StopAllCoroutines();
        }
        
        // Unsubscribe from events to prevent callbacks on destroyed objects
        // (Optional but good practice - objects will be destroyed anyway, but this prevents warnings)
        if (gameFlowManager != null)
        {
            gameFlowManager.OnPotUpdated -= UpdatePotDisplay;
            gameFlowManager.OnGameOver -= OnGameOver;
            gameFlowManager.OnRoundComplete -= OnRoundComplete;
            gameFlowManager.OnCPUAction -= OnCPUAction;
            gameFlowManager.OnDealCardsRequested -= OnDealCardsRequested;
            gameFlowManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void StartNewRound()
    {
        // Reset players
        humanPlayer.ResetForNewRound();
        cpuPlayer.ResetForNewRound();
        
        // Reset game state
        gameState.pot = 0;
        gameState.currentBet = 0;
        gameState.currentPhase = GamePhase.Ante;
        
        // Reset betting state
        gameState.ResetBettingRound();
        
        // Hide pot for new round (will be shown again in ante phase)
        if (tableAnimator != null)
        {
            tableAnimator.HidePot();
        }
        
        // Activate Ante phase in status bar at the beginning of round
        if (gamePhaseBar != null)
        {
            gamePhaseBar.SetPhase(GamePhase.Ante);
        }
        
        // Start new game flow (will handle ante -> deal cards -> exchange -> betting -> showdown)
        // Cards will be dealt in the DealCards phase, not here
        gameFlowManager.StartGame();
    }

    private void SaveSingleModeRecord(Player winner, int winChips)
    {
        if (humanPlayer == null)
        {
            return;
        }

        bool isWin = winner == humanPlayer;

        string date = System.DateTime.Now.ToString("yyyy-MM-dd");
        int recordedChips = isWin ? winChips : 0;
        string winnerLabel = isWin ? "You" : "CPU";

        SingleModeRecord record = new SingleModeRecord
        {
            date = date,
            winChips = recordedChips.ToString(),
            winner = winnerLabel
        };

        LeaderboardStorage.AddSingleModeRecord(record, 200);
    }

    void Update()
    {
        // Update button states based on current phase
        if (gameState != null)
        {
            UpdateButtonStates();
        }
    }
}

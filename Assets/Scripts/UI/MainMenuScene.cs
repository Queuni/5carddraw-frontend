using UnityEngine;
using UnityEngine.UI;

// Main menu screen - shows different buttons based on guest or logged in user
public class MainMenuScene : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button logoutButton;
    public Button loginButton;
    public Player currentPlayer;

    private void Start()
    {
        Spinner.Instance.Hide();
        UpdateButtonStates();
        UpdatePlayerInfo();
    }

    // Show or hide buttons based on guest mode
    private void UpdateButtonStates()
    {
        bool isGuestMode = AuthManager.Instance != null && AuthManager.Instance.IsGuestMode();

        // Guest users see login button, logged in users see logout button
        Utils.SetButtonVisible(logoutButton, !isGuestMode);
        Utils.SetButtonVisible(loginButton, isGuestMode);
    }

    // Set player name and avatar in main menu
    private void UpdatePlayerInfo()
    {
        if (currentPlayer == null)
        {
            return;
        }

        // Default guest values
        currentPlayer.SetPlayerName("Player");
        if (ResourceManager.avatarSpriteList != null && ResourceManager.avatarSpriteList.Count > 0)
        {
            currentPlayer.SetAvatar(ResourceManager.avatarSpriteList[0]);
        }

        // If user is logged in and not guest, use session values
        if (AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated() && !AuthManager.Instance.IsGuestMode())
        {
            var session = AuthManager.Instance.GetCurrentSession();
            Utils.ApplySessionToPlayer(currentPlayer, session);
        }
    }

    public void OnSingleModeClicked()
    {
        Utils.LoadScene("SinglePlayScene");
    }

    public void OnOnlineModeClicked()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsGuestMode())
        {
            Utils.LoadScene("LoginScene");
        }
        else
        {
            Utils.LoadScene("RoomScene");
        }
    }

    public void OnRulesClicked()
    {
        Utils.LoadScene("RulesScene");
    }

    public void OnProfileClicked()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsGuestMode())
        {
            Utils.LoadScene("LoginScene");
        }
        else
        {
            Utils.LoadScene("ProfileScene");
        }
    }

    public void OnLeaderboardClicked()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsGuestMode())
        {
            Utils.LoadScene("LoginScene");
        }
        else
        {
            Utils.LoadScene("LeaderboardScene");
        }
    }

    public void OnSettingsClicked()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsGuestMode())
        {
            Utils.LoadScene("LoginScene");
        }
        else
        {
            Utils.LoadScene("SettingScene");
        }
    }

    public void OnLogoutClicked()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated())
        {
            // Sign out and clear session
            AuthManager.Instance.SignOut();
        }
        Utils.LoadScene("LoginScene");
    }

    public void OnLoginClicked()
    {
        Utils.LoadScene("LoginScene");
    }

    
}

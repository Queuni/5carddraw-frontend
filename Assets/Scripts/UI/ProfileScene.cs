using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles profile screen - username and avatar selection
public class ProfileScene : MonoBehaviour
{
    public Button currentAvatarButton;
    public List<Button> avatarButtonList;
    public TMP_InputField userNameInput;
    public Button updateButton;

    [Header("Delete Account")]
    public Button deleteAccountButton;
    public GameObject askSurePanel;
    public Button askSureContinueButton;
    public Button askSureCancelButton;

    private int selectedAvatarIndex = 0;
    private bool isDeleteRequestInProgress;

    [System.Serializable]
    private class UpdateProfileRequestData
    {
        public string userName;
        public int avatarIndex;
    }

    // Start is called before the first frame update
    void Start()
    {
        Spinner.Instance.Hide();

        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated() || AuthManager.Instance.IsGuestMode())
        {
            AlertBar.Instance.ShowMessage("Login required");
            Utils.LoadScene("LoginScene");
            return;
        }

        var session = AuthManager.Instance.GetCurrentSession();
        if (session == null)
        {
            AlertBar.Instance.ShowMessage("Session not found");
            Utils.LoadScene("LoginScene");
            return;
        }

        if (ResourceManager.avatarSpriteList == null || ResourceManager.avatarSpriteList.Count == 0)
        {
            DebugConfig.LogError("Avatars do not exist");
            return;
        }

        // Clamp avatar index to available range
        selectedAvatarIndex = Mathf.Clamp(session.avatarIndex, 0, ResourceManager.avatarSpriteList.Count - 1);

        // Set current avatar image
        if (currentAvatarButton != null &&
            currentAvatarButton.TryGetComponent<Image>(out var currentImage))
        {
            currentImage.sprite = ResourceManager.avatarSpriteList[selectedAvatarIndex];
        }

        // Assign avatar buttons safely
        int count = Mathf.Min(
            avatarButtonList.Count,
            ResourceManager.avatarSpriteList.Count
        );

        for (int i = 0; i < count; i++)
        {
            Button avatarButton = avatarButtonList[i];

            if (avatarButton != null &&
                avatarButton.TryGetComponent<Image>(out var image))
            {
                image.sprite = ResourceManager.avatarSpriteList[i];

                int index = i; // Capture for closure
                avatarButton.onClick.RemoveAllListeners();
                avatarButton.onClick.AddListener(() => OnAvatarSelected(index));
            }
        }

        // Set current username
        if (userNameInput != null)
        {
            userNameInput.text = session.userName;
        }

        if (updateButton != null)
        {
            updateButton.onClick.RemoveAllListeners();
            updateButton.onClick.AddListener(OnUpdateClicked);
        }

        if (askSurePanel != null)
        {
            askSurePanel.SetActive(false);
        }

        if (deleteAccountButton != null)
        {
            deleteAccountButton.onClick.RemoveAllListeners();
            deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
        }

        if (askSureContinueButton != null)
        {
            askSureContinueButton.onClick.RemoveAllListeners();
            askSureContinueButton.onClick.AddListener(OnAskSureContinueClicked);
        }

        if (askSureCancelButton != null)
        {
            askSureCancelButton.onClick.RemoveAllListeners();
            askSureCancelButton.onClick.AddListener(OnAskSureCancelClicked);
        }
    }

    private void OnAvatarSelected(int index)
    {
        selectedAvatarIndex = Mathf.Clamp(index, 0, ResourceManager.avatarSpriteList.Count - 1);

        if (currentAvatarButton != null &&
            currentAvatarButton.TryGetComponent<Image>(out var currentImage))
        {
            currentImage.sprite = ResourceManager.avatarSpriteList[selectedAvatarIndex];
        }
    }

    public void OnUpdateClicked()
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsAuthenticated() || AuthManager.Instance.IsGuestMode())
        {
            AlertBar.Instance.ShowMessage("Login required");
            return;
        }

        var session = AuthManager.Instance.GetCurrentSession();
        if (session == null)
        {
            AlertBar.Instance.ShowMessage("Session not found");
            return;
        }

        string newUserName = userNameInput != null ? userNameInput.text.Trim() : "";

        // Validate username
        if (string.IsNullOrEmpty(newUserName))
        {
            AlertBar.Instance.ShowMessage("Enter username");
            return;
        }
        if (!Utils.IsValidUserName(newUserName))
        {
            AlertBar.Instance.ShowMessage("Invalid username");
            return;
        }

        // Check if anything changed
        if (newUserName == session.userName && selectedAvatarIndex == session.avatarIndex)
        {
            AlertBar.Instance.ShowMessage("No changes");
            return;
        }

        if (APIService.Instance == null)
        {
            AlertBar.Instance.ShowMessage("API not ready");
            return;
        }

        Spinner.Instance.Show();

        var request = new UpdateProfileRequestData
        {
            userName = newUserName,
            avatarIndex = selectedAvatarIndex
        };

        APIService.Instance.Put<UserSession>(
            "/profile",
            request,
            (updatedProfile) =>
            {
                Spinner.Instance.Hide();

                // Update local session from server response if available
                var current = AuthManager.Instance.GetCurrentSession();
                if (current != null && updatedProfile != null)
                {
                    current.userName = updatedProfile.userName;
                    current.avatarIndex = updatedProfile.avatarIndex;
                    current.SaveToPlayerPrefs();
                }

                AlertBar.Instance.ShowMessage("Profile updated");
            },
            (error) =>
            {
                Spinner.Instance.Hide();
                AlertBar.Instance.ShowMessage(error);
            },
            requiresAuth: true
        );
    }

    public void OnBackClicked()
    {
        Utils.LoadScene("MainMenuScene");
    }

    public void OnDeleteAccountClicked()
    {
        if (askSurePanel != null)
        {
            askSurePanel.SetActive(true);
        }
        else
        {
            RequestDeleteAccount();
        }
    }

    private void OnAskSureContinueClicked()
    {
        RequestDeleteAccount();
    }

    private void OnAskSureCancelClicked()
    {
        if (askSurePanel != null)
        {
            askSurePanel.SetActive(false);
        }
    }

    private void RequestDeleteAccount()
    {
        if (APIService.Instance == null)
        {
            AlertBar.Instance.ShowMessage("Network not ready.");
            return;
        }

        if (isDeleteRequestInProgress)
        {
            return;
        }

        isDeleteRequestInProgress = true;
        if (askSureContinueButton != null)
        {
            askSureContinueButton.interactable = false;
        }
        Spinner.Instance.Show();

        APIService.Instance.Delete<DeleteAccountResponse>(
            "/profile",
            (data) =>
            {
                isDeleteRequestInProgress = false;
                Spinner.Instance.Hide();
                if (askSurePanel != null)
                {
                    askSurePanel.SetActive(false);
                }
                AuthManager.Instance?.SignOut();
                AlertBar.Instance.ShowMessage("Account deleted.");
                Utils.LoadScene("LoginScene");
            },
            (error) =>
            {
                isDeleteRequestInProgress = false;
                if (askSureContinueButton != null)
                {
                    askSureContinueButton.interactable = true;
                }
                Spinner.Instance.Hide();
                AlertBar.Instance.ShowMessage(error ?? "Failed to delete account.");
            },
            true
        );
    }

    [System.Serializable]
    private class DeleteAccountResponse
    {
        public string message;
    }
}

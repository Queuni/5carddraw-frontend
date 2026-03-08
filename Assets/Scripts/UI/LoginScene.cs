using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles the login screen
public class LoginScene : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Forgot Password - Ask Sure Panel")]
    [Tooltip("Confirmation panel with forgot password content. Assign Yes/No buttons.")]
    public GameObject askSurePanel;
    public Button askSureYesButton;
    public Button askSureNoButton;

    private void Start()
    {
        AppBootstrap.EnsureManagers();
        Spinner.Instance.Hide();

        if (askSurePanel != null)
        {
            askSurePanel.SetActive(false);
        }

        if (askSureYesButton != null)
        {
            askSureYesButton.onClick.RemoveAllListeners();
            askSureYesButton.onClick.AddListener(OnAskSureYesClicked);
        }

        if (askSureNoButton != null)
        {
            askSureNoButton.onClick.RemoveAllListeners();
            askSureNoButton.onClick.AddListener(OnAskSureNoClicked);
        }
    }

    public void OnToRegisterClicked()
    {
        Utils.LoadScene("RegisterScene");
    }

    public void OnLoginClicked()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[LoginScene] AuthManager instance not found!");
            AlertBar.Instance.ShowMessage("Auth not ready");
            return;
        }

        string identifier = emailInput != null ? emailInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(identifier))
        {
            AlertBar.Instance.ShowMessage("Enter email or username");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            AlertBar.Instance.ShowMessage("Enter password");
            return;
        }

        Spinner.Instance.Show();

        AuthManager.Instance.SignInWithEmail(
            identifier: identifier,
            password: password,
            onSuccess: (session) =>
            {
                Spinner.Instance.Hide();
                Debug.Log($"[LoginScene] Sign in successful: {session.userName}");
                AppBootstrap.ConnectSocket(session);
                Utils.LoadScene("MainMenuScene");
            },
            onError: (error) =>
            {
                Spinner.Instance.Hide();
                Debug.LogError($"[LoginScene] Sign in failed: {error}");
                AlertBar.Instance.ShowMessage(error);
            }
        );
    }

    public void OnForgotClicked()
    {
        string email = emailInput != null ? emailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email))
        {
            AlertBar.Instance.ShowMessage("Enter your email address first.");
            return;
        }
        if (!Utils.IsValidEmail(email))
        {
            AlertBar.Instance.ShowMessage("Invalid email.");
            return;
        }
        if (askSurePanel != null)
        {
            askSurePanel.SetActive(true);
        }
        else
        {
            RequestForgotPassword(email);
        }
    }

    private void OnAskSureYesClicked()
    {
        string email = emailInput != null ? emailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email) || !Utils.IsValidEmail(email))
        {
            AlertBar.Instance.ShowMessage("Enter a valid email above.");
            return;
        }
        RequestForgotPassword(email);
    }

    private void OnAskSureNoClicked()
    {
        if (askSurePanel != null)
        {
            askSurePanel.SetActive(false);
        }
    }

    private void RequestForgotPassword(string email)
    {
        if (APIService.Instance == null)
        {
            AlertBar.Instance.ShowMessage("Network not ready.");
            return;
        }

        Spinner.Instance.Show();

        APIService.Instance.Post<ForgotPasswordResponse>(
            "/auth/forgot-password",
            new ForgotPasswordRequest { email = email },
            (data) =>
            {
                Spinner.Instance.Hide();
                if (askSurePanel != null)
                {
                    askSurePanel.SetActive(false);
                }
                AlertBar.Instance.ShowMessage("Check your mailbox or spam folder.");
            },
            (error) =>
            {
                Spinner.Instance.Hide();
                AlertBar.Instance.ShowMessage(error ?? "Request failed.");
            },
            false
        );
    }

    [Serializable]
    private class ForgotPasswordRequest
    {
        public string email;
    }

    [Serializable]
    private class ForgotPasswordResponse
    {
        public string message;
    }

    // When guest button is clicked, sign in as anonymous user
    public void OnGuestModeClicked()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[LoginScene] AuthManager instance not found!");
            AlertBar.Instance.ShowMessage("Auth not ready");
            return;
        }

        Spinner.Instance.Show();
        
        AuthManager.Instance.SignInAnonymously(
            onSuccess: (session) =>
            {
                Spinner.Instance.Hide();
                Debug.Log($"[LoginScene] Guest mode sign-in successful: {session.userName}");
                AppBootstrap.ConnectSocket(session);
                Utils.LoadScene("MainMenuScene");
            },
            onError: (error) =>
            {
                Spinner.Instance.Hide();
                Debug.LogError($"[LoginScene] Guest mode sign-in failed: {error}");
                AlertBar.Instance.ShowMessage($"Guest sign in failed: {error}");
            }
        );
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles the registration screen
public class RegisterScene : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField userNameInput;

    private void Start()
    {
        Spinner.Instance.Hide();
    }

    public void OnCreateAccountClicked()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[RegisterScene] AuthManager instance not found!");
            AlertBar.Instance.ShowMessage("Auth not ready");
            return;
        }

        string email = emailInput != null ? emailInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";
        string userName = userNameInput != null ? userNameInput.text.Trim() : "";
        int avatarIndex = 0;

        // Validate input
        if (string.IsNullOrEmpty(email))
        {
            AlertBar.Instance.ShowMessage("Enter email");
            return;
        }

        if (!Utils.IsValidEmail(email))
        {
            AlertBar.Instance.ShowMessage("Invalid email");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            AlertBar.Instance.ShowMessage("Enter password");
            return;
        }

        if (!Utils.IsValidPassword(password))
        {
            AlertBar.Instance.ShowMessage("Password too weak");
            return;
        }

        if (string.IsNullOrEmpty(userName))
        {
            AlertBar.Instance.ShowMessage("Enter username");
            return;
        }

        if (userName.Length < 1 || userName.Length > 20)
        {
            AlertBar.Instance.ShowMessage("Invalid username");
            return;
        }

        Spinner.Instance.Show();

        AuthManager.Instance.SignUpWithEmail(
            email: email,
            password: password,
            userName: userName,
            avatarIndex: avatarIndex,
            onSuccess: (session) =>
            {
                Spinner.Instance.Hide();
                Debug.Log($"[RegisterScene] Account created successfully: {session.userName}");
                AlertBar.Instance.ShowMessage("Account created!");
                Utils.LoadScene("MainMenuScene");
            },
            onError: (error) =>
            {
                Spinner.Instance.Hide();
                Debug.LogError($"[RegisterScene] Account creation failed: {error}");
                AlertBar.Instance.ShowMessage(error);
            }
        );
    }

    public void OnToLoginClicked()
    {
        Utils.LoadScene("LoginScene");
    }
}

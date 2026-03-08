using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Utils
{
    public static string previousSceneName;

    public static void LoadScene(string sceneName, bool destroyAll = true)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            previousSceneName = sceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"Scene '{sceneName}' not found!");
        }
    }

    public static bool IsValidEmail(string email)
    {
        // Cross-platform email validation using regex instead of System.Net.Mail
        // System.Net.Mail may not be available on all platforms (especially WebGL)
        if (string.IsNullOrEmpty(email))
            return false;
            
        try
        {
            // Simple regex-based email validation that works on all platforms
            var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string password)
    {
        // At least 8 chars, must contain at least one letter and one number
        var pattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$";
        return Regex.IsMatch(password, pattern);
    }

    // Validate username (1-20 characters, alphanumeric and spaces)
    public static bool IsValidUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return false;

        var pattern = @"^[a-zA-Z0-9\s]{1,20}$";
        return Regex.IsMatch(userName, pattern);
    }

    public static void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public static void LoadSceneAsDialog(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public static void CloseDialogScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }

    public static string ObjectToJson<T>(T obj)
    {
        return JsonUtility.ToJson(obj, true);
    }

    public static T JsonToObject<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    public static void LogToFile(string message)
    {
        // WebGL doesn't support file writing, so we only log to console
        #if !UNITY_WEBGL || UNITY_EDITOR
        try
        {
            string logPath = Path.Combine(Application.persistentDataPath, "game_log.txt");
            string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch (System.Exception e)
        {
            // Fallback to console if file writing fails (e.g., WebGL, permissions)
            Debug.Log($"[LogToFile] {message} (File write failed: {e.Message})");
        }
        #else
        // WebGL: Just use console logging
        Debug.Log($"[LogToFile] {message}");
        #endif
    }

    public static Suit ParseSuit(string s)
    {
        switch (s.ToLower())
        {
            case "spades": return Suit.Spades;
            case "clubs": return Suit.Clubs;
            case "diamonds": return Suit.Diamonds;
            case "hearts": return Suit.Hearts;
            default: return Suit.Hearts;
        }
    }

    public static int ParseRank(string r)
    {
        switch (r.ToLower())
        {
            case "jack": return 11;
            case "queen": return 12;
            case "king": return 13;
            case "ace": return 14;
            case "2": return 15;
            default:
                int val;
                return int.TryParse(r, out val) ? val : 0;
        }
    }

    public static int RankToInt(Rank r) => (int)r;
    public static int SuitToInt(Suit s) => s switch { Suit.Spades => 0, Suit.Clubs => 1, Suit.Diamonds => 2, _ => 3 };

    // Enable or disable a button (check if button exists first)
    public static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
        else
        {
            Debug.LogWarning("[Utils] Button reference is null - cannot set interactable state");
        }
    }

    // Show or hide a button (check if button exists first)
    public static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
        else
        {
            Debug.LogWarning("[Utils] Button reference is null - cannot set visibility");
        }
    }

    public static void ApplySessionToPlayer(Player player, UserSession session)
    {
        if (player == null || session == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(session.userName))
        {
            player.SetPlayerName(session.userName);
        }

        if (ResourceManager.avatarSpriteList != null && ResourceManager.avatarSpriteList.Count > 0)
        {
            int index = Mathf.Clamp(session.avatarIndex, 0, ResourceManager.avatarSpriteList.Count - 1);
            player.SetAvatar(ResourceManager.avatarSpriteList[index]);
        }
    }
}

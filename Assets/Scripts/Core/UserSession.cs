using System;
using UnityEngine;

// Stores user login information and saves it to device
[Serializable]
public class UserSession
{
    public string uid;
    public string userName;
    public int avatarIndex;
    public bool isAnonymous;
    public bool isEmailVerified;
    public string email;
    public string authToken;
    public long lastLoginTimestamp;

    private const string PREFS_UID = "UserSession_UID";
    private const string PREFS_USER_NAME = "UserSession_UserName";
    private const string PREFS_AVATAR_INDEX = "UserSession_AvatarIndex";
    private const string PREFS_IS_ANONYMOUS = "UserSession_IsAnonymous";
    private const string PREFS_IS_EMAIL_VERIFIED = "UserSession_IsEmailVerified";
    private const string PREFS_EMAIL = "UserSession_Email";
    private const string PREFS_AUTH_TOKEN = "UserSession_AuthToken";
    private const string PREFS_LAST_LOGIN = "UserSession_LastLogin";

    public UserSession()
    {
        uid = string.Empty;
        userName = string.Empty;
        avatarIndex = 0;
        isAnonymous = true;
        isEmailVerified = false;
        email = string.Empty;
        authToken = string.Empty;
        lastLoginTimestamp = 0;
    }

    // Check if we have a saved session
    public static bool HasStoredSession()
    {
        return PlayerPrefs.HasKey(PREFS_UID) && !string.IsNullOrEmpty(PlayerPrefs.GetString(PREFS_UID));
    }

    // Load saved session from device
    public static UserSession LoadFromPlayerPrefs()
    {
        if (!HasStoredSession())
        {
            return null;
        }

        var session = new UserSession
        {
            uid = PlayerPrefs.GetString(PREFS_UID, string.Empty),
            userName = PlayerPrefs.GetString(PREFS_USER_NAME, string.Empty),
            avatarIndex = PlayerPrefs.GetInt(PREFS_AVATAR_INDEX, 0),
            isAnonymous = PlayerPrefs.GetInt(PREFS_IS_ANONYMOUS, 1) == 1,
            isEmailVerified = PlayerPrefs.GetInt(PREFS_IS_EMAIL_VERIFIED, 0) == 1,
            email = PlayerPrefs.GetString(PREFS_EMAIL, string.Empty),
            authToken = PlayerPrefs.GetString(PREFS_AUTH_TOKEN, string.Empty),
            lastLoginTimestamp = long.Parse(PlayerPrefs.GetString(PREFS_LAST_LOGIN, "0"))
        };

        return session;
    }

    // Save session to device so user stays logged in
    public void SaveToPlayerPrefs()
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[UserSession] Cannot save session: UID is empty");
            return;
        }

        PlayerPrefs.SetString(PREFS_UID, uid);
        PlayerPrefs.SetString(PREFS_USER_NAME, userName);
        PlayerPrefs.SetInt(PREFS_AVATAR_INDEX, avatarIndex);
        PlayerPrefs.SetInt(PREFS_IS_ANONYMOUS, isAnonymous ? 1 : 0);
        PlayerPrefs.SetInt(PREFS_IS_EMAIL_VERIFIED, isEmailVerified ? 1 : 0);
        PlayerPrefs.SetString(PREFS_EMAIL, email ?? string.Empty);
        PlayerPrefs.SetString(PREFS_AUTH_TOKEN, authToken ?? string.Empty);
        PlayerPrefs.SetString(PREFS_LAST_LOGIN, lastLoginTimestamp.ToString());
        PlayerPrefs.Save();
    }

    // Delete saved session from device
    public static void ClearFromPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(PREFS_UID);
        PlayerPrefs.DeleteKey(PREFS_USER_NAME);
        PlayerPrefs.DeleteKey(PREFS_AVATAR_INDEX);
        PlayerPrefs.DeleteKey(PREFS_IS_ANONYMOUS);
        PlayerPrefs.DeleteKey(PREFS_IS_EMAIL_VERIFIED);
        PlayerPrefs.DeleteKey(PREFS_EMAIL);
        PlayerPrefs.DeleteKey(PREFS_AUTH_TOKEN);
        PlayerPrefs.DeleteKey(PREFS_LAST_LOGIN);
        PlayerPrefs.Save();
    }

    // Check if session has all required info
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(authToken);
    }

    // Clear all session data
    public void Clear()
    {
        uid = string.Empty;
        userName = string.Empty;
        avatarIndex = 0;
        isAnonymous = true;
        isEmailVerified = false;
        email = string.Empty;
        authToken = string.Empty;
        lastLoginTimestamp = 0;
    }
}


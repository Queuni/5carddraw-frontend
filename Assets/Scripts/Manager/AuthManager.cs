using System;
using System.Collections;
using UnityEngine;

// Data we get back from the server after signing in
[Serializable]
public class AuthResponseData
{
    public string uid;
    public string userName; // Backend now uses userName
    public int avatarIndex;
    public bool isAnonymous;
    public bool isEmailVerified;
    public string email;
    public string token;
}

// Data we send to the server for signup
[Serializable]
public class SignupRequestData
{
    public string email;
    public string password;
    public string userName; // Backend now uses userName
    public int avatarIndex;
}

// Data we send to the server for verify-token
[Serializable]
public class VerifyTokenRequestData
{
    public string idToken;
}

// Data we send to the server for signin (email or username + password)
[Serializable]
public class SigninRequestData
{
    public string identifier;
    public string password;
}

// Handles user authentication - sign in, sign out, check if user is guest
// WebGL-compatible: Uses only REST API calls, no Firebase SDK
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public event Action<UserSession> OnAuthStateChanged;
    public event Action OnSignOut;

    private UserSession currentSession;
    private APIService apiService;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        apiService = GetComponent<APIService>();
        if (apiService == null)
        {
            apiService = gameObject.AddComponent<APIService>();
        }

        // Try to restore session on startup
        RestoreSession();
    }

    // Helper method to make API request and wait for response
    private IEnumerator MakeAuthRequest<TRequest, TResponse>(
        string endpoint,
        TRequest requestData,
        Action<TResponse> onSuccess,
        Action<string> onError,
        bool requiresAuth = false)
    {
        bool requestCompleted = false;
        bool requestSuccess = false;
        TResponse responseData = default(TResponse);
        string errorMessage = null;

        apiService.Post<TResponse>(
            endpoint,
            requestData,
            (data) =>
            {
                responseData = data;
                requestSuccess = true;
                requestCompleted = true;
            },
            (error) =>
            {
                errorMessage = error;
                requestSuccess = false;
                requestCompleted = true;
            },
            requiresAuth
        );

        while (!requestCompleted)
        {
            yield return null;
        }

        if (requestSuccess && responseData != null)
        {
            onSuccess?.Invoke(responseData);
        }
        else
        {
            onError?.Invoke(errorMessage ?? "Request failed");
        }
    }

    // Helper method to create and save user session from response
    private void CreateSessionFromResponse(AuthResponseData responseData, string successMessage)
    {
        currentSession = new UserSession
        {
            uid = responseData.uid,
            userName = responseData.userName,
            avatarIndex = responseData.avatarIndex,
            isAnonymous = responseData.isAnonymous,
            isEmailVerified = responseData.isEmailVerified,
            email = responseData.email ?? string.Empty,
            authToken = responseData.token,
            lastLoginTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        currentSession.SaveToPlayerPrefs();
        apiService.SetAuthToken(responseData.token);
        OnAuthStateChanged?.Invoke(currentSession);
        
        DebugConfig.Log($"[AuthManager] {successMessage}: {currentSession.userName}");
    }

    // Sign in as guest user (WebGL-compatible: uses backend API)
    public void SignInAnonymously(Action<UserSession> onSuccess, Action<string> onError)
    {
        StartCoroutine(SignInAnonymouslyCoroutine(onSuccess, onError));
    }

    private IEnumerator SignInAnonymouslyCoroutine(Action<UserSession> onSuccess, Action<string> onError)
    {
        // Check if we already have a valid anonymous session
        if (currentSession != null && currentSession.IsValid() && currentSession.isAnonymous)
        {
            DebugConfig.Log("[AuthManager] Already signed in as anonymous user");
            onSuccess?.Invoke(currentSession);
            yield break;
        }

        // Check if there's a stored anonymous session
        if (UserSession.HasStoredSession())
        {
            var storedSession = UserSession.LoadFromPlayerPrefs();
            if (storedSession != null && storedSession.IsValid() && storedSession.isAnonymous)
            {
                // Restore the stored anonymous session
                currentSession = storedSession;
                apiService.SetAuthToken(storedSession.authToken);
                OnAuthStateChanged?.Invoke(currentSession);
                DebugConfig.Log($"[AuthManager] Restored anonymous session: {currentSession.userName}");
                onSuccess?.Invoke(currentSession);
                yield break;
            }
        }

        DebugConfig.Log("[AuthManager] Creating anonymous user via backend API...");

        AuthResponseData responseData = null;
        bool success = false;

        yield return StartCoroutine(MakeAuthRequest<object, AuthResponseData>(
            "/auth/create-anonymous",
            null,
            (data) =>
            {
                responseData = data;
                success = true;
            },
            (error) =>
            {
                onError?.Invoke(error ?? "Guest sign in failed");
            },
            requiresAuth: false
        ));

        if (success && responseData != null)
        {
            CreateSessionFromResponse(responseData, "Guest mode sign-in successful");
            onSuccess?.Invoke(currentSession);
        }
    }

    // Load saved session when game starts
    private void RestoreSession()
    {
        if (UserSession.HasStoredSession())
        {
            currentSession = UserSession.LoadFromPlayerPrefs();
            if (currentSession != null && currentSession.IsValid())
            {
                apiService.SetAuthToken(currentSession.authToken);
                OnAuthStateChanged?.Invoke(currentSession);
                DebugConfig.Log($"[AuthManager] Session restored for user: {currentSession.userName}");
            }
            else
            {
                // Session is invalid, clear it
                UserSession.ClearFromPlayerPrefs();
                currentSession = null;
            }
        }
    }

    // Get current user's session info
    public UserSession GetCurrentSession()
    {
        return currentSession;
    }

    // Check if user is logged in
    public bool IsAuthenticated()
    {
        return currentSession != null && currentSession.IsValid();
    }

    // Check if current user is a guest
    public bool IsGuestMode()
    {
        return IsAuthenticated() && currentSession.isAnonymous;
    }

    // Get the authentication token
    public string GetIdToken()
    {
        if (currentSession != null && !string.IsNullOrEmpty(currentSession.authToken))
        {
            return currentSession.authToken;
        }
        return null;
    }

    // Refresh the token (for WebGL, we'll verify with backend)
    public void RefreshIdToken(Action<string> onSuccess, Action<string> onError)
    {
        string token = GetIdToken();
        if (!string.IsNullOrEmpty(token))
        {
            // Verify token with backend to check if it's still valid
            StartCoroutine(VerifyTokenCoroutine(token, onSuccess, onError));
        }
        else
        {
            onError?.Invoke("No token available");
        }
    }

    private IEnumerator VerifyTokenCoroutine(string token, Action<string> onSuccess, Action<string> onError)
    {
        var verifyTokenData = new VerifyTokenRequestData
        {
            idToken = token
        };

        AuthResponseData responseData = null;
        bool success = false;

        yield return StartCoroutine(MakeAuthRequest<VerifyTokenRequestData, AuthResponseData>(
            "/auth/verify-token",
            verifyTokenData,
            (data) =>
            {
                responseData = data;
                success = true;
            },
            (error) =>
            {
                onError?.Invoke(error ?? "Token refresh failed");
            },
            requiresAuth: false
        ));

        if (success && responseData != null && !string.IsNullOrEmpty(responseData.token))
        {
            // Update token in session
            if (currentSession != null)
            {
                currentSession.authToken = responseData.token;
                currentSession.SaveToPlayerPrefs();
                apiService.SetAuthToken(responseData.token);
            }
            onSuccess?.Invoke(responseData.token);
        }
    }

    // Sign up with email and password (WebGL-compatible: uses backend API)
    public void SignUpWithEmail(string email, string password, string userName, int avatarIndex, Action<UserSession> onSuccess, Action<string> onError)
    {
        StartCoroutine(SignUpWithEmailCoroutine(email, password, userName, avatarIndex, onSuccess, onError));
    }

    private IEnumerator SignUpWithEmailCoroutine(string email, string password, string userName, int avatarIndex, Action<UserSession> onSuccess, Action<string> onError)
    {
        // Validate input
        if (string.IsNullOrEmpty(email) || !Utils.IsValidEmail(email))
        {
            onError?.Invoke("Invalid email");
            yield break;
        }

        if (string.IsNullOrEmpty(password) || !Utils.IsValidPassword(password))
        {
            onError?.Invoke("Password too weak");
            yield break;
        }

        if (string.IsNullOrEmpty(userName) || userName.Length < 1 || userName.Length > 20)
        {
            onError?.Invoke("Invalid username");
            yield break;
        }

        DebugConfig.Log("[AuthManager] Creating account with email and password via backend API...");

        var signupData = new SignupRequestData
        {
            email = email,
            password = password,
            userName = userName,
            avatarIndex = avatarIndex
        };

        AuthResponseData responseData = null;
        bool success = false;

        yield return StartCoroutine(MakeAuthRequest<SignupRequestData, AuthResponseData>(
            "/auth/signup",
            signupData,
            (data) =>
            {
                responseData = data;
                success = true;
            },
            (error) =>
            {
                onError?.Invoke(error ?? "Sign up failed");
            },
            requiresAuth: false
        ));

        if (success && responseData != null)
        {
            CreateSessionFromResponse(responseData, "Account created successfully");
            onSuccess?.Invoke(currentSession);
        }
    }

    // Sign in with email and password (WebGL-compatible: uses backend API)
    public void SignInWithEmail(string identifier, string password, Action<UserSession> onSuccess, Action<string> onError)
    {
        StartCoroutine(SignInWithEmailCoroutine(identifier, password, onSuccess, onError));
    }

    private IEnumerator SignInWithEmailCoroutine(string identifier, string password, Action<UserSession> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(password))
        {
            onError?.Invoke("Enter password");
            yield break;
        }

        DebugConfig.Log($"[AuthManager] Signing in via backend API...");

        var signinRequest = new SigninRequestData
        {
            identifier = identifier,
            password = password
        };

        AuthResponseData responseData = null;
        bool success = false;

        yield return StartCoroutine(MakeAuthRequest<SigninRequestData, AuthResponseData>(
            "/auth/signin",
            signinRequest,
            (data) =>
            {
                responseData = data;
                success = true;
            },
            (error) =>
            {
                onError?.Invoke(error ?? "Sign in failed");
            },
            requiresAuth: false
        ));

        if (success && responseData != null)
        {
            CreateSessionFromResponse(responseData, "Sign in successful");
            onSuccess?.Invoke(currentSession);
        }
    }

    // Sign out and clear session
    public void SignOut()
    {
        // Clear local session
        if (currentSession != null)
        {
            UserSession.ClearFromPlayerPrefs();
            currentSession = null;
        }

        apiService.ClearAuthToken();
        OnSignOut?.Invoke();
        OnAuthStateChanged?.Invoke(null);
        
        DebugConfig.Log("[AuthManager] User signed out");
    }
}

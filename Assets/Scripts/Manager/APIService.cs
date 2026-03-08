using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Response format from the server
[Serializable]
public class APIResponse<T>
{
    public bool success;
    public T data;
    public APIError error;
}

[Serializable]
public class APIError
{
    public string code;
    public string message;
    public object details;
}

// Handles all communication with the backend server
public class APIService : MonoBehaviour
{
    public static APIService Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("Local backend URL (used in Unity Editor and development)")]
    [SerializeField] private string localBackendURL = "http://localhost:3000/api";
    
    [Tooltip("Production backend URL (used in builds)")]
    [SerializeField] private string productionBackendURL = "https://5carddraw.app/api";

    private string baseURL;
    private string authToken;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Automatically set backend URL based on environment
        InitializeBackendURL();
    }
    
    private void InitializeBackendURL()
    {
        // Use local backend in Unity Editor or development builds
        // Use production backend in release builds
        if (Application.isEditor || Debug.isDebugBuild)
        {
            baseURL = localBackendURL;
            DebugConfig.Log($"[APIService] Using local backend: {baseURL}");
        }
        else
        {
            baseURL = productionBackendURL;
            DebugConfig.Log($"[APIService] Using production backend: {baseURL}");
        }
    }

    // Set the auth token for API requests
    public void SetAuthToken(string token)
    {
        authToken = token;
    }

    // Clear the auth token
    public void ClearAuthToken()
    {
        authToken = null;
    }

    // Get the base URL
    public string GetBaseURL()
    {
        return baseURL;
    }

    // Change the base URL (for different environments)
    public void SetBaseURL(string url)
    {
        baseURL = url;
    }

    // Send POST request to server
    public void Post<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError, bool requiresAuth = false)
    {
        StartCoroutine(PostCoroutine(endpoint, data, onSuccess, onError, requiresAuth));
    }
    
    // Send PUT request to server
    public void Put<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError, bool requiresAuth = false)
    {
        StartCoroutine(PutCoroutine(endpoint, data, onSuccess, onError, requiresAuth));
    }
    
    // Send GET request to server
    public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError, bool requiresAuth = false)
    {
        StartCoroutine(GetCoroutine(endpoint, onSuccess, onError, requiresAuth));
    }

    // Send GET request to server and return raw response text
    public void GetRaw(string endpoint, Action<string> onSuccess, Action<string> onError, bool requiresAuth = false)
    {
        StartCoroutine(GetRawCoroutine(endpoint, onSuccess, onError, requiresAuth));
    }

    // Send DELETE request to server
    public void Delete<T>(string endpoint, Action<T> onSuccess, Action<string> onError, bool requiresAuth = false)
    {
        StartCoroutine(DeleteCoroutine(endpoint, onSuccess, onError, requiresAuth));
    }

    private void SetAuthHeader(UnityWebRequest request, bool requiresAuth)
    {
        if (!string.IsNullOrEmpty(authToken))
        {
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
        }
        else if (requiresAuth)
        {
            // Backend will verify token only if endpoint requires authentication
        }
    }

    private static string GetResponseText(UnityWebRequest request)
    {
        return request.downloadHandler != null ? request.downloadHandler.text : "";
    }

    private IEnumerator PostCoroutine<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError, bool requiresAuth)
    {
        string url = $"{baseURL}{endpoint}";
        string jsonData = data != null ? Utils.ObjectToJson(data) : "{}";
        
        // Log request only in debug mode
        if (DebugConfig.EnableAPILogs)
        {
            Debug.Log($"[APIService] POST {url}");
            Debug.Log($"[APIService] Request body: {jsonData}");
        }

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(request, requiresAuth);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessResponse<T>(GetResponseText(request), onSuccess, onError);
            }
            else
            {
                HandleErrorResponse(request, onError);
            }
        }
    }

    private IEnumerator PutCoroutine<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError, bool requiresAuth)
    {
        string url = $"{baseURL}{endpoint}";
        string jsonData = data != null ? Utils.ObjectToJson(data) : "{}";
        
        // Log request only in debug mode
        if (DebugConfig.EnableAPILogs)
        {
            Debug.Log($"[APIService] PUT {url}");
            Debug.Log($"[APIService] Request body: {jsonData}");
        }
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(request, requiresAuth);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessResponse<T>(GetResponseText(request), onSuccess, onError);
            }
            else
            {
                HandleErrorResponse(request, onError);
            }
        }
    }

    private IEnumerator GetCoroutine<T>(string endpoint, Action<T> onSuccess, Action<string> onError, bool requiresAuth)
    {
        string url = $"{baseURL}{endpoint}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            SetAuthHeader(request, requiresAuth);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessResponse<T>(GetResponseText(request), onSuccess, onError);
            }
            else
            {
                HandleErrorResponse(request, onError);
            }
        }
    }

    private IEnumerator GetRawCoroutine(string endpoint, Action<string> onSuccess, Action<string> onError, bool requiresAuth)
    {
        string url = $"{baseURL}{endpoint}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            SetAuthHeader(request, requiresAuth);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(GetResponseText(request));
            }
            else
            {
                HandleErrorResponse(request, onError);
            }
        }
    }

    private IEnumerator DeleteCoroutine<T>(string endpoint, Action<T> onSuccess, Action<string> onError, bool requiresAuth)
    {
        string url = $"{baseURL}{endpoint}";

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            SetAuthHeader(request, requiresAuth);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessResponse<T>(GetResponseText(request), onSuccess, onError);
            }
            else
            {
                HandleErrorResponse(request, onError);
            }
        }
    }

    private void HandleSuccessResponse<T>(string responseText, Action<T> onSuccess, Action<string> onError)
    {
        try
        {
            APIResponse<T> apiResponse = Utils.JsonToObject<APIResponse<T>>(responseText);

            if (apiResponse != null && apiResponse.success && apiResponse.data != null)
            {
                onSuccess?.Invoke(apiResponse.data);
                return;
            }

            if (apiResponse != null && apiResponse.data == null && apiResponse.error == null)
            {
                // Fallback: some endpoints may return raw data without wrapper
                T rawData = Utils.JsonToObject<T>(responseText);
                if (rawData != null)
                {
                    onSuccess?.Invoke(rawData);
                    return;
                }
            }

            string errorMessage = "An error occurred";

            if (apiResponse?.error != null)
            {
                // Use the error message from backend
                errorMessage = apiResponse.error.message ?? "An error occurred";

                // Map common error codes to short messages
                if (apiResponse.error.code == "USER_NOT_FOUND")
                {
                    errorMessage = "Email not found";
                }
                else if (apiResponse.error.code == "USERNAME_ALREADY_EXISTS")
                {
                    errorMessage = "Username taken";
                }
                else if (apiResponse.error.code == "INVALID_CREDENTIALS")
                {
                    errorMessage = "Invalid credentials";
                }
                else if (apiResponse.error.code == "INTERNAL_ERROR")
                {
                    errorMessage = "Server error";
                }
            }

            if (DebugConfig.EnableDetailedErrors)
            {
                Debug.LogError($"[APIService] API Error: {errorMessage} (Code: {apiResponse?.error?.code})");
            }
            onError?.Invoke(errorMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse response: {ex.Message}\nResponse: {responseText}");
            onError?.Invoke("Failed to parse server response");
        }
    }

    private void HandleErrorResponse(UnityWebRequest request, Action<string> onError)
    {
        string errorMessage = "Network error occurred";

        // Try to parse error response from backend (for 400, 401, etc.)
        if (request.responseCode >= 400 && request.responseCode < 500)
        {
            string responseText = request.downloadHandler?.text;
            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    APIResponse<object> apiResponse = Utils.JsonToObject<APIResponse<object>>(responseText);
                    if (apiResponse.error != null)
                    {
                        errorMessage = apiResponse.error.message ?? "Request failed";
                        
                        // Map common error codes
                        if (apiResponse.error.code == "MISSING_REQUIRED_FIELD")
                        {
                            errorMessage = "Missing required field";
                        }
                        else if (apiResponse.error.code == "EMAIL_ALREADY_EXISTS")
                        {
                            errorMessage = "Email already used";
                        }
                        else if (apiResponse.error.code == "INVALID_INPUT")
                        {
                            errorMessage = "Invalid input";
                        }
                        else if (apiResponse.error.code == "USER_NOT_FOUND")
                        {
                            errorMessage = "Email not found";
                        }
                        else if (apiResponse.error.code == "INVALID_CREDENTIALS")
                        {
                            errorMessage = "Invalid email or password";
                        }
                    }
                }
                catch
                {
                    // If parsing fails, use status code based message
                    errorMessage = GetErrorMessageFromStatusCode(request.responseCode);
                }
            }
            else
            {
                // No response body, use status code based message
                errorMessage = GetErrorMessageFromStatusCode(request.responseCode);
            }
        }
        else if (request.responseCode >= 500)
        {
            errorMessage = "Server error";
            string responseText = request.downloadHandler?.text;
            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    APIResponse<object> apiResponse = Utils.JsonToObject<APIResponse<object>>(responseText);
                    if (apiResponse?.error != null && !string.IsNullOrEmpty(apiResponse.error.message))
                    {
                        errorMessage = apiResponse.error.message;
                    }
                }
                catch
                {
                    // Keep "Server error" if parsing fails
                }
            }
        }
        else if (!string.IsNullOrEmpty(request.error))
        {
            errorMessage = request.error;
        }

        if (DebugConfig.EnableDetailedErrors)
        {
            Debug.LogError($"[APIService] Request failed: {request.responseCode} - {errorMessage}");
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                Debug.LogError($"[APIService] Response body: {request.downloadHandler.text}");
            }
        }
        onError?.Invoke(errorMessage);
    }
    
    // Helper method to avoid duplicate status code handling
    private string GetErrorMessageFromStatusCode(long statusCode)
    {
        return statusCode switch
        {
            400 => "Invalid request",
            401 => "Auth failed",
            404 => "Not found",
            _ => "Request failed"
        };
    }
}


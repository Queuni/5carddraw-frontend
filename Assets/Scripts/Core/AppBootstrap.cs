using UnityEngine;

// Ensures core managers exist across scenes and provides startup helpers.
public static class AppBootstrap
{
    private const string ManagersRootName = "AppManagers";
    private static GameObject managersRoot;

    public static void EnsureManagers()
    {
        if (AuthManager.Instance != null && MultiplayerManager.Instance != null && WebSocketManager.Instance != null)
        {
            return;
        }

        if (managersRoot == null)
        {
            managersRoot = GameObject.Find(ManagersRootName);
            if (managersRoot == null)
            {
                managersRoot = new GameObject(ManagersRootName);
                Object.DontDestroyOnLoad(managersRoot);
            }
        }

        EnsureComponent<AuthManager>(managersRoot);
        EnsureComponent<MultiplayerManager>(managersRoot);
        EnsureComponent<WebSocketManager>(managersRoot);
    }

    public static void ConnectSocket(UserSession session)
    {
        if (session == null)
        {
            return;
        }

        EnsureManagers();
        if (WebSocketManager.Instance != null)
        {
            Debug.Log("connection...");
            WebSocketManager.Instance.Connect(session.uid, session.userName, session.authToken);
        }
    }

    private static T EnsureComponent<T>(GameObject root) where T : Component
    {
        T existing = Object.FindObjectOfType<T>();
        if (existing != null)
        {
            return existing;
        }

        return root.AddComponent<T>();
    }
}

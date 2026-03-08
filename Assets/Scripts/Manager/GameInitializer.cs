using UnityEngine;

// Creates the managers when game starts so they're available everywhere
public class GameInitializer : MonoBehaviour
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        // Create a GameObject to hold our managers
        GameObject managersObject = new GameObject("Managers");
        DontDestroyOnLoad(managersObject);

        // Add AuthManager if it doesn't exist
        if (AuthManager.Instance == null)
        {
            managersObject.AddComponent<AuthManager>();
        }

        // Add APIService if it doesn't exist
        if (APIService.Instance == null)
        {
            managersObject.AddComponent<APIService>();
        }

        isInitialized = true;
        Debug.Log("[GameInitializer] Core managers initialized");
    }
}


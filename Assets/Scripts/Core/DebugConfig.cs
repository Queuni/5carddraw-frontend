using UnityEngine;

// Centralized debug configuration for production builds
public static class DebugConfig
{
    // Set to false in production builds to disable debug logs
    public static bool EnableDebugLogs = true;
    
    // Set to false in production to disable verbose API logging
    public static bool EnableAPILogs = true;
    
    // Set to false in production to disable detailed error logging
    public static bool EnableDetailedErrors = true;
    
    // Conditional debug log
    public static void Log(string message)
    {
        if (EnableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    // Conditional debug log error
    public static void LogError(string message)
    {
        if (EnableDetailedErrors)
        {
            Debug.LogError(message);
        }
    }
    
    // Conditional debug log warning
    public static void LogWarning(string message)
    {
        if (EnableDetailedErrors)
        {
            Debug.LogWarning(message);
        }
    }
}


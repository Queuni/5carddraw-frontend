using UnityEngine;

public enum AnimationSpeedOption
{
    Normal = 0,
    Fast = 1
}

public static class GameSettings
{
    private const string PrefAnimationSpeed = "settings_animation_speed";
    private const string PrefConfirmBeforeFold = "settings_confirm_before_fold";
    private const string PrefCardAutoSort = "settings_card_auto_sort";

    private static bool isLoaded;

    public static AnimationSpeedOption AnimationSpeed { get; set; } = AnimationSpeedOption.Normal;
    public static bool ConfirmBeforeFold { get; set; } = true;
    public static bool CardAutoSort { get; set; } = true;

    public static float AnimationSpeedMultiplier =>
        AnimationSpeed == AnimationSpeedOption.Fast ? 1.5f : 1.0f;

    public static float GetAnimationDuration(float baseDuration)
    {
        return baseDuration / AnimationSpeedMultiplier;
    }

    public static void Load()
    {
        if (isLoaded)
        {
            return;
        }

        AnimationSpeed = (AnimationSpeedOption)PlayerPrefs.GetInt(PrefAnimationSpeed, (int)AnimationSpeedOption.Normal);
        ConfirmBeforeFold = PlayerPrefs.GetInt(PrefConfirmBeforeFold, 1) == 1;
        CardAutoSort = PlayerPrefs.GetInt(PrefCardAutoSort, 1) == 1;
        isLoaded = true;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(PrefAnimationSpeed, (int)AnimationSpeed);
        PlayerPrefs.SetInt(PrefConfirmBeforeFold, ConfirmBeforeFold ? 1 : 0);
        PlayerPrefs.SetInt(PrefCardAutoSort, CardAutoSort ? 1 : 0);
        PlayerPrefs.Save();
        isLoaded = true;
    }
}

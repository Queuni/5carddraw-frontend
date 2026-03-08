using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingScene : MonoBehaviour
{
    public Button saveButton;
    public Button backButton;
    public Button animationNormalButton;
    public Button animationFastButton;
    public Button confirmOnButton;
    public Button confirmOffButton;
    public Button sortOnButton;
    public Button sortOffButton;

    [Header("Button Visuals")]
    [Range(0.1f, 1f)]
    public float activeAlpha = 1f;
    [Range(0.1f, 1f)]
    public float inactiveAlpha = 0.1f;

    private AnimationSpeedOption pendingAnimationSpeed;
    private bool pendingConfirmBeforeFold;
    private bool pendingCardAutoSort;
    private bool hasPendingChanges;

    private void Start()
    {
        GameSettings.Load();
        InitializePendingSettings();
        BindButtons();
        RefreshVisuals();
        UpdateSaveButtonState();
    }

    private void InitializePendingSettings()
    {
        pendingAnimationSpeed = GameSettings.AnimationSpeed;
        pendingConfirmBeforeFold = GameSettings.ConfirmBeforeFold;
        pendingCardAutoSort = GameSettings.CardAutoSort;
        hasPendingChanges = false;
    }

    private void BindButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (animationNormalButton != null)
        {
            animationNormalButton.onClick.AddListener(() => SetAnimationSpeed(AnimationSpeedOption.Normal));
        }
        if (animationFastButton != null)
        {
            animationFastButton.onClick.AddListener(() => SetAnimationSpeed(AnimationSpeedOption.Fast));
        }

        if (confirmOnButton != null)
        {
            confirmOnButton.onClick.AddListener(() => SetConfirmBeforeFold(true));
        }
        if (confirmOffButton != null)
        {
            confirmOffButton.onClick.AddListener(() => SetConfirmBeforeFold(false));
        }

        if (sortOnButton != null)
        {
            sortOnButton.onClick.AddListener(() => SetCardAutoSort(true));
        }
        if (sortOffButton != null)
        {
            sortOffButton.onClick.AddListener(() => SetCardAutoSort(false));
        }
    }

    private void SetAnimationSpeed(AnimationSpeedOption option)
    {
        pendingAnimationSpeed = option;
        UpdatePendingState();
        RefreshVisuals();
    }

    private void SetConfirmBeforeFold(bool enabled)
    {
        pendingConfirmBeforeFold = enabled;
        UpdatePendingState();
        RefreshVisuals();
    }

    private void SetCardAutoSort(bool enabled)
    {
        pendingCardAutoSort = enabled;
        UpdatePendingState();
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        SetButtonAlpha(animationNormalButton, pendingAnimationSpeed == AnimationSpeedOption.Normal);
        SetButtonAlpha(animationFastButton, pendingAnimationSpeed == AnimationSpeedOption.Fast);
        SetButtonAlpha(confirmOnButton, pendingConfirmBeforeFold);
        SetButtonAlpha(confirmOffButton, !pendingConfirmBeforeFold);
        SetButtonAlpha(sortOnButton, pendingCardAutoSort);
        SetButtonAlpha(sortOffButton, !pendingCardAutoSort);
    }

    private void SetButtonAlpha(Button button, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = isActive ? activeAlpha : inactiveAlpha;
        image.color = color;
    }

    private void OnSaveClicked()
    {
        ApplyPendingSettings();
        GameSettings.Save();

        AlertBar.Instance.ShowMessage("Settings saved successfully.");

        Utils.LoadScene("MainMenuScene");
    }

    private void OnBackClicked()
    {
        Utils.LoadScene("MainMenuScene");
    }

    private void ApplyPendingSettings()
    {
        GameSettings.AnimationSpeed = pendingAnimationSpeed;
        GameSettings.ConfirmBeforeFold = pendingConfirmBeforeFold;
        GameSettings.CardAutoSort = pendingCardAutoSort;
        hasPendingChanges = false;
        UpdateSaveButtonState();
    }

    private void UpdatePendingState()
    {
        hasPendingChanges = pendingAnimationSpeed != GameSettings.AnimationSpeed
            || pendingConfirmBeforeFold != GameSettings.ConfirmBeforeFold
            || pendingCardAutoSort != GameSettings.CardAutoSort;
        UpdateSaveButtonState();
    }

    private void UpdateSaveButtonState()
    {
        if (saveButton == null)
        {
            return;
        }

        saveButton.interactable = hasPendingChanges;
    }
}

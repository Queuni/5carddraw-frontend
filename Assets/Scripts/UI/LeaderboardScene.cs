using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardScene : MonoBehaviour
{
    private const int MaxRowsPerMode = 30;
    private const float ActiveButtonAlpha = 1f;
    private const float InactiveButtonAlpha = 0.1f;

    public Button backButton;
    public Button SearchButton;
    public Button singleModeButton;
    public Button multiModeButton;
    public TMP_InputField searchInput;
    public SingleModeItem singleModeItemPrefab;
    public MultiModeItem multiModeItemPrefab;
    public SingleModeItem singleModelHeaderItem;
    public MultiModeItem multiModelHeaderItem;
    public Transform scrollContent;

    [SerializeField] private List<SingleModeRecord> singleModeEntries = new List<SingleModeRecord>();
    [SerializeField] private List<MultiModeRecord> multiModeEntries = new List<MultiModeRecord>();

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private LeaderboardMode currentMode = LeaderboardMode.Single;
    private bool isMultiModeLoading;

    // Start is called before the first frame update
    void Start()
    {
        BindButtons();
        SetMode(LeaderboardMode.Single);
    }

    private void BindButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (SearchButton != null)
        {
            SearchButton.onClick.RemoveAllListeners();
            SearchButton.onClick.AddListener(OnSearchClicked);
        }

        if (singleModeButton != null)
        {
            singleModeButton.onClick.RemoveAllListeners();
            singleModeButton.onClick.AddListener(() => SetMode(LeaderboardMode.Single));
        }

        if (multiModeButton != null)
        {
            multiModeButton.onClick.RemoveAllListeners();
            multiModeButton.onClick.AddListener(() => SetMode(LeaderboardMode.Multi));
        }
    }

    private void SetMode(LeaderboardMode mode)
    {
        currentMode = mode;
        UpdateButtonVisuals();
        UpdateHeaders();
        if (currentMode == LeaderboardMode.Multi)
        {
            FetchMultiLeaderboard();
        }
        else
        {
            RefreshLeaderboard();
        }
    }

    private void UpdateButtonVisuals()
    {
        SetButtonAlpha(singleModeButton, currentMode == LeaderboardMode.Single ? ActiveButtonAlpha : InactiveButtonAlpha);
        SetButtonAlpha(multiModeButton, currentMode == LeaderboardMode.Multi ? ActiveButtonAlpha : InactiveButtonAlpha);
    }

    private void SetButtonAlpha(Button button, float alpha)
    {
        if (button == null || button.image == null)
        {
            return;
        }

        Color color = button.image.color;
        color.a = alpha;
        button.image.color = color;
    }

    private void UpdateHeaders()
    {
        if (singleModelHeaderItem != null)
        {
            singleModelHeaderItem.gameObject.SetActive(currentMode == LeaderboardMode.Single);
        }

        if (multiModelHeaderItem != null)
        {
            multiModelHeaderItem.gameObject.SetActive(currentMode == LeaderboardMode.Multi);
        }
    }

    private void OnSearchClicked()
    {
        if (currentMode == LeaderboardMode.Multi)
        {
            FetchMultiLeaderboard();
            return;
        }

        RefreshLeaderboard();
    }

    private void RefreshLeaderboard()
    {
        ClearSpawnedItems();

        string query = searchInput != null ? searchInput.text : string.Empty;
        int shownCount = 0;
        if (currentMode == LeaderboardMode.Single)
        {
            shownCount = PopulateSingleMode(query);
        }
        else
        {
            shownCount = PopulateMultiMode(query);
        }

        if (shownCount == 0 && AlertBar.Instance != null)
        {
            AlertBar.Instance.ShowMessage("No leaderboard records yet.");
        }
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i]);
            }
        }
        spawnedItems.Clear();
    }

    private int PopulateSingleMode(string query)
    {
        if (singleModeItemPrefab == null || scrollContent == null)
        {
            return 0;
        }

        List<SingleModeRecord> dataSource = LoadSingleModeEntries();
        int addedCount = 0;
        for (int i = 0; i < dataSource.Count && addedCount < MaxRowsPerMode; i++)
        {
            SingleModeRecord entry = dataSource[i];
            if (!MatchesSingleMode(entry, query))
            {
                continue;
            }

            SingleModeItem item = Instantiate(singleModeItemPrefab, scrollContent);
            item.SetItem(entry.date, entry.winChips, entry.winner);
            spawnedItems.Add(item.gameObject);
            addedCount++;
        }

        return addedCount;
    }

    private int PopulateMultiMode(string query)
    {
        if (multiModeItemPrefab == null || scrollContent == null)
        {
            return 0;
        }

        List<MultiModeRecord> dataSource = LoadMultiModeEntries();
        int addedCount = 0;
        for (int i = 0; i < dataSource.Count && addedCount < MaxRowsPerMode; i++)
        {
            MultiModeRecord entry = dataSource[i];
            if (!MatchesMultiMode(entry, query))
            {
                continue;
            }

            MultiModeItem item = Instantiate(multiModeItemPrefab, scrollContent);
            string rank = string.IsNullOrEmpty(entry.rank) ? (addedCount + 1).ToString() : entry.rank;
            item.SetItem(rank, entry.player, entry.winChips, entry.played);
            spawnedItems.Add(item.gameObject);
            addedCount++;
        }

        return addedCount;
    }

    private bool MatchesSingleMode(SingleModeRecord entry, string query)
    {
        if (entry == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string needle = query.Trim().ToLowerInvariant();
        return ContainsQuery(entry.date, needle)
            || ContainsQuery(entry.winChips, needle)
            || ContainsQuery(entry.winner, needle);
    }

    private List<SingleModeRecord> LoadSingleModeEntries()
    {
        List<SingleModeRecord> stored = LeaderboardStorage.LoadSingleModeRecords();
        return stored != null && stored.Count > 0 ? stored : singleModeEntries;
    }

    private bool MatchesMultiMode(MultiModeRecord entry, string query)
    {
        if (entry == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string needle = query.Trim().ToLowerInvariant();
        return ContainsQuery(entry.rank, needle)
            || ContainsQuery(entry.player, needle)
            || ContainsQuery(entry.winChips, needle)
            || ContainsQuery(entry.played, needle);
    }

    private List<MultiModeRecord> LoadMultiModeEntries()
    {
        return multiModeEntries;
    }

    private bool ContainsQuery(string value, string needle)
    {
        return !string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains(needle);
    }

    private void OnBackClicked()
    {
        Utils.LoadScene("MainMenuScene");
    }

    private void FetchMultiLeaderboard()
    {
        if (isMultiModeLoading)
        {
            return;
        }

        if (APIService.Instance == null)
        {
            RefreshLeaderboard();
            return;
        }

        isMultiModeLoading = true;
        APIService.Instance.GetRaw(
            $"/multiplayer/leaderboard?limit={MaxRowsPerMode}",
            (responseText) =>
            {
                isMultiModeLoading = false;
                multiModeEntries = ParseMultiLeaderboardFromRawJson(responseText);
                RefreshLeaderboard();
            },
            (error) =>
            {
                isMultiModeLoading = false;
                if (AlertBar.Instance != null && !string.IsNullOrEmpty(error))
                {
                    AlertBar.Instance.ShowMessage(error);
                }
                RefreshLeaderboard();
            }
        );
    }

    /// <summary>
    /// Parse leaderboard from raw JSON so we get winCount reliably. Unity JsonUtility often
    /// does not fill int/string fields in nested list elements (data.entries[].winCount).
    /// </summary>
    private List<MultiModeRecord> ParseMultiLeaderboardFromRawJson(string json)
    {
        List<MultiModeRecord> result = new List<MultiModeRecord>();
        if (string.IsNullOrEmpty(json))
        {
            return result;
        }

        MatchCollection rankMatches = Regex.Matches(json, "\"rank\":(\\d+)");
        MatchCollection playerMatches = Regex.Matches(json, "\"player\":\"([^\"]+)\"");
        MatchCollection winChipsMatches = Regex.Matches(json, "\"winChips\":(\\d+)");
        MatchCollection winsMatches = Regex.Matches(json, "\"wins\":(\\d+)");

        int count = rankMatches.Count;
        if (count == 0 || count != playerMatches.Count || count != winChipsMatches.Count || count != winsMatches.Count)
        {
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            result.Add(new MultiModeRecord
            {
                rank = rankMatches[i].Groups[1].Value,
                player = playerMatches[i].Groups[1].Value,
                winChips = winChipsMatches[i].Groups[1].Value,
                played = winsMatches[i].Groups[1].Value
            });

            
        }

        return result;
    }

    [System.Serializable]
    private class MultiModeRecord
    {
        public string rank;
        public string player;
        public string winChips;
        public string played;
    }

    private enum LeaderboardMode
    {
        Single,
        Multi
    }
}

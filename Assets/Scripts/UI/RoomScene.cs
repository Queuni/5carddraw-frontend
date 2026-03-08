using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomScene : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 5f;
    private const int MaxRoomNameLength = 24;

    [Header("UI References")]
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Button joinButton;
    public Button backButton;
    public Transform roomsContent;
    public RoomItem roomItemPrefab;

    private readonly List<RoomEntry> roomEntries = new List<RoomEntry>();
    private string selectedRoomId;
    private Image selectedEntryBackground;
    private Coroutine refreshCoroutine;
    private bool isRefreshing;

    private Color entryNormalColor = new Color(0.1f, 0.12f, 0.12f, 0.35f);
    private Color entrySelectedColor = new Color(0.2f, 0.45f, 0.2f, 0.6f);

    private readonly Dictionary<string, RoomEntry> roomEntryMap = new Dictionary<string, RoomEntry>();

    private void Start()
    {
        Spinner.Instance.Hide();
        BindUI();
        UpdateJoinButtonState();
    }

    private void OnEnable()
    {
        StartRefreshLoop();
    }

    private void OnDisable()
    {
        StopRefreshLoop();
    }

    private void BindUI()
    {
        if (roomNameInput != null)
        {
            roomNameInput.characterLimit = MaxRoomNameLength;
            roomNameInput.onValueChanged.AddListener(HandleRoomNameChanged);
        }

        if (createButton != null)
        {
            createButton.onClick.AddListener(OnCreateClicked);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void StartRefreshLoop()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }
        refreshCoroutine = StartCoroutine(RefreshRoomsLoop());
    }

    private void StopRefreshLoop()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    private IEnumerator RefreshRoomsLoop()
    {
        while (true)
        {
            yield return RefreshRooms();
            yield return new WaitForSeconds(RefreshIntervalSeconds);
        }
    }

    private IEnumerator RefreshRooms()
    {
        if (isRefreshing || APIService.Instance == null)
        {
            yield break;
        }

        isRefreshing = true;
        bool completed = false;
        string errorMessage = null;
        List<RoomListItem> rooms = null;

        APIService.Instance.Get<RoomListResponse>(
            "/multiplayer/rooms",
            (response) =>
            {
                rooms = response != null ? response.rooms : null;
                completed = true;
            },
            (error) =>
            {
                errorMessage = error;
                completed = true;
            },
            false
        );

        while (!completed)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning($"[RoomScene] Failed to refresh rooms: {errorMessage}");
        }
        else
        {
            UpdateRoomEntries(rooms ?? new List<RoomListItem>());
        }

        isRefreshing = false;
    }

    private void UpdateRoomEntries(List<RoomListItem> rooms)
    {
        HashSet<string> incomingIds = new HashSet<string>(rooms.Select(room => room.roomId));

        for (int i = roomEntries.Count - 1; i >= 0; i--)
        {
            RoomEntry entry = roomEntries[i];
            if (!incomingIds.Contains(entry.roomId))
            {
                if (entry.item != null)
                {
                    Destroy(entry.item.gameObject);
                }
                roomEntryMap.Remove(entry.roomId);
                roomEntries.RemoveAt(i);

                if (selectedRoomId == entry.roomId)
                {
                    ClearSelection();
                }
            }
        }

        foreach (RoomListItem room in rooms)
        {
            if (roomEntryMap.TryGetValue(room.roomId, out RoomEntry entry))
            {
                entry.data = room;
                ApplyRoomData(entry);
                continue;
            }

            if (roomItemPrefab == null || roomsContent == null)
            {
                continue;
            }

            RoomItem item = Instantiate(roomItemPrefab, roomsContent);
            RoomEntry newEntry = new RoomEntry
            {
                roomId = room.roomId,
                item = item,
                background = item != null ? item.backgroundImage : null,
                data = room
            };

            roomEntries.Add(newEntry);
            roomEntryMap[room.roomId] = newEntry;

            ApplyRoomData(newEntry);
            BindRoomEntryClick(newEntry);
        }

        UpdateJoinButtonState();
    }

    private void ApplyRoomData(RoomEntry entry)
    {
        if (entry == null || entry.item == null || entry.data == null)
        {
            return;
        }

        int playerCount = entry.data.playerCount;
        if (playerCount <= 0 && entry.data.joinedCount > 0)
        {
            playerCount = entry.data.joinedCount;
        }
        int maxPlayers = entry.data.maxPlayers > 0 ? entry.data.maxPlayers : 4;
        entry.item.setRoomInfo(entry.data.roomName ?? "Room", entry.data.hostUsername ?? "Host", playerCount, maxPlayers);

        if (entry.background != null)
        {
            bool isSelected = !string.IsNullOrEmpty(selectedRoomId) && selectedRoomId == entry.roomId;
            entry.background.color = isSelected ? entrySelectedColor : entryNormalColor;
        }
    }

    private void BindRoomEntryClick(RoomEntry entry)
    {
        if (entry == null || entry.item == null || entry.item.button == null)
        {
            return;
        }

        entry.item.button.onClick.RemoveAllListeners();
        entry.item.button.onClick.AddListener(() => SelectRoom(entry));
    }

    private void SelectRoom(RoomEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (selectedEntryBackground != null)
        {
            selectedEntryBackground.color = entryNormalColor;
        }

        selectedRoomId = entry.roomId;
        selectedEntryBackground = entry.background;
        if (selectedEntryBackground != null)
        {
            selectedEntryBackground.color = entrySelectedColor;
        }

        UpdateJoinButtonState();
    }

    private void ClearSelection()
    {
        if (selectedEntryBackground != null)
        {
            selectedEntryBackground.color = entryNormalColor;
        }

        selectedRoomId = null;
        selectedEntryBackground = null;
        UpdateJoinButtonState();
    }

    private void UpdateJoinButtonState()
    {
        Utils.SetButtonInteractable(joinButton, !string.IsNullOrEmpty(selectedRoomId));
    }

    private void HandleRoomNameChanged(string value)
    {
        if (roomNameInput == null)
        {
            return;
        }

        string sanitized = SanitizeRoomName(value);
        if (roomNameInput.text != sanitized)
        {
            roomNameInput.SetTextWithoutNotify(sanitized);
        }
    }

    private string SanitizeRoomName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = string.Join(" ", value.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length > MaxRoomNameLength)
        {
            normalized = normalized.Substring(0, MaxRoomNameLength);
        }

        return normalized;
    }

    private void OnCreateClicked()
    {
        if (MultiplayerManager.Instance == null || AuthManager.Instance == null)
        {
            AlertBar.Instance.ShowMessage("Multiplayer not ready");
            return;
        }

        UserSession session = AuthManager.Instance.GetCurrentSession();
        if (session == null)
        {
            AlertBar.Instance.ShowMessage("Please sign in");
            Utils.LoadScene("LoginScene");
            return;
        }

        string roomName = roomNameInput != null ? SanitizeRoomName(roomNameInput.text) : string.Empty;
        if (string.IsNullOrEmpty(roomName))
        {
            AlertBar.Instance.ShowMessage("Enter room name");
            return;
        }

        Utils.SetButtonInteractable(createButton, false);
        Utils.SetButtonInteractable(joinButton, false);
        Spinner.Instance.Show();

        MultiplayerManager.Instance.CreateRoom(
            session.uid,
            session.userName,
            session.avatarIndex,
            roomName,
            (roomId) =>
            {
                Spinner.Instance.Hide();
                Utils.LoadScene("MultiPlayScene");
            },
            (error) =>
            {
                Spinner.Instance.Hide();
                Utils.SetButtonInteractable(createButton, true);
                UpdateJoinButtonState();
                AlertBar.Instance.ShowMessage(error ?? "Failed to create room");
            }
        );
    }

    private void OnJoinClicked()
    {
        if (string.IsNullOrEmpty(selectedRoomId))
        {
            AlertBar.Instance.ShowMessage("Select a room");
            return;
        }

        if (MultiplayerManager.Instance == null || AuthManager.Instance == null)
        {
            AlertBar.Instance.ShowMessage("Multiplayer not ready");
            return;
        }

        UserSession session = AuthManager.Instance.GetCurrentSession();
        if (session == null)
        {
            AlertBar.Instance.ShowMessage("Please sign in");
            Utils.LoadScene("LoginScene");
            return;
        }

        Utils.SetButtonInteractable(joinButton, false);
        Utils.SetButtonInteractable(createButton, false);
        Spinner.Instance.Show();

        MultiplayerManager.Instance.JoinRoom(
            selectedRoomId,
            session.uid,
            session.userName,
            session.avatarIndex,
            () =>
            {
                Spinner.Instance.Hide();
                Utils.LoadScene("MultiPlayScene");
            },
            (error) =>
            {
                Spinner.Instance.Hide();
                Utils.SetButtonInteractable(createButton, true);
                UpdateJoinButtonState();
                AlertBar.Instance.ShowMessage(error ?? "Failed to join room");
            }
        );
    }

    private void OnBackClicked()
    {
        Utils.LoadScene("MainMenuScene");
    }

    [Serializable]
    private class RoomListResponse
    {
        public List<RoomListItem> rooms;
    }

    [Serializable]
    private class RoomListItem
    {
        public string roomId;
        public string roomName;
        public string hostUsername;
        public int playerCount;
        public int joinedCount;
        public int maxPlayers;
        public string currentPhase;
        public bool isActive;
        public string createdAt;
    }

    private class RoomEntry
    {
        public string roomId;
        public RoomItem item;
        public Image background;
        public RoomListItem data;
    }
}

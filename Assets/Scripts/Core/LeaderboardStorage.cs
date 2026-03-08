using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SingleModeRecord
{
    public string date;
    public string winChips;
    public string winner; // "You" or "CPU"
}

public static class LeaderboardStorage
{
    private const string SingleModeFileName = "single_mode_leaderboard.json";

    public static List<SingleModeRecord> LoadSingleModeRecords()
    {
        string path = GetSingleModePath();
        if (!File.Exists(path))
        {
            return new List<SingleModeRecord>();
        }

        try
        {
            string json = File.ReadAllText(path);
            SingleModeRecordList wrapper = JsonUtility.FromJson<SingleModeRecordList>(json);
            return wrapper != null && wrapper.items != null ? wrapper.items : new List<SingleModeRecord>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load single mode leaderboard data: {e.Message}");
            return new List<SingleModeRecord>();
        }
    }

    public static void SaveSingleModeRecords(List<SingleModeRecord> records)
    {
        try
        {
            SingleModeRecordList wrapper = new SingleModeRecordList
            {
                items = records ?? new List<SingleModeRecord>()
            };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(GetSingleModePath(), json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to save single mode leaderboard data: {e.Message}");
        }
    }

    public static void AddSingleModeRecord(SingleModeRecord record, int maxRecords = 100)
    {
        if (record == null)
        {
            return;
        }

        List<SingleModeRecord> records = LoadSingleModeRecords();
        records.Insert(0, record);
        if (maxRecords > 0 && records.Count > maxRecords)
        {
            records.RemoveRange(maxRecords, records.Count - maxRecords);
        }
        SaveSingleModeRecords(records);
    }

    private static string GetSingleModePath()
    {
        return Path.Combine(Application.persistentDataPath, SingleModeFileName);
    }

    [Serializable]
    private class SingleModeRecordList
    {
        public List<SingleModeRecord> items = new List<SingleModeRecord>();
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    [Serializable]
    public class SaveData
    {
        public int money;
        public int profit;
        public List<string> bettingHistory = new List<string>();
        public List<int> winningNumbers = new List<int>();
    }

    // Where the save lives
    // If you *insist* on the project folder, switch back to Application.dataPath
    static string path = Path.Combine(Application.dataPath, "save.json");

    // ---- DEFAULT TEMPLATE (edit these to your needs) ----
    static SaveData GetDefaultSave()
    {
        return new SaveData
        {
            money = 1000,              // starting money
            profit = 0,                // starting profit
            bettingHistory = new List<string>(),
            winningNumbers = new List<int>()
        };
    }

    // Create the file on disk with default content and return it
    static SaveData CreateFreshSaveFile()
    {
        var data = GetDefaultSave();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Created fresh save at: " + path);
        return data;
    }

    public static void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Saved to: " + path);
    }

    public static SaveData Load()
    {
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) // corrupted or empty
                {
                    Debug.LogWarning("Save file invalid. Rebuilding with defaults...");
                    return CreateFreshSaveFile();
                }
                Debug.Log("Loaded from: " + path);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed: " + e.Message + "\nRebuilding with defaults...");
                return CreateFreshSaveFile();
            }
        }
        // No file -> create deterministic default save
        return CreateFreshSaveFile();
    }

    // Call this when the player selects "New Game"
    public static SaveData NewGame()
    {
        return CreateFreshSaveFile();
    }

    // ---------- ADD MONEY AND PROFIT ----------
    // NOTE: This overwrites values. If you want incremental, change to +=.
    public static void AddMoneyAndProfit(int amount, int profitChange)
    {
        var data = Load();
        data.money = amount;           // or: data.money += amount;
        data.profit = profitChange;    // or: data.profit += profitChange;
        Save(data);
    }

    // ---------- ADD BET ----------
    public static void AddWiningNumber(List<int> winningNumber, BetHistory betHistory)
    {
        var data = Load();
        int lastWinningLine = betHistory.winningNumbers.Count > 0 ? betHistory.winningNumbers[betHistory.winningNumbers.Count - 1] : -1;
        data.winningNumbers.Add(lastWinningLine);
        Save(data);
    }

    public static void AddBet(List<string> betText, BetHistory betHistory)
    {
        var data = Load();
        string lastBetLine = betHistory.bettingHistory.Count > 0 ? betHistory.bettingHistory[betHistory.bettingHistory.Count - 1] : null;
        data.bettingHistory.Add(lastBetLine);
        Save(data);
    }
}

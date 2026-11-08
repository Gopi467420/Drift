using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// The best scores of each mode, kept on this device. Each mode has its own board.
// OnlineLeaderboard holds the world board, and these fill in when it can't be reached.
public static class Leaderboard
{
    public const int MaxEntries = 10;

    private const int MaxNameLength = 10;
    private const string DefaultName = "PLAYER";
    private const string BoardKeyPrefix = "Leaderboard_";
    private const string NameKey = "PlayerName";

    [Serializable]
    public struct Entry
    {
        public string name;
        public int score;
        // Only the world board fills these in, and only for showing, so they aren't saved.
        [NonSerialized] public string mode;
        [NonSerialized] public bool isPlayer;
    }

    // JsonUtility can't save a bare list, so the list sits in a class.
    [Serializable]
    private class Board
    {
        public List<Entry> entries = new();
    }

    public static string PlayerName
    {
        get => PlayerPrefs.GetString(NameKey, DefaultName);
        set
        {
            PlayerPrefs.SetString(NameKey, CleanName(value));
            PlayerPrefs.Save();
        }
    }

    public static List<Entry> GetTop(string mode)
    {
        return Load(mode).entries;
    }

    // Adds the score if it makes the board. Returns its place (0 is first), or -1 if it missed.
    public static int Add(string mode, string playerName, int score)
    {
        if (score <= 0) return -1;

        Board board = Load(mode);

        int rank = 0;
        while (rank < board.entries.Count && board.entries[rank].score >= score) rank++;

        if (rank >= MaxEntries) return -1;

        board.entries.Insert(rank, new Entry { name = CleanName(playerName), score = score });

        if (board.entries.Count > MaxEntries) board.entries.RemoveAt(MaxEntries);

        Save(mode, board);
        return rank;
    }

    public static void Rename(string mode, int rank, string playerName)
    {
        Board board = Load(mode);
        if (rank < 0 || rank >= board.entries.Count) return;

        Entry entry = board.entries[rank];
        entry.name = CleanName(playerName);
        board.entries[rank] = entry;

        Save(mode, board);
    }

    // The score of the last place on a full board.
    public static int GetScoreToBeat(string mode)
    {
        List<Entry> entries = Load(mode).entries;
        return entries.Count < MaxEntries ? 0 : entries[entries.Count - 1].score;
    }

    // Letters, digits and spaces only, which also keeps TextMeshPro tags out of the list.
    private static string CleanName(string playerName)
    {
        var cleaned = new StringBuilder();

        foreach (char letter in (playerName ?? "").ToUpperInvariant())
        {
            if (cleaned.Length >= MaxNameLength) break;
            if (char.IsLetterOrDigit(letter) || letter == ' ') cleaned.Append(letter);
        }

        string result = cleaned.ToString().Trim();
        return result.Length > 0 ? result : DefaultName;
    }

    private static Board Load(string mode)
    {
        string json = PlayerPrefs.GetString(BoardKeyPrefix + mode, "");
        return json.Length > 0 ? JsonUtility.FromJson<Board>(json) : new Board();
    }

    private static void Save(string mode, Board board)
    {
        PlayerPrefs.SetString(BoardKeyPrefix + mode, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

// The world board, on Unity Leaderboards. All modes share it, and each score carries
// the mode it was set in. Its ID on the Unity dashboard is "endless".
// A phone can lose its signal at any moment, so nothing here throws: a failed call
// logs a warning and the game carries on with the scores on the device.
public static class OnlineLeaderboard
{
    private const string BoardId = "endless";

    // The extra data saved with a score. Unity sends it back as JSON.
    [Serializable]
    private class ScoreDetails
    {
        public string mode;
    }

    private static Task<bool> connecting;
    private static string sentName;

    // Safe to call often: it signs in once, and tries again only after a failure.
    public static Task<bool> Connect()
    {
        if (connecting == null || (connecting.IsCompleted && !connecting.Result))
            connecting = SignIn();

        return connecting;
    }

    private static async Task<bool> SignIn()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            // Anonymous: the service remembers this device, with no account to create.
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception problem)
        {
            Debug.LogWarning("Online leaderboard: could not sign in. " + problem.Message);
            return false;
        }
    }

    public static async void Submit(string mode, int score)
    {
        if (!await Connect()) return;

        try
        {
            await SendName();
            var options = new AddPlayerScoreOptions { Metadata = new ScoreDetails { mode = mode } };
            await LeaderboardsService.Instance.AddPlayerScoreAsync(BoardId, score, options);
        }
        catch (Exception problem)
        {
            Debug.LogWarning("Online leaderboard: could not send the score. " + problem.Message);
        }
    }

    public static async void UpdateName()
    {
        if (!await Connect()) return;

        try
        {
            await SendName();
        }
        catch (Exception problem)
        {
            Debug.LogWarning("Online leaderboard: could not send the name. " + problem.Message);
        }
    }

    // Null means the board could not be reached, so show the one on the device.
    public static async Task<List<Leaderboard.Entry>> GetTop()
    {
        if (!await Connect()) return null;

        try
        {
            var options = new GetScoresOptions { Limit = Leaderboard.MaxEntries, IncludeMetadata = true };
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(BoardId, options);
            var entries = new List<Leaderboard.Entry>();

            foreach (LeaderboardEntry result in page.Results)
            {
                entries.Add(new Leaderboard.Entry
                {
                    name = DisplayName(result.PlayerName),
                    score = (int)result.Score,
                    mode = ModeOf(result),
                    isPlayer = result.PlayerId == AuthenticationService.Instance.PlayerId
                });
            }

            return entries;
        }
        catch (Exception problem)
        {
            Debug.LogWarning("Online leaderboard: could not read the board. " + problem.Message);
            return null;
        }
    }

    private static async Task SendName()
    {
        // The service refuses spaces in a name.
        string playerName = Leaderboard.PlayerName.Replace(" ", "");

        if (playerName == sentName) return;

        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
        sentName = playerName;
    }

    // The service adds a tag like #1234 to keep names unique, and the font has no # anyway.
    private static string DisplayName(string playerName)
    {
        return (playerName ?? "").Split('#')[0].ToUpperInvariant();
    }

    private static string ModeOf(LeaderboardEntry result)
    {
        if (string.IsNullOrEmpty(result.Metadata)) return "";

        ScoreDetails details = JsonUtility.FromJson<ScoreDetails>(result.Metadata);
        return details != null && details.mode != null ? details.mode.ToUpperInvariant() : "";
    }
}

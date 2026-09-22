using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace XpSystem.Commands;

public class XpLeaderboardSubCommand : ICommand
{
    private static readonly Dictionary<string, string> NameCache = [];
    private static readonly Regex UsernameRegex = new(@"""username""\s*:\s*""([^""]+)""", RegexOptions.Compiled);

    public string Command => "leaderboard";

    public string[] Aliases => ["top", "lb"];

    public string Description => "Top 5 players by XP.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            IReadOnlyDictionary<string, long> allXp = XpManager.GetAllXp();
            LogManager.Debug($"[XpLeaderboard] Records read: {allXp.Count}");

            var top = allXp.Where(kv => !kv.Key.EndsWith("@discord")).Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).Take(5).Select(kv =>
            {
                Player onlinePlayer = Player.Get(kv.Key);
                string name = onlinePlayer?.Nickname ?? ResolveOfflineName(kv.Key);
                return new
                {
                    Name = name,
                    Xp = kv.Value,
                    Rank = XpManager.GetRank(kv.Value)
                };
            }).ToList();

            LogManager.Debug($"[XpLeaderboard] Top 5 players selected: {string.Join(", ", top.Select(p => $"{p.Name} ({p.Xp} XP)"))}");

            if (top.Count == 0)
            {
                response = "There is no data to show.";
                return true;
            }

            StringBuilder sb = new("\n═══ Top 5 Players ═══\n");
            for (int i = 0; i < top.Count; i++)
            {
                var p = top[i];
                string rankName = p.Rank?.BadgeName ?? "No rank";
                sb.AppendLine($"{i + 1}. {p.Name} — {p.Xp} XP ({rankName})");
            }

            sb.Append("═══════════════");

            response = sb.ToString();
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }

    private static string ResolveOfflineName(string userId)
    {
        if (NameCache.TryGetValue(userId, out string cached))
            return cached;

        string name;
        if (userId.EndsWith("@northwood"))
            name = userId.Substring(0, userId.Length - 10);
        else
            name = TryFetchSteamName(userId) ?? userId;

        NameCache[userId] = name;
        return name;
    }

    private static string TryFetchSteamName(string userId)
    {
        if (!userId.EndsWith("@steam"))
            return null;

        string steamId = userId.Substring(0, userId.Length - 6);

        try
        {
            string json = HttpQuery.Get($"https://playerdb.co/api/player/steam/{steamId}");
            Match match = UsernameRegex.Match(json);
            if (match.Success)
                return match.Groups[1].Value;
        }
        catch
        {
            return null;
        }

        return null;
    }
}
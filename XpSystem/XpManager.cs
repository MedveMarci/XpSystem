using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using XpSystem.Storage;

namespace XpSystem;

public static class XpManager
{
    public static bool IsEnabled { get; set; } = true;

    public static long GetXp(Player player)
    {
        try
        {
            if (player == null || player.DoNotTrack || player.IsDummy)
                return 0;

            return XpStorage.Get(player.UserId);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to read XP: {ex.Message}");
            return 0;
        }
    }

    public static long GetXp(string userId)
    {
        try
        {
            return XpStorage.Get(userId);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to read XP: {ex.Message}");
            return 0;
        }
    }

    public static IReadOnlyDictionary<string, long> GetAllXp()
    {
        try
        {
            return XpStorage.GetAll();
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to read the XP data: {ex.Message}");
            return new Dictionary<string, long>();
        }
    }

    public static void AddXp(Player player, int baseAmount, string reason)
    {
        try
        {
            if (!IsEnabled || player == null || player.DoNotTrack || player.IsDummy)
                return;

            Config config = XpSystemPlugin.Singleton?.Config;
            if (config == null)
                return;

            long previousXp = GetXp(player);
            RankConfig previousRank = GetRank(previousXp);

            int finalAmount = Math.Max(1, (int)(baseAmount * config.XpMultiplier));

            long currentXp = XpStorage.Add(player.UserId, finalAmount);
            RankConfig currentRank = GetRank(currentXp);
            RankConfig nextRank = GetNextRank(currentXp);

            string hintText;
            string rankUpLine = string.Empty;
            if (currentRank != null && previousRank != null && !string.Equals(previousRank.BadgeName, currentRank.BadgeName, StringComparison.Ordinal))
                rankUpLine = $"<size=26><color=#FFD700>Rank up! New rank: {currentRank.BadgeName}</color></size>\n";

            if (nextRank != null)
            {
                long remaining = nextRank.MinXp - currentXp;
                hintText = rankUpLine + $"<color=green>+{finalAmount} XP</color> | {reason}\n" + $"<size=24>Next rank: <color={nextRank.BadgeColor}>{nextRank.BadgeName}</color> ({remaining} XP left)</size>";
            }
            else
            {
                hintText = rankUpLine + $"<color=green>+{finalAmount} XP</color> | {reason}\n" + "<size=24><color=#FFD700>Maximum rank reached!</color></size>";
            }

            player.SendHint(hintText, config.HintDuration);
            BadgeManager.UpdatePlayerBadge(player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to add XP: {ex.Message}");
        }
    }

    public static RankConfig GetRank(long xp)
    {
        Config config = XpSystemPlugin.Singleton?.Config;
        if (config?.Ranks == null || config.Ranks.Count == 0)
            return null;

        RankConfig best = null;
        foreach (RankConfig rank in config.Ranks)
            if (xp >= rank.MinXp && (best == null || rank.MinXp > best.MinXp))
                best = rank;

        return best;
    }

    public static RankConfig GetNextRank(long xp)
    {
        Config config = XpSystemPlugin.Singleton?.Config;
        if (config?.Ranks == null || config.Ranks.Count == 0)
            return null;

        return config.Ranks.Where(r => r.MinXp > xp).OrderBy(r => r.MinXp).FirstOrDefault();
    }

    public static void RemoveXp(Player player, int amount)
    {
        try
        {
            if (player == null || player.DoNotTrack || player.IsDummy)
                return;

            long currentXp = GetXp(player);
            XpStorage.Set(player.UserId, Math.Max(0, currentXp - amount));
            BadgeManager.UpdatePlayerBadge(player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to remove XP: {ex.Message}");
        }
    }

    public static void SetXp(Player player, long amount)
    {
        try
        {
            if (player == null || player.DoNotTrack || player.IsDummy)
                return;

            XpStorage.Set(player.UserId, Math.Max(0, amount));
            BadgeManager.UpdatePlayerBadge(player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to set XP: {ex.Message}");
        }
    }
}
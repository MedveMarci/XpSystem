using System;
using System.Collections.Concurrent;
using LabApi.Features.Wrappers;
using LabApiExtensions.FakeExtension;

namespace XpSystem;

public static class BadgeManager
{
    private static readonly ConcurrentDictionary<string, OriginalBadgeInfo> OriginalBadges = new();

    public static void CacheOriginalBadge(Player player)
    {
        try
        {
            if (player?.ReferenceHub == null || string.IsNullOrEmpty(player.UserId))
                return;

            ServerRoles sr = player.ReferenceHub.serverRoles;
            UserGroup group = sr.Group;

            bool hasGlobal = !string.IsNullOrEmpty(sr.NetworkGlobalBadge);

            OriginalBadgeInfo info = new()
            {
                HasGlobalBadge = hasGlobal
            };

            if (hasGlobal)
            {
                info.HasBadge = true;
                info.BadgeText = string.Empty;
                info.BadgeColor = string.Empty;
                info.IsBadgeHidden = false;
            }
            else if (group != null && !string.IsNullOrEmpty(group.BadgeText))
            {
                info.HasBadge = true;
                info.BadgeText = group.BadgeText;
                info.BadgeColor = group.BadgeColor ?? "default";
                info.IsBadgeHidden = string.IsNullOrEmpty(player.GroupName);
            }
            else
            {
                info.HasBadge = false;
                info.BadgeText = string.Empty;
                info.BadgeColor = string.Empty;
                info.IsBadgeHidden = false;
            }

            OriginalBadges[player.UserId] = info;
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Badge cache error: {ex.Message}");
        }
    }

    public static void RemoveCachedBadge(string userId)
    {
        if (!string.IsNullOrEmpty(userId))
            OriginalBadges.TryRemove(userId, out _);
    }

    public static void ClearCache()
    {
        OriginalBadges.Clear();
    }

    public static void UpdatePlayerBadge(Player player)
    {
        try
        {
            if (player?.ReferenceHub == null || !player.IsReady)
                return;

            if (!OriginalBadges.TryGetValue(player.UserId, out OriginalBadgeInfo originalInfo))
            {
                CacheOriginalBadge(player);
                if (!OriginalBadges.TryGetValue(player.UserId, out originalInfo))
                    return;
            }

            if (originalInfo.HasGlobalBadge)
                return;

            Config config = XpSystemPlugin.Singleton?.Config;
            if (config == null)
                return;

            string rankName;
            string rankColor;

            if (player.DoNotTrack)
            {
                rankName = config.DntBadgeText;
                rankColor = config.DntBadgeColor;
            }
            else
            {
                long xp = XpManager.GetXp(player);
                RankConfig rank = XpManager.GetRank(xp);
                if (rank == null)
                    return;

                rankName = rank.BadgeName;
                rankColor = rank.BadgeColor;
            }

            if (originalInfo.IsBadgeHidden)
            {
                ApplyHiddenBadgeForAllViewers(player, rankName, rankColor);
                return;
            }

            string displayColor = rankColor;
            if (originalInfo.HasBadge && !string.IsNullOrEmpty(originalInfo.BadgeColor) && !originalInfo.IsBadgeHidden)
                displayColor = originalInfo.BadgeColor;

            string displayBadge = originalInfo.HasBadge && !string.IsNullOrEmpty(originalInfo.BadgeText) ? $"{rankName} - {originalInfo.BadgeText}" : rankName;

            foreach (Player viewer in Player.ReadyList)
                try
                {
                    if (viewer?.ReferenceHub == null || !viewer.IsReady)
                        continue;

                    player.SetFakeBadgeText(viewer, displayBadge);
                    player.SetFakeBadgeColor(viewer, displayColor);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"[XpSystem] Fake badge error (viewer: {viewer?.UserId}): {ex.Message}");
                }
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Badge update error: {ex.Message}");
        }
    }

    private static void ApplyHiddenBadgeForAllViewers(Player player, string rankName, string rankColor)
    {
        foreach (Player viewer in Player.ReadyList)
            try
            {
                if (viewer?.ReferenceHub == null || !viewer.IsReady)
                    continue;

                if (viewer.RemoteAdminAccess)
                    continue;

                player.SetFakeBadgeText(viewer, rankName);
                player.SetFakeBadgeColor(viewer, rankColor);
            }
            catch (Exception ex)
            {
                LogManager.Error($"[XpSystem] Hidden badge error (viewer: {viewer?.UserId}): {ex.Message}");
            }
    }

    public static void UpdateAllBadges()
    {
        foreach (Player player in Player.ReadyList)
            try
            {
                if (player?.ReferenceHub == null)
                    continue;

                CacheOriginalBadge(player);
                UpdatePlayerBadge(player);
            }
            catch (Exception ex)
            {
                LogManager.Error($"[XpSystem] Badge update error ({player?.UserId}): {ex.Message}");
            }
    }

    public static void UpdateBadgesForViewer(Player viewer)
    {
        if (viewer?.ReferenceHub == null || !viewer.IsReady)
            return;

        foreach (Player player in Player.ReadyList)
            try
            {
                if (player?.ReferenceHub == null || !player.IsReady)
                    continue;

                if (player.UserId == viewer.UserId)
                    continue;

                UpdateSingleBadgeForViewer(player, viewer);
            }
            catch (Exception ex)
            {
                LogManager.Error($"[XpSystem] Badge viewer update error: {ex.Message}");
            }
    }

    private static void UpdateSingleBadgeForViewer(Player player, Player viewer)
    {
        if (!OriginalBadges.TryGetValue(player.UserId, out OriginalBadgeInfo originalInfo))
            return;

        if (originalInfo.HasGlobalBadge)
            return;

        Config config = XpSystemPlugin.Singleton?.Config;
        if (config == null)
            return;

        string rankName;
        string rankColor;

        if (player.DoNotTrack)
        {
            rankName = config.DntBadgeText;
            rankColor = config.DntBadgeColor;
        }
        else
        {
            long xp = XpManager.GetXp(player);
            RankConfig rank = XpManager.GetRank(xp);
            if (rank == null)
                return;

            rankName = rank.BadgeName;
            rankColor = rank.BadgeColor;
        }

        if (originalInfo.IsBadgeHidden && viewer.RemoteAdminAccess)
            return;

        if (originalInfo.IsBadgeHidden)
        {
            player.SetFakeBadgeText(viewer, rankName);
            player.SetFakeBadgeColor(viewer, rankColor);
            return;
        }

        string displayBadge = originalInfo.HasBadge && !string.IsNullOrEmpty(originalInfo.BadgeText) ? $"{rankName} - {originalInfo.BadgeText}" : rankName;

        string displayColor = originalInfo.HasBadge && !string.IsNullOrEmpty(originalInfo.BadgeColor) ? originalInfo.BadgeColor : rankColor;

        player.SetFakeBadgeText(viewer, displayBadge);
        player.SetFakeBadgeColor(viewer, displayColor);
    }

    internal class OriginalBadgeInfo
    {
        public string BadgeText { get; set; } = string.Empty;

        public string BadgeColor { get; set; } = string.Empty;

        public bool IsBadgeHidden { get; set; }

        public bool HasGlobalBadge { get; set; }

        public bool HasBadge { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;
using UserSettings.ServerSpecific;
using XpSystem.ApiManager;

namespace XpSystem;

internal static class XpInfoMenu
{
    private static SSGroupHeader _header;
    private static SSTextArea _textArea;

    internal static void Register()
    {
        try
        {
            Config config = XpSystemPlugin.Singleton?.Config;
            if (config is not { InfoMenuEnabled: true } || _textArea != null)
                return;

            _header = new SSGroupHeader("XpSystem");
            _textArea = new SSTextArea(config.InfoMenuSettingId, "Loading...");

            ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? [];
            ServerSpecificSettingsSync.DefinedSettings = [.. existing, _header, _textArea];
            ServerSpecificSettingsSync.ServerOnStatusReceived += OnStatusReceived;
            ServerSpecificSettingsSync.SendToAll();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to register the info menu: {ex}");
        }
    }

    internal static void Unregister()
    {
        try
        {
            if (_textArea == null)
                return;

            ServerSpecificSettingsSync.ServerOnStatusReceived -= OnStatusReceived;

            ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? [];
            ServerSpecificSettingsSync.DefinedSettings = [.. existing.Where(s => s != _header && s != _textArea)];
            ServerSpecificSettingsSync.SendToAll();
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to unregister the info menu: {ex}");
        }
        finally
        {
            _header = null;
            _textArea = null;
        }
    }

    internal static void UpdateIfOpen(Player player)
    {
        if (player?.ReferenceHub != null && ServerSpecificSettingsSync.IsTabOpenForUser(player.ReferenceHub))
            Update(player);
    }

    internal static void Update(Player player)
    {
        try
        {
            if (_textArea == null || player?.ReferenceHub == null || !player.IsReady || player.IsDummy)
                return;

            ReferenceHub hub = player.ReferenceHub;
            _textArea.SendTextUpdate(BuildText(player), false, receiver => receiver == hub);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to update the info menu: {ex.Message}");
        }
    }

    private static void OnStatusReceived(ReferenceHub hub, SSSUserStatusReport report)
    {
        Update(Player.Get(hub));
    }

    private static string BuildText(Player player)
    {
        if (player.DoNotTrack)
            return "<color=#A0A0A0>You have Do Not Track enabled, so no XP is stored for you.</color>";

        Config config = XpSystemPlugin.Singleton?.Config;
        List<RankConfig> ranks = config?.Ranks ?? [];

        long xp = XpManager.GetXp(player);
        RankConfig rank = XpManager.GetRank(xp);
        RankConfig nextRank = XpManager.GetNextRank(xp);

        string rankName = rank?.BadgeName ?? "Unranked";
        string rankColor = EventHandlers.GetHexColor(rank?.BadgeColor);
        int level = ranks.Count(r => r.MinXp <= xp);

        StringBuilder sb = new();
        sb.AppendLine($"<size=30><b>- <color={rankColor}>{rankName}</color></b></size>");
        sb.AppendLine($"Level {level} / {ranks.Count}");

        if (nextRank != null)
        {
            long rankStart = rank?.MinXp ?? 0;
            double progress = (double)(xp - rankStart) / Math.Max(1, nextRank.MinXp - rankStart);

            sb.AppendLine($"XP {Format(xp)} / {Format(nextRank.MinXp)}");
            sb.AppendLine(BuildBar(progress, rankColor));
            sb.AppendLine($"{Format(nextRank.MinXp - xp)} XP left until <color={EventHandlers.GetHexColor(nextRank.BadgeColor)}>{nextRank.BadgeName}</color>");
        }
        else
        {
            sb.AppendLine($"XP {Format(xp)}");
            sb.AppendLine(BuildBar(1, "#FFD700"));
            sb.AppendLine("<color=#FFD700>Maximum rank reached!</color>");
        }

        sb.AppendLine($"This round: <color=green>+{Format(XpManager.GetRoundXp(player))} XP</color>");

        IReadOnlyDictionary<string, long> allXp = XpManager.GetAllXp();
        int position = allXp.Values.Count(v => v > xp) + 1;
        int total = allXp.Count + (allXp.ContainsKey(player.UserId) ? 0 : 1);
        sb.Append($"Leaderboard: #{position} of {total}");

        if (!XpManager.IsEnabled)
            sb.Append("\n<color=#C50000>XP gaining is currently turned off.</color>");

        return sb.ToString();
    }

    private static string BuildBar(double progress, string color)
    {
        progress = Math.Max(0, Math.Min(1, progress));
        int filled = (int)Math.Round(progress * 40);

        return $"[<color={color}>{new string('|', filled)}</color><color=#555555>{new string('|', 40 - filled)}</color>] {progress * 100:0}%";
    }

    private static string Format(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
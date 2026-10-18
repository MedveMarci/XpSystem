using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles.PlayableScps.Scp3114;
using XpSystem.ApiManager;

namespace XpSystem;

public static class BadgeManager
{
    private static readonly char[] ForbiddenChars = ['[', ']', '<', '>'];

    private static readonly Dictionary<ReferenceHub, BadgeState> States = new();

    public static void Refresh(Player player, bool force = false)
    {
        try
        {
            if (!IsRealPlayer(player))
                return;

            BadgeState state = GetState(player.ReferenceHub);
            UpdateRankInfo(player, state);

            ServerRoles roles = player.ReferenceHub.serverRoles;
            TryGetBadge(player, roles.Network_myText, roles.Network_myColor, out string text, out string color);

            if (force || text != state.SentText || color != state.SentColor)
                roles.SetSyncVarDirtyBit(1ul | 2ul);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Badge refresh error: {ex.Message}");
        }
    }

    public static void RefreshAll()
    {
        foreach (Player player in Player.ReadyList)
            Refresh(player, true);
    }

    public static void Forget(Player player)
    {
        if (player?.ReferenceHub != null)
            States.Remove(player.ReferenceHub);
    }

    public static void Clear()
    {
        States.Clear();
    }

    public static void Restore()
    {
        foreach (Player player in Player.ReadyList)
            try
            {
                if (!IsRealPlayer(player) || !States.TryGetValue(player.ReferenceHub, out BadgeState state))
                    continue;

                if (state.RankInfo != null && player.CustomInfo == state.RankInfo)
                    player.CustomInfo = string.Empty;

                player.ReferenceHub.serverRoles.SetSyncVarDirtyBit(1ul | 2ul);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Badge restore error ({player?.UserId}): {ex.Message}");
            }

        Clear();
    }

    internal static bool TryGetSerializedBadge(ReferenceHub hub, string realText, string realColor, out string text, out string color)
    {
        text = color = null;

        if (!Player.TryGet(hub.gameObject, out Player player) || !IsRealPlayer(player))
            return false;

        bool replaced = TryGetBadge(player, realText, realColor, out text, out color);

        BadgeState state = GetState(hub);
        state.SentText = text;
        state.SentColor = color;

        return replaced;
    }

    private static bool TryGetBadge(Player player, string realText, string realColor, out string text, out string color)
    {
        text = color = null;

        ServerRoles roles = player.ReferenceHub.serverRoles;
        if (!string.IsNullOrEmpty(roles.GlobalBadge) || !TryGetRank(player, out string rankName, out string rankColor))
            return false;

        if (string.IsNullOrEmpty(realText))
        {
            text = Sanitize(rankName);
            color = rankColor;
        }
        else
        {
            text = Sanitize($"{rankName} - {realText}");
            color = IsUsableColor(roles, realColor) ? realColor : rankColor;
        }

        if (!IsUsableColor(roles, color))
            color = "default";

        if (!string.IsNullOrEmpty(text))
            return true;

        text = color = null;
        return false;
    }

    private static void UpdateRankInfo(Player player, BadgeState state)
    {
        string current = player.CustomInfo;
        if (!string.IsNullOrEmpty(current) && current != state.RankInfo)
        {
            state.RankInfo = null;
            return;
        }

        string info = GetWantedRankInfo(player);

        if (current != (info ?? string.Empty))
            player.CustomInfo = info ?? string.Empty;

        state.RankInfo = info;
    }

    private static string GetWantedRankInfo(Player player)
    {
        if (player.RoleBase is Scp3114Role scp3114)
            return TryGetDisguise(scp3114, out Player disguise) ? GetRankInfo(disguise ?? player) : null;

        bool globalBadge = XpSystemPlugin.Singleton?.Config is { GlobalBadgeRankInfo: true } && !string.IsNullOrEmpty(player.ReferenceHub.serverRoles.GlobalBadge);
        return globalBadge ? GetRankInfo(player) : null;
    }

    private static bool TryGetDisguise(Scp3114Role role, out Player disguise)
    {
        disguise = null;

        if (!role.SubroutineModule.TryGetSubroutine(out Scp3114Identity identity))
            return false;

        Scp3114Identity.StolenIdentity stolen = identity.CurIdentity;
        if (stolen.Status != Scp3114Identity.DisguiseStatus.Active || stolen.Ragdoll == null)
            return false;

        ReferenceHub owner = stolen.Ragdoll.Info.OwnerHub;
        if (owner != null)
            disguise = Player.Get(owner);

        return true;
    }

    private static string GetRankInfo(Player player)
    {
        if (!TryGetRank(player, out string name, out string color))
            return null;

        name = Sanitize(name);
        if (string.IsNullOrEmpty(name))
            return null;

        string info = $"<color={EventHandlers.GetHexColor(color)}>{name}</color>";
        if (NicknameSync.ValidateCustomInfo(info, out _))
            return info;

        return NicknameSync.ValidateCustomInfo(name, out _) ? name : null;
    }

    private static bool TryGetRank(Player player, out string name, out string color)
    {
        name = color = null;

        Config config = XpSystemPlugin.Singleton?.Config;
        if (config == null)
            return false;

        if (player.DoNotTrack)
        {
            name = config.DntBadgeText;
            color = config.DntBadgeColor;
            return true;
        }

        RankConfig rank = XpManager.GetRank(XpManager.GetXp(player));
        if (rank == null)
            return false;

        name = rank.BadgeName;
        color = rank.BadgeColor;
        return true;
    }

    private static bool IsUsableColor(ServerRoles roles, string color)
    {
        if (string.IsNullOrEmpty(color))
            return false;

        foreach (ServerRoles.NamedColor namedColor in roles.NamedColors)
            if (namedColor.Name == color)
                return !namedColor.Restricted;

        return false;
    }

    private static string Sanitize(string text)
    {
        if (text == null)
            return null;

        if (text.IndexOfAny(ForbiddenChars) >= 0)
            text = new string([.. text.Where(c => Array.IndexOf(ForbiddenChars, c) < 0)]);

        if (text.IndexOf("\\u003", StringComparison.Ordinal) >= 0)
            text = text.Replace("\\u003c", string.Empty).Replace("\\u003e", string.Empty);

        return text.Trim();
    }

    private static BadgeState GetState(ReferenceHub hub)
    {
        if (!States.TryGetValue(hub, out BadgeState state))
            States[hub] = state = new BadgeState();

        return state;
    }

    private static bool IsRealPlayer(Player player)
    {
        return player?.ReferenceHub != null && player.IsReady && !player.IsDummy && !player.IsHost;
    }

    private sealed class BadgeState
    {
        public string SentText;
        public string SentColor;
        public string RankInfo;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Arguments.Scp3114Events;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using XpSystem.Extensions;
using XpSystem.Storage;

namespace XpSystem;

internal class EventHandlers : CustomEventsHandler
{
    public static readonly IReadOnlyDictionary<string, string> colorMap = new Dictionary<string, string>
    {
        { "pink", "#FF96DE" },
        { "red", "#C50000" },
        { "brown", "#944710" },
        { "silver", "#A0A0A0" },
        { "light_green", "#32CD32" },
        { "crimson", "#DC143C" },
        { "cyan", "#00B7EB" },
        { "aqua", "#00FFFF" },
        { "deep_pink", "#FF1493" },
        { "tomato", "#FF6448" },
        { "yellow", "#FAFF86" },
        { "magenta", "#FF0090" },
        { "blue_green", "#4DFFB8" },
        { "orange", "#FF9966" },
        { "lime", "#BFFF00" },
        { "green", "#228B22" },
        { "emerald", "#50C878" },
        { "carmine", "#960018" },
        { "nickel", "#727472" },
        { "mint", "#98FB98" },
        { "army_green", "#4B5320" },
        { "pumpkin", "#EE7600" }
    };

    private static readonly Dictionary<string, Dictionary<string, DateTime>> _lastGrantAt = [];

    private static bool TryGrant(Player player, string eventKey)
    {
        if (player == null || player.DoNotTrack || player.IsDummy)
            return false;

        Config config = XpSystemPlugin.Singleton?.Config;
        if (config?.XpEvents == null)
            return false;

        if (!config.XpEvents.TryGetValue(eventKey, out XpEventConfig ev))
            return false;

        if (!ev.Enabled || ev.Xp <= 0)
            return false;

        float cooldownSeconds = 0f;
        if (config.EventCooldowns != null && config.EventCooldowns.TryGetValue(eventKey, out float configuredCooldown))
            cooldownSeconds = configuredCooldown;

        if (IsOnCooldown(player, eventKey, cooldownSeconds))
            return false;

        XpManager.AddXp(player, ev.Xp, ev.Reason);
        MarkGrant(player, eventKey);
        return true;
    }

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        try
        {
            if (ev.Player != null)
                DelayedBadgeSetup(ev.Player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerJoined error: {ex.Message}");
        }

        base.OnPlayerJoined(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        try
        {
            if (ev.Player != null && !string.IsNullOrEmpty(ev.Player.UserId))
            {
                BadgeManager.RemoveCachedBadge(ev.Player.UserId);
                ClearPlayerCooldowns(ev.Player.UserId);
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerLeft error: {ex.Message}");
        }

        base.OnPlayerLeft(ev);
    }

    public override void OnPlayerGroupChanged(PlayerGroupChangedEventArgs ev)
    {
        try
        {
            if (ev.Player == null) return;
            BadgeManager.CacheOriginalBadge(ev.Player);
            BadgeManager.UpdatePlayerBadge(ev.Player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerGroupChanged error: {ex.Message}");
        }

        base.OnPlayerGroupChanged(ev);
    }

    public override void OnPlayerChangedBadgeVisibility(PlayerChangedBadgeVisibilityEventArgs ev)
    {
        try
        {
            if (ev.Player == null) return;
            RefreshBadgeAfterVisibilityChange(ev.Player);
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerChangedBadgeVisibility error: {ex.Message}");
        }

        base.OnPlayerChangedBadgeVisibility(ev);
    }

    public override void OnServerRoundStarted()
    {
        try
        {
            BadgeManager.UpdateAllBadges();
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnServerRoundStarted error: {ex.Message}");
        }

        base.OnServerRoundStarted();
    }

    public override void OnServerWaitingForPlayers()
    {
        try
        {
            BadgeManager.ClearCache();
            ClearAllCooldowns();
            BadgeManager.UpdateAllBadges();
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnServerWaitingForPlayers error: {ex.Message}");
        }

        base.OnServerWaitingForPlayers();
    }

    public override void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        try
        {
            if (!Round.IsRoundStarted) return;

            bool isPocketDimensionDeath = ev.DamageHandler != null && ev.DamageHandler.DeathScreenText == DeathTranslations.PocketDecay.DeathscreenTranslation;

            if (isPocketDimensionDeath)
            {
                foreach (Player scp106 in Player.List.Where(p => p.Role == RoleTypeId.Scp106))
                    TryGrant(scp106, "Scp106Kill");

                base.OnPlayerDeath(ev);
                return;
            }

            Player attacker = ev.Attacker;
            if (attacker == null || attacker.DoNotTrack || attacker.IsDummy) return;
            if (ev.Player != null && ev.Player.IsDummy) return;
            if (ev.Player != null && attacker.UserId == ev.Player.UserId) return;

            bool victimWasScp = ev.OldRole.IsScp();
            bool victimWasZombie = ev.OldRole == RoleTypeId.Scp0492;
            bool attackerIsScp = attacker.Role.IsScp();

            if (attackerIsScp && !victimWasScp)
                TryGrant(attacker, "ScpKillHuman");
            else if (victimWasZombie)
                TryGrant(attacker, "ZombieKill");
            else
                TryGrant(attacker, victimWasScp ? "ScpKill" : "Kill");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerDeath error: {ex.Message}");
        }

        base.OnPlayerDeath(ev);
    }

    public override void OnScp3114Disguised(Scp3114DisguisedEventArgs ev)
    {
        try
        {
            ReferenceHub ownerHub = ev.Ragdoll.Base.NetworkInfo.OwnerHub;
            Player disguisedPlayer = ownerHub == null ? null : Player.Get(ownerHub);
            Player target = disguisedPlayer ?? ev.Player;
            ev.Player.CustomInfo = GetXpCustomInfo(target) ?? string.Empty;
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnScp3114Disguised error: {ex.Message}");
            ev.Player.CustomInfo = string.Empty;
        }

        base.OnScp3114Disguised(ev);
    }

    public override void OnScp3114Revealed(Scp3114RevealedEventArgs ev)
    {
        ev.Player.CustomInfo = "";
        base.OnScp3114Revealed(ev);
    }

    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        if (ev.OldRole == RoleTypeId.Scp3114)
            ev.Player.CustomInfo = "";
        base.OnPlayerChangedRole(ev);
    }

    private static string GetXpCustomInfo(Player player)
    {
        if (player == null || player.DoNotTrack || player.IsDummy)
            return null;

        RankConfig rank = XpManager.GetRank(XpManager.GetXp(player));
        if (rank == null || string.IsNullOrWhiteSpace(rank.BadgeName))
            return null;

        string hexColor = colorMap.TryGetValue(rank.BadgeColor, out string hex) ? hex : "#FFFFFF";

        return string.IsNullOrWhiteSpace(rank.BadgeColor) ? rank.BadgeName : $"<color={hexColor}>{rank.BadgeName}</color>";
    }

    public override void OnPlayerActivatedGenerator(PlayerActivatedGeneratorEventArgs ev)
    {
        try
        {
            TryGrant(ev.Player, "Generator");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerActivatedGenerator error: {ex.Message}");
        }

        base.OnPlayerActivatedGenerator(ev);
    }

    public override void OnPlayerEscaped(PlayerEscapedEventArgs ev)
    {
        try
        {
            TryGrant(ev.Player, "Escape");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerEscaped error: {ex.Message}");
        }

        base.OnPlayerEscaped(ev);
    }

    public override void OnPlayerUsedItem(PlayerUsedItemEventArgs ev)
    {
        try
        {
            if (ev.UsableItem.Type is ItemType.SCP500 or ItemType.Medkit or ItemType.Painkillers or ItemType.Adrenaline)
                TryGrant(ev.Player, "UsedMedicalItem");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerUsedItem error: {ex.Message}");
        }

        base.OnPlayerUsedItem(ev);
    }

    public override void OnServerRoundEnded(RoundEndedEventArgs ev)
    {
        try
        {
            List<Player> wonPlayers = [.. Player.List.Where(p => p.Team.GetLeadingTeam() == ev.LeadingTeam)];
            foreach (Player player in wonPlayers)
                TryGrant(player, "RoundWin");

            XpStorage.SaveInBackground();
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnServerRoundEnded error: {ex.Message}");
        }

        base.OnServerRoundEnded(ev);
    }

    public override void OnScp914ProcessedInventoryItem(Scp914ProcessedInventoryItemEventArgs ev)
    {
        try
        {
            TryGrant(ev.Player, "Scp914Upgrade");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnScp914ProcessedInventoryItem error: {ex.Message}");
        }

        base.OnScp914ProcessedInventoryItem(ev);
    }

    public override void OnPlayerPickedUpItem(PlayerPickedUpItemEventArgs ev)
    {
        try
        {
            if (ev.Item.Type is ItemType.MicroHID or ItemType.GunSCP127 or ItemType.Jailbird or ItemType.ParticleDisruptor)
                TryGrant(ev.Player, "PickedUpSpecialItem");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnPlayerPickedUpItem error: {ex.Message}");
        }

        base.OnPlayerPickedUpItem(ev);
    }

    public override void OnScp049ResurrectedBody(Scp049ResurrectedBodyEventArgs ev)
    {
        try
        {
            TryGrant(ev.Player, "Scp049Resurrection");
        }
        catch (Exception ex)
        {
            LogManager.Error($"OnScp049ResurrectedBody error: {ex.Message}");
        }

        base.OnScp049ResurrectedBody(ev);
    }

    private static void DelayedBadgeSetup(Player player)
    {
        Timing.CallDelayed(0.1f, () =>
        {
            try
            {
                if (player?.ReferenceHub == null || !player.IsReady)
                    return;

                BadgeManager.CacheOriginalBadge(player);
                BadgeManager.UpdatePlayerBadge(player);
                BadgeManager.UpdateBadgesForViewer(player);
            }
            catch (Exception ex)
            {
                LogManager.Error($"DelayedBadgeSetup error: {ex.Message}");
            }
        });
    }

    private static void RefreshBadgeAfterVisibilityChange(Player player)
    {
        Timing.CallDelayed(0.1f, () =>
        {
            try
            {
                if (player?.ReferenceHub == null || !player.IsReady)
                    return;

                BadgeManager.CacheOriginalBadge(player);
                BadgeManager.UpdatePlayerBadge(player);
            }
            catch (Exception ex)
            {
                LogManager.Error($"RefreshBadgeAfterVisibilityChange error: {ex.Message}");
            }
        });
    }

    private static bool IsOnCooldown(Player player, string eventKey, float cooldownSeconds)
    {
        if (cooldownSeconds <= 0f)
            return false;

        if (string.IsNullOrWhiteSpace(player.UserId))
            return false;

        if (!_lastGrantAt.TryGetValue(player.UserId, out Dictionary<string, DateTime> perEvent))
            return false;

        if (!perEvent.TryGetValue(eventKey, out DateTime lastGrant))
            return false;

        return (DateTime.UtcNow - lastGrant).TotalSeconds < cooldownSeconds;
    }

    private static void MarkGrant(Player player, string eventKey)
    {
        if (string.IsNullOrWhiteSpace(player.UserId))
            return;

        if (!_lastGrantAt.TryGetValue(player.UserId, out Dictionary<string, DateTime> perEvent))
        {
            perEvent = [];
            _lastGrantAt[player.UserId] = perEvent;
        }

        perEvent[eventKey] = DateTime.UtcNow;
    }

    private static void ClearPlayerCooldowns(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _lastGrantAt.Remove(userId);
    }

    private static void ClearAllCooldowns()
    {
        _lastGrantAt.Clear();
    }
}
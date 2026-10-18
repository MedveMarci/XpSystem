using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace XpSystem.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class XpCommand : ParentCommand
{
    public override string Command => "xp";

    public override string Description =>
        "XP system. Subcommands: refresh, leaderboard, toggle, add, remove, get, setmultiplier";

    public override string[] Aliases => [];

    public XpCommand()
    {
        LoadGeneratedCommands();
    }

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new XpRefreshSubCommand());
        RegisterCommand(new XpLeaderboardSubCommand());
        RegisterCommand(new XpToggleSubCommand());
        RegisterCommand(new XpAddSubCommand());
        RegisterCommand(new XpRemoveSubCommand());
        RegisterCommand(new XpGetSubCommand());
        RegisterCommand(new XpSetMultiplierSubCommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "Only players can use this command.";
                return false;
            }

            if (player.DoNotTrack)
            {
                response = "Do Not Track is enabled - no data is stored.";
                return true;
            }

            long xp = XpManager.GetXp(player);
            RankConfig currentRank = XpManager.GetRank(xp);
            RankConfig nextRank = XpManager.GetNextRank(xp);

            if (currentRank == null)
            {
                response = "There is no rank configuration available.";
                return false;
            }

            string info = "\n═══ XP System ═══\n" + $"XP: {xp}\n" + $"Rank: {currentRank.BadgeName}\n";

            info += nextRank != null ? $"Next rank: {nextRank.BadgeName} ({nextRank.MinXp - xp} XP left)\n" : "Maximum rank reached!\n";

            info += "═══════════════";

            response = info;
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }
}
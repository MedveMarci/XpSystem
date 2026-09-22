using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace XpSystem.Commands;

public class XpRefreshSubCommand : ICommand
{
    public string Command => "refresh";

    public string[] Aliases => [];

    public string Description => "Refreshes every badge (requires the xp.refresh permission).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "Only players can use this command.";
                return false;
            }

            if (!sender.HasPermission("xp.refresh"))
            {
                response = "You do not have permission. Required: xp.refresh";
                return false;
            }

            BadgeManager.UpdateAllBadges();
            response = "Every badge has been refreshed.";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }
}
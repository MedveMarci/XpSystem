using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace XpSystem.Commands;

public class XpToggleSubCommand : ICommand
{
    public string Command => "toggle";

    public string[] Aliases => [];

    public string Description => "Turns XP gaining on and off globally (requires the xp.toggle permission).";

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

            if (!sender.HasPermission("xp.toggle"))
            {
                response = "You do not have permission. Required: xp.toggle";
                return false;
            }

            XpManager.IsEnabled = !XpManager.IsEnabled;
            response = XpManager.IsEnabled ? "XP gaining enabled." : "XP gaining disabled.";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }
}
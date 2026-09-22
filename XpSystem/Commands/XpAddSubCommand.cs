using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Utils;

namespace XpSystem.Commands;

public class XpAddSubCommand : ICommand
{
    public string Command => "add";

    public string[] Aliases => [];

    public string Description =>
        "Gives XP to the target (requires the xp.admin permission). Usage: .xp add <target> <amount>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            if (!sender.HasPermission("xp.admin"))
            {
                response = "You do not have permission. Required: xp.admin";
                return false;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: .xp add <target> <amount>";
                return false;
            }

            List<ReferenceHub> targets = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] remaining);
            if (targets.Count == 0)
            {
                response = "Target not found.";
                return false;
            }

            if (remaining.Length < 1 || !int.TryParse(remaining[0], out int amount) || amount <= 0)
            {
                response = "Invalid amount. Provide a positive whole number.";
                return false;
            }

            StringBuilder sb = new();
            foreach (Player targetPlayer in targets.Select(Player.Get).Where(targetPlayer => targetPlayer != null))
            {
                if (targetPlayer.DoNotTrack)
                {
                    sb.AppendLine($"{targetPlayer.Nickname}: Do Not Track is enabled, XP cannot be modified.");
                    continue;
                }

                XpManager.AddXp(targetPlayer, amount, "Admin command");
                sb.AppendLine($"{targetPlayer.Nickname}: +{amount} XP");
            }

            response = sb.ToString().TrimEnd();
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }
}
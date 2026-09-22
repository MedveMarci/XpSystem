using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Utils;

namespace XpSystem.Commands;

public class XpGetSubCommand : ICommand
{
    public string Command => "get";

    public string[] Aliases => [];

    public string Description =>
        "Shows the target's XP (requires the xp.admin permission). Usage: .xp get <target>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        try
        {
            if (!sender.HasPermission("xp.admin"))
            {
                response = "You do not have permission. Required: xp.admin";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: .xp get <target>";
                return false;
            }

            List<ReferenceHub> targets = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out _);
            if (targets.Count == 0)
            {
                response = "Target not found.";
                return false;
            }

            StringBuilder sb = new();
            foreach (Player targetPlayer in targets.Select(Player.Get).Where(targetPlayer => targetPlayer != null))
            {
                if (targetPlayer.DoNotTrack)
                {
                    sb.AppendLine($"{targetPlayer.Nickname}: Do Not Track is enabled");
                    continue;
                }

                long xp = XpManager.GetXp(targetPlayer);
                RankConfig rank = XpManager.GetRank(xp);
                sb.AppendLine($"{targetPlayer.Nickname}: {xp} XP ({rank?.BadgeName ?? "No rank"})");
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
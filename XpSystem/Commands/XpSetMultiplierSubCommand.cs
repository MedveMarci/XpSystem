using System;
using System.Globalization;
using CommandSystem;
using LabApi.Features.Permissions;

namespace XpSystem.Commands;

public class XpSetMultiplierSubCommand : ICommand
{
    public string Command => "setmultiplier";

    public string[] Aliases => ["setmult", "multiplier"];

    public string Description =>
        "Sets the XP multiplier and saves it (requires the xp.admin permission). Usage: .xp setmultiplier <multiplier>";

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
                response = "Usage: .xp setmultiplier <multiplier>";
                return false;
            }

            if (!float.TryParse(arguments.At(0), NumberStyles.Float, CultureInfo.InvariantCulture, out float multiplier) || multiplier < 0)
            {
                response = "Invalid multiplier. Provide a non-negative number (e.g. 1.5).";
                return false;
            }

            Config config = XpSystemPlugin.Singleton?.Config;
            if (config == null)
            {
                response = "The configuration is not available.";
                return false;
            }

            float old = config.XpMultiplier;
            config.XpMultiplier = multiplier;
            XpSystemPlugin.Singleton.SaveConfig();

            response = $"XP multiplier set: {old} → {multiplier}";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Error: {ex.Message}";
            return false;
        }
    }
}
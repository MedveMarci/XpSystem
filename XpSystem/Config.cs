using System.Collections.Generic;
using System.ComponentModel;

namespace XpSystem;

public class Config
{
    [Description("Enables debug logging in the server console.")]
    public bool Debug { get; set; } = false;

    [Description("Folder where the XP data file is stored.")]
    public string DataFolder { get; set; } = "XpSystem";

    [Description("Name of the XP data file inside the data folder.")]
    public string DataFileName { get; set; } = "xp_data.json";

    [Description("How often (in seconds) the XP data is automatically saved to disk. Minimum: 10.")]
    public int AutoSaveIntervalSeconds { get; set; } = 60;

    [Description("Global XP multiplier applied to every XP gain. Default: 1.0")]
    public float XpMultiplier { get; set; } = 1.0f;

    [Description("Badge text shown for players who have Do Not Track enabled.")]
    public string DntBadgeText { get; set; } = "No Data";

    [Description("Badge color for players who have Do Not Track enabled. Available colors: default, pink, red, brown, silver, light_green, crimson, cyan, aqua, deep_pink, tomato, yellow, magenta, blue_green, orange, lime, green, emerald, carmine, nickel, mint, army_green, pumpkin")]
    public string DntBadgeColor { get; set; } = "nickel";

    [Description("How long (in seconds) the XP hint is displayed.")]
    public float HintDuration { get; set; } = 5f;

    [Description("XP event settings. Key = event id, Value = Enabled/Xp/Reason. To add a new event, add its key here and override the matching method in EventHandlers.")]
    public Dictionary<string, XpEventConfig> XpEvents { get; set; } = new()
    {
        ["Kill"] = new XpEventConfig(true, 40, "Kill"),
        ["ScpKill"] = new XpEventConfig(true, 80, "SCP Kill"),
        ["ScpKillHuman"] = new XpEventConfig(true, 40, "Human killed as SCP"),
        ["Scp106Kill"] = new XpEventConfig(true, 80, "Pocket Dimension kill"),
        ["ZombieKill"] = new XpEventConfig(true, 40, "Zombie killed"),
        ["Scp049Resurrection"] = new XpEventConfig(true, 50, "Body resurrected"),
        ["Generator"] = new XpEventConfig(true, 50, "Generator activated"),
        ["Escape"] = new XpEventConfig(true, 100, "Escaped"),
        ["UsedMedicalItem"] = new XpEventConfig(true, 20, "Medical item used"),
        ["PickedUpSpecialItem"] = new XpEventConfig(true, 25, "Special item picked up"),
        ["RoundWin"] = new XpEventConfig(true, 100, "Round won"),
        ["Scp914Upgrade"] = new XpEventConfig(true, 15, "Upgraded item in SCP-914")
    };

    [Description("Per-event cooldown in seconds. 0 = no cooldown. The keys must match the XpEvents keys.")]
    public Dictionary<string, float> EventCooldowns { get; set; } = new()
    {
        ["Kill"] = 20f,
        ["ScpKill"] = 0f,
        ["ScpKillHuman"] = 20f,
        ["Scp106Kill"] = 20f,
        ["ZombieKill"] = 20f,
        ["Scp049Resurrection"] = 0f,
        ["Generator"] = 60f,
        ["Escape"] = 0f,
        ["UsedMedicalItem"] = 0f,
        ["PickedUpSpecialItem"] = 60f,
        ["RoundWin"] = 0f,
        ["Scp914Upgrade"] = 60f
    };

    [Description("Rank list. Every rank has a MinXp (minimum XP), a BadgeName (displayed name) and a BadgeColor (badge color) field. Add the ranks in ascending MinXp order.")]
    public List<RankConfig> Ranks { get; set; } =
    [
        new() { MinXp = 0, BadgeName = "Rookie", BadgeColor = "default" },
        new() { MinXp = 100, BadgeName = "Player", BadgeColor = "lime" },
        new() { MinXp = 500, BadgeName = "Experienced", BadgeColor = "cyan" },
        new() { MinXp = 1000, BadgeName = "Veteran", BadgeColor = "aqua" },
        new() { MinXp = 2500, BadgeName = "Elite", BadgeColor = "yellow" },
        new() { MinXp = 5000, BadgeName = "Legend", BadgeColor = "orange" },
        new() { MinXp = 10000, BadgeName = "Master", BadgeColor = "crimson" }
    ];
}
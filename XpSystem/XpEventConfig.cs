using System.ComponentModel;

namespace XpSystem;

public class XpEventConfig
{
    [Description("Whether this XP event is enabled.")]
    public bool Enabled { get; set; } = true;

    [Description("Base XP granted by this event. The global multiplier applies to it as well.")]
    public int Xp { get; set; }

    [Description("Reason text shown in the XP hint.")]
    public string Reason { get; set; } = string.Empty;

    public XpEventConfig()
    { }

    public XpEventConfig(bool enabled, int xp, string reason)
    {
        Enabled = enabled;
        Xp = xp;
        Reason = reason;
    }
}
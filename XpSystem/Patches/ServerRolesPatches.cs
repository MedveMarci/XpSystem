using HarmonyLib;

namespace XpSystem.Patches;

[HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.SerializeSyncVars))]
internal static class SerializeBadgePatch
{
    private static void Prefix(ReferenceHub ____hub, ref string ____myText, ref string ____myColor, out (string Text, string Color)? __state)
    {
        __state = null;

        if (!BadgeManager.TryGetSerializedBadge(____hub, ____myText, ____myColor, out string text, out string color))
            return;

        __state = (____myText, ____myColor);
        ____myText = text;
        ____myColor = color;
    }

    private static void Finalizer(ref string ____myText, ref string ____myColor, (string Text, string Color)? __state)
    {
        if (__state is not { } real)
            return;

        ____myText = real.Text;
        ____myColor = real.Color;
    }
}

[HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.RefreshLocalTag))]
internal static class RefreshLocalTagPatch
{
    private static void Postfix(ServerRoles __instance)
    {
        if (!string.IsNullOrEmpty(__instance.GlobalBadge) || !string.IsNullOrEmpty(__instance.Network_myText))
            return;

        if (string.IsNullOrEmpty(__instance.MyText) && __instance.CurrentColor is not { Restricted: true })
            return;

        __instance.SetText(null);
        __instance.SetColor(null);
    }
}
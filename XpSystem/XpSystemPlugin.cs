using System;
using System.Collections.Generic;
using System.IO;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using MEC;
using XpSystem.Storage;
using Version = System.Version;

namespace XpSystem;

internal class XpSystemPlugin : Plugin<Config>
{
    internal static XpSystemPlugin Singleton;
    private CoroutineHandle _autoSaveHandle;
    private EventHandlers _eventHandlers;

    public override string Name => "XpSystem";

    public override string Description => "XP system with ranks and badge display.";

    public override string Author => "MedveMarci";

    public override Version Version { get; } = new(1, 0, 0);

    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        try
        {
            Singleton = this;

            string dataDirectory = Path.Combine(PathManager.Configs.FullName, Config.DataFolder);
            XpStorage.Initialize(dataDirectory, Config.DataFileName);

            _eventHandlers = new EventHandlers();
            CustomHandlersManager.RegisterEventsHandler(_eventHandlers);

            int interval = Math.Max(10, Config.AutoSaveIntervalSeconds);
            _autoSaveHandle = Timing.RunCoroutine(AutoSaveCoroutine(interval));
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to enable: {ex}");
        }
    }

    public override void Disable()
    {
        try
        {
            Timing.KillCoroutines(_autoSaveHandle);

            if (_eventHandlers != null)
                CustomHandlersManager.UnregisterEventsHandler(_eventHandlers);

            BadgeManager.ClearCache();

            XpStorage.Save();
            XpStorage.Unload();

            _eventHandlers = null;

            Singleton = null;
        }
        catch (Exception ex)
        {
            LogManager.Error($"[XpSystem] Failed to disable: {ex}");
        }
    }

    private static IEnumerator<float> AutoSaveCoroutine(int intervalSeconds)
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(intervalSeconds);
            XpStorage.SaveInBackground();
        }
    }
}
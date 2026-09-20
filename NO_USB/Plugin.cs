using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NO_USB;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.nikkorap.blueprinter")]
public class Plugin : BaseUnityPlugin
{
    private const bool DebugLogs = true;
    private new static ManualLogSource Logger { get; set; } = null!;
    private Harmony? Harmony { get; set; }
    
    private void Awake()
    {
        Logger = base.Logger;
        
        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Repatch();
        
        Debug($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
    }
    
    private void Repatch()
    {
        Harmony?.UnpatchSelf();
        Harmony?.PatchAll();
    }
    
    internal static void Debug(string message, DebugType type = DebugType.LogInfo)
    {
#pragma warning disable CS0162 // Unreachable code detected
        if (!DebugLogs) return;
#pragma warning restore CS0162 // Unreachable code detected
        switch (type)
        {
            case DebugType.LogDebug:
                Logger.LogDebug(message);
                break;
            case DebugType.LogError:
                Logger.LogError(message);
                break;
            case DebugType.LogWarning:
                Logger.LogWarning(message);
                break;
            case DebugType.LogInfo:
            default:
                Logger.LogInfo(message);
                break;
        }
    }
    
    internal enum DebugType
    {
        LogDebug,
        LogError,
        LogInfo,
        LogWarning
    }
}
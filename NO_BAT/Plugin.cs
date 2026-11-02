using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace NO_BAT;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.nikkorap.blueprinter")]
public class Plugin : BaseUnityPlugin
{
    private new static ManualLogSource Logger { get; set; } = null!;
    private Harmony? Harmony { get; set; }
    
    private static ConfigEntry<bool> PluginEnabled { get; set; } = null!;
    private static ConfigEntry<float> MapIconSizeMult { get; set; } = null!;
    
    private static float _originalIconSize = 1.8f;
    private static bool MapIconSizeChanged { get; set; }
    private static AircraftDefinition? CachedTridentDefinition { get; set; }
    
    private void Awake()
    {
        Logger = base.Logger;
        
        PluginEnabled = Config.Bind("General", "Enable Plugin", true);
        MapIconSizeMult = Config.Bind("General", "Trident icon size multiplier", 3f);
        PluginEnabled.SettingChanged += (_, _) => OnSettingsChanged();
        MapIconSizeMult.SettingChanged += (_, _) => OnSettingsChanged();
        
        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
    }
    
    private static void OnSettingsChanged()
    {
        AdjustIconSize();
    }
    
    internal static void AdjustIconSize()
    {
        var tridentDefinition = GetTridentDefinition();
        if (tridentDefinition == null)
            return;
        
        if (!PluginEnabled.Value)
        {
            if (!MapIconSizeChanged)
                return;
            
            tridentDefinition.mapIconSize = _originalIconSize;
            MapIconSizeChanged = false;
        }
        else
        {
            if (!MapIconSizeChanged)
            {
                _originalIconSize = tridentDefinition.mapIconSize;
                MapIconSizeChanged = true;
            }
            
            tridentDefinition.mapIconSize = _originalIconSize * MapIconSizeMult.Value;
        }
    }
    
    private static AircraftDefinition? GetTridentDefinition()
    {
        if (CachedTridentDefinition != null)
            return CachedTridentDefinition;
        
        if (!Encyclopedia.Lookup.TryGetValue("PB22Trident", out var candidate) ||
            candidate is not AircraftDefinition tridentDefinition || tridentDefinition == null)
            return null;
        
        CachedTridentDefinition = tridentDefinition;
        return tridentDefinition;
    }
}

[HarmonyPatch]
internal static class HarmonyPatches
{
    [HarmonyPatch(typeof(Blueprinter.PatchRunner), nameof(Blueprinter.PatchRunner.ApplyAllOps))]
    [HarmonyPostfix]
    private static void BlueprinterReadyPatch()
    {
        Plugin.AdjustIconSize();
    }
}
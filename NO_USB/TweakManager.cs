using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NO_USB;

internal static class TweakManager
{
    private const string FireToleranceStat = "Missile.FireTolerance";
    private const string RadarSizeStat = "Missile.RadarSize";
    private const string VisibleRangeStat = "Missile.VisibleRange";
    
    private static bool _applied;
    
    internal static void ApplyAll()
    {
        if (_applied)
        {
            Plugin.Debug("Unit stat overrides were already processed, skipping duplicate ApplyAll.");
            return;
        }
        
        if (!ConfigManager.TryReadConfig(out var config))
        {
            Plugin.Debug("Unit stat overrides were not applied because the config could not be loaded.",
                Plugin.DebugType.LogError);
            return;
        }
        
        _applied = true;
        
        if (config.Overrides.Count == 0)
        {
            Plugin.Debug("Unit stat config contains no overrides.", Plugin.DebugType.LogError);
            return;
        }
        
        var seenOverrides = new HashSet<string>(StringComparer.Ordinal);
        var successful = 0;
        var failed = 0;
        var disabled = 0;
        var duplicates = 0;
        
        for (var index = 0; index < config.Overrides.Count; index++)
        {
            var tweak = config.Overrides[index];
            var entryNumber = index + 1;
            
            if (tweak == null)
            {
                Plugin.Debug($"Override #{entryNumber} is null, skipping.", Plugin.DebugType.LogWarning);
                failed++;
                continue;
            }
            
            if (tweak.Disabled)
            {
                disabled++;
                continue;
            }
            
            var jsonKey = tweak.JsonKey.Trim();
            var stat = tweak.Stat.Trim();
            
            if (string.IsNullOrEmpty(jsonKey))
            {
                Plugin.Debug($"Override #{entryNumber} has an empty jsonKey, skipping.", Plugin.DebugType.LogWarning);
                failed++;
                continue;
            }
            
            if (string.IsNullOrEmpty(stat))
            {
                Plugin.Debug($"Override #{entryNumber} for \"{jsonKey}\" has an empty stat, skipping.",
                    Plugin.DebugType.LogWarning);
                failed++;
                continue;
            }
            
            if (float.IsNaN(tweak.Value) || float.IsInfinity(tweak.Value))
            {
                Plugin.Debug($"Override #{entryNumber} for \"{jsonKey}\" {stat} has a non-finite value, skipping.",
                    Plugin.DebugType.LogWarning);
                failed++;
                continue;
            }
            
            // jsonKeys are case-sensitive like in Encyclopedia.Lookup, but stat names aren't
            var duplicateKey = jsonKey + "\u001F" + stat.ToUpperInvariant();
            
            if (!seenOverrides.Add(duplicateKey))
            {
                duplicates++;
                Plugin.Debug($"Duplicate override #{entryNumber} for \"{jsonKey}\"'s {stat}, " +
                             "latest duplicate entry takes priority.", Plugin.DebugType.LogWarning);
            }
            
            if (TryApply(jsonKey, stat, tweak.Value))
                successful++;
            else
                failed++;
        }
        
        Plugin.Debug($"Unit stat overrides complete. {successful} applied/validated, {failed} failed, " +
                     $"{disabled} disabled, {duplicates} duplicate(s).");
    }
    
    private static bool TryApply(string jsonKey, string stat, float configuredValue)
    {
        if (Encyclopedia.Lookup == null || !Encyclopedia.Lookup.TryGetValue(jsonKey, out var definition) || !definition)
        {
            Plugin.Debug($"Unit \"{jsonKey}\" was not found in Encyclopedia.Lookup.", Plugin.DebugType.LogWarning);
            return false;
        }
        
        // Currently only supports missiles so this is global
        // TO-DO: move it in specific sections when needed once more than missiles need support
        if (definition is not MissileDefinition missileDefinition)
        {
            Plugin.Debug($"Unit \"{jsonKey}\" is {definition.GetType().Name}, not MissileDefinition, " +
                         $"cannot apply {stat}.", Plugin.DebugType.LogWarning);
            return false;
        }
        
        switch (stat.ToLowerInvariant())
        {
            case "missile.firetolerance":
                return ApplyFireTolerance(jsonKey, stat, missileDefinition, configuredValue);
            
            case "missile.radarsize":
                return ApplyFloat(jsonKey, RadarSizeStat, missileDefinition.radarSize, configuredValue,
                    value => missileDefinition.radarSize = value);
            
            case "missile.visiblerange":
                return ApplyFloat(jsonKey, VisibleRangeStat, missileDefinition.visibleRange, configuredValue,
                    value => missileDefinition.visibleRange = value);
            
            default:
                Plugin.Debug($"Unsupported stat \"{stat}\" for \"{jsonKey}\". Supported stats: " +
                             $"{FireToleranceStat}, {RadarSizeStat}, {VisibleRangeStat}.", Plugin.DebugType.LogWarning);
                return false;
        }
    }
    
    private static bool ApplyFireTolerance(string jsonKey, string stat, MissileDefinition missileDefinition,
        float configuredValue)
    {
        if (!missileDefinition.unitPrefab)
        {
            Plugin.Debug($"Missile \"{jsonKey}\" has no unitPrefab, cannot apply {stat}.", Plugin.DebugType.LogWarning);
            return false;
        }
        
        var missile = missileDefinition.unitPrefab.GetComponent<Missile>();
        if (!missile)
        {
            Plugin.Debug(
                $"MissileDefinition \"{jsonKey}\" has no Missile component on its unitPrefab, cannot apply {stat}.",
                Plugin.DebugType.LogWarning);
            return false;
        }
        
        var armorProperties = missile.GetArmorProperties();
        if (armorProperties == null)
        {
            Plugin.Debug($"Missile \"{jsonKey}\" has null ArmorProperties, cannot apply {stat}.",
                Plugin.DebugType.LogWarning);
            return false;
        }
        
        return ApplyFloat(jsonKey, FireToleranceStat, armorProperties.fireTolerance, configuredValue,
            value => armorProperties.fireTolerance = value);
    }
    
    private static bool ApplyFloat(string jsonKey, string stat, float loadedValue, float configuredValue,
        Action<float> setter)
    {
        if (Mathf.Approximately(loadedValue, configuredValue))
        {
            Plugin.Debug($"{jsonKey} {stat} is already {FormatFloat(loadedValue)}.");
            return true;
        }
        
        setter(configuredValue);
        
        Plugin.Debug($"{jsonKey} {stat}: {FormatFloat(loadedValue)} => {FormatFloat(configuredValue)}.");
        return true;
    }
    
    private static string FormatFloat(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
}
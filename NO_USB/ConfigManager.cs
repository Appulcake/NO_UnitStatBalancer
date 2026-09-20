using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace NO_USB;

internal static class ConfigManager
{
    private const int SupportedSchemaVersion = 1;
    
    private static readonly string ConfigPath =
        Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.UnitStatTweaks.json");
    
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        MissingMemberHandling = MissingMemberHandling.Error
    };
    
    internal static bool TryReadConfig(out ConfigFile config)
    {
        config = null!;
        
        if (!File.Exists(ConfigPath))
        {
            Plugin.Debug($"Unit stat config ({ConfigPath}) was not found.", Plugin.DebugType.LogError);
            return false;
        }
        
        try
        {
            var json = File.ReadAllText(ConfigPath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                Plugin.Debug($"Unit stat config ({ConfigPath}) is empty.", Plugin.DebugType.LogError);
                return false;
            }
            
            var loaded = JsonConvert.DeserializeObject<ConfigFile>(json, JsonSettings);
            if (loaded == null)
            {
                Plugin.Debug($"Unit stat config ({ConfigPath}) deserialised to null.", Plugin.DebugType.LogError);
                return false;
            }
            
            if (loaded.SchemaVersion != SupportedSchemaVersion)
            {
                Plugin.Debug($"Unsupported unit stat config schema version {loaded.SchemaVersion} " +
                             $"(expected {SupportedSchemaVersion}).", Plugin.DebugType.LogError);
                return false;
            }
            
            config = loaded;
            return true;
        }
        catch (JsonException ex)
        {
            Plugin.Debug($"Invalid unit stat config JSON:\n{ex.Message}", Plugin.DebugType.LogError);
            return false;
        }
        catch (IOException ex)
        {
            Plugin.Debug($"Could not read unit stat config:\n{ex.Message}", Plugin.DebugType.LogError);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Plugin.Debug($"Could not access unit stat config:\n{ex.Message}", Plugin.DebugType.LogError);
            return false;
        }
        catch (Exception ex)
        {
            Plugin.Debug($"Unexpected error while loading unit stat config:\n{ex}", Plugin.DebugType.LogError);
            return false;
        }
    }
    
    internal sealed class ConfigFile
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }
        
        [JsonProperty("overrides", Required = Required.Always)]
        public List<FloatOverride> Overrides { get; set; } = [];
    }
    
    internal sealed class FloatOverride
    {
        [JsonProperty("jsonKey", Required = Required.Always)]
        public string JsonKey { get; set; } = "";
        
        [JsonProperty("stat", Required = Required.Always)]
        public string Stat { get; set; } = "";
        
        [JsonProperty("value", Required = Required.Always)]
        public float Value { get; set; }
        
        [JsonProperty("disabled")] public bool Disabled { get; set; }
        
        [JsonProperty("_comment")]
        public string? Comment { get; set; }
    }
}
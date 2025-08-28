using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Drawing.Search.Infrastructure;

public class SearchSettings
{
    private static readonly string ConfigFolderPath =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "config");

    private static readonly string ConfigFileName = "config.json";

    private static readonly string ConfigFilePath = Path.Combine(ConfigFolderPath, ConfigFileName);

    public bool ShowAllAssemblyPositions { get; set; } = false;
    public bool IsDarkMode { get; set; } = true;

    public bool WildcardSearch { get; set; } = false;
    public bool IsTestMode { get; set; } = true;


    public static SearchSettings Load()
    {
        if (!Directory.Exists(ConfigFilePath)) Directory.CreateDirectory(ConfigFolderPath);
        if (!File.Exists(ConfigFilePath)) return new SearchSettings();
        try
        {
            var jsonString = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<SearchSettings>(jsonString) ?? new SearchSettings();
        }
        catch
        {
            SearchLoggerServiceLocator.Current.LogError(new InvalidOperationException(), "Failed to load settings... using defaults.");
            return new SearchSettings();
        }
    }

    public void Save()
    {
        try
        {
            var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, jsonString);
        }
        catch (Exception e)
        {
            SearchLoggerServiceLocator.Current.LogError(e, "Failed to save settings.");
            throw;
        }
    }
}
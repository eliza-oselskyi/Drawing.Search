using System;
using System.IO;
using Newtonsoft.Json;

namespace Drawing.Search.Core;

public class SearchSettings
{
    private static readonly string ConfigFolderPath =
        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "config");

    private static readonly string ConfigFileName = "config.json";

    private static readonly string ConfigFilePath = Path.Combine(ConfigFolderPath, ConfigFileName);   
    
    public bool ShowAllAssemblyPositions { get; set; } = false;
    public bool IsDarkMode { get; set; } = true;

    public bool WildcardSearch { get; set; } = false;
    

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
            Console.WriteLine(e);
            throw;
        }
    }
}
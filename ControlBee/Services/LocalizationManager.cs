using System.Text.RegularExpressions;
using ControlBee.Interfaces;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlBee.Services;

public class LocalizationManager : ILocalizationManager
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(LocalizationManager));
    private static LocalizationManager? _instance;
    private JObject? _translations;

    public LocalizationManager()
    {
        // Set the singleton instance if not already set
        _instance ??= this;
    }

    /// <summary>
    /// Gets the singleton instance. This property is intended for use in XAML bindings.
    /// When using dependency injection, inject ILocalizationManager instead.
    /// </summary>
    public static LocalizationManager Instance =>
        _instance
        ?? throw new InvalidOperationException(
            "LocalizationManager has not been initialized. Ensure it's registered in DI container."
        );

    public void Load(string jsonPath)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            _translations = JObject.Parse(json);
        }
        catch (IOException)
        {
            Logger.Warn($"File not found. (${jsonPath})");
        }
    }

    public string? GetValue(string key)
    {
        return _translations?.SelectToken(key)?.ToString();
    }

    public string Translate(string key, Dictionary<string, string>? args = null)
    {
        var value = GetValue(key);
        if (value == null)
            return $"[MISSING:{key}]";

        // Replace placeholders like ${username}
        if (args != null)
            value = Regex.Replace(
                value,
                @"\$\{(\w+)\}",
                match =>
                {
                    var varName = match.Groups[1].Value;
                    return args.TryGetValue(varName, out var replacement)
                        ? replacement
                        : match.Value;
                }
            );

        return value;
    }
}

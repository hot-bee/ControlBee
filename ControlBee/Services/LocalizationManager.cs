using System.ComponentModel;
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
    private static readonly JsonMergeSettings MergeSettings = new()
    {
        MergeArrayHandling = MergeArrayHandling.Replace,
    };
    private JObject? _translations;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Translate(key);

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

    // Loads the files in order into one set of translations, so a shared file can carry the keys
    // every application has in common and the application's own file the rest. A key a later file
    // repeats replaces the earlier value, and each such override is logged so an intended override
    // and an accidental duplicate can be told apart. A file that cannot be read is skipped; when
    // none could be read the current translations stay as they are.
    public void Load(params string[] jsonPaths)
    {
        var merged = new JObject();
        var loaded = 0;
        foreach (var jsonPath in jsonPaths)
        {
            JObject next;
            try
            {
                next = JObject.Parse(File.ReadAllText(jsonPath));
            }
            catch (IOException)
            {
                Logger.Warn($"File not found. ({jsonPath})");
                continue;
            }
            foreach (var path in OverriddenPaths(merged, next))
                Logger.Warn($"Locale key '{path}' in {jsonPath} overrides an earlier file.");
            merged.Merge(next, MergeSettings);
            loaded++;
        }
        if (loaded == 0)
            return;
        _translations = merged;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    // Paths of the values in `incoming` that `existing` already holds with a different value.
    private static IEnumerable<string> OverriddenPaths(JObject existing, JObject incoming) =>
        incoming
            .Descendants()
            .OfType<JValue>()
            .Where(leaf =>
                existing.SelectToken(leaf.Path) is { } earlier && !JToken.DeepEquals(earlier, leaf)
            )
            .Select(leaf => leaf.Path);

    public string? GetValue(string key, Dictionary<string, string>? args = null)
    {
        var value = _translations?.SelectToken(key)?.ToString();
        if (value == null)
            return null;

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

    public string Translate(string key, Dictionary<string, string>? args = null)
    {
        return GetValue(key, args) ?? $"[MISSING:{key}]";
    }
}

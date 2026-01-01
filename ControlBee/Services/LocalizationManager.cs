using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlBee.Services;

public class LocalizationManager
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(LocalizationManager));
    private JObject? _translations;

    private LocalizationManager() { }

    public static LocalizationManager Instance { get; } = new();

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

    public string Translate(string key, Dictionary<string, string>? args = null)
    {
        var value = _translations?.SelectToken(key)?.ToString();
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

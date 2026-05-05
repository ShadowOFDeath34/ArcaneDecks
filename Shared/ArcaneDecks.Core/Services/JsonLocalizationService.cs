using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ArcaneDecks.Core.Services;

public class JsonLocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private string _currentLanguage = "en";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnLanguageChanged?.Invoke();
            }
        }
    }

    public event Action? OnLanguageChanged;

    public JsonLocalizationService(string basePath)
    {
        LoadLanguage(basePath, "en");
        LoadLanguage(basePath, "tr");
    }

    private void LoadLanguage(string basePath, string lang)
    {
        var filePath = Path.Combine(basePath, $"{lang}.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                _translations[lang] = dict;
            }
        }
    }

    public string Get(string key, params object[] args)
    {
        if (_translations.TryGetValue(_currentLanguage, out var langDict) && langDict.TryGetValue(key, out var text))
        {
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        // Fallback to English
        if (_translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enText))
        {
            return args.Length > 0 ? string.Format(enText, args) : enText;
        }

        return key;
    }
}

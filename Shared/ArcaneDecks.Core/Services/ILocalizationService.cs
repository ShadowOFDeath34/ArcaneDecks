using System;

namespace ArcaneDecks.Core.Services;

public interface ILocalizationService
{
    string Get(string key, params object[] args);
    string CurrentLanguage { get; set; }
    event Action? OnLanguageChanged;
}

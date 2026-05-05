using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class SettingsScreen : IScreen
{
    private readonly ScreenManager _screenManager;
    private readonly ILocalizationService _localization;
    private readonly CardSystem _cardSystem;
    private readonly CombatSystem _combatSystem;
    private readonly RunManager _runManager;
    private readonly List<EnemyTemplate> _enemyTemplates;

    private SpriteFont? _font;
    private GraphicsDevice? _graphics;
    private Texture2D? _pixel;
    private InputState _previousInput;

    private Rectangle _languageToggleRect;
    private Rectangle _backRect;
    private bool _languageHovered;
    private bool _backHovered;

    private readonly List<string> _languages = new() { "en", "tr" };

    public SettingsScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
    {
        _screenManager = screenManager;
        _localization = localization;
        _cardSystem = cardSystem;
        _combatSystem = combatSystem;
        _runManager = runManager;
        _enemyTemplates = enemyTemplates;
    }

    public void Initialize() { }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphics = graphicsDevice;
        _font = contentManager.Load<SpriteFont>("Font");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        int centerX = graphicsDevice.Viewport.Width / 2;
        _languageToggleRect = new Rectangle(centerX - 150, 320, 300, 55);
        _backRect = new Rectangle(centerX - 150, 420, 300, 55);
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
    }

    public void Show() { }
    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        _languageHovered = _languageToggleRect.Contains(input.MousePosition);
        _backHovered = _backRect.Contains(input.MousePosition);

        if (input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
        {
            if (_languageHovered)
            {
                int idx = _languages.IndexOf(_localization.CurrentLanguage);
                _localization.CurrentLanguage = _languages[(idx + 1) % _languages.Count];
            }

            if (_backHovered)
            {
                _screenManager.ChangeScreen(new MainMenuScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
            }
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null || _pixel == null) return;

        spriteBatch.Begin();

        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(25, 15, 50));

        var title = _localization.Get("ui.settings.title");
        var titleSize = _font.MeasureString(title);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.5f) / 2, 120);
        spriteBatch.DrawString(_font, title, titlePos, Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        // Language label
        var langLabel = _localization.Get("ui.settings.language") + ":";
        var langLabelSize = _font.MeasureString(langLabel);
        var langLabelPos = new Vector2(_languageToggleRect.X, _languageToggleRect.Y - langLabelSize.Y - 8);
        spriteBatch.DrawString(_font, langLabel, langLabelPos, Color.LightGray);

        // Language toggle button
        var langBtnColor = _languageHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _languageToggleRect, langBtnColor);
        DrawBorder(spriteBatch, _languageToggleRect, new Color(100, 80, 150), 2);

        var currentLangKey = _localization.CurrentLanguage == "tr" ? "ui.settings.language_tr" : "ui.settings.language_en";
        var langText = _localization.Get(currentLangKey);
        var langTextSize = _font.MeasureString(langText);
        var langTextPos = new Vector2(
            _languageToggleRect.X + (_languageToggleRect.Width - langTextSize.X) / 2,
            _languageToggleRect.Y + (_languageToggleRect.Height - langTextSize.Y) / 2);
        spriteBatch.DrawString(_font, langText, langTextPos, Color.White);

        // Back button
        var backBtnColor = _backHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _backRect, backBtnColor);
        DrawBorder(spriteBatch, _backRect, new Color(100, 80, 150), 2);

        var backText = _localization.Get("ui.settings.back");
        var backTextSize = _font.MeasureString(backText);
        var backTextPos = new Vector2(
            _backRect.X + (_backRect.Width - backTextSize.X) / 2,
            _backRect.Y + (_backRect.Height - backTextSize.Y) / 2);
        spriteBatch.DrawString(_font, backText, backTextPos, Color.White);

        spriteBatch.End();
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }
}

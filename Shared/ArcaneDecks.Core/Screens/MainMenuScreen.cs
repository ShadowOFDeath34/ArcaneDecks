using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class MainMenuScreen : IScreen
{
    private readonly ScreenManager _screenManager;
    private readonly ILocalizationService _localization;
    private readonly CardSystem _cardSystem;
    private readonly CombatSystem _combatSystem;
    private readonly List<EnemyTemplate> _enemyTemplates;

    private SpriteFont? _font;
    private GraphicsDevice? _graphics;
    private Texture2D? _pixel;
    private readonly List<MenuButton> _buttons = new();
    private InputState _previousInput;

    public MainMenuScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, List<EnemyTemplate> enemyTemplates)
    {
        _screenManager = screenManager;
        _localization = localization;
        _cardSystem = cardSystem;
        _combatSystem = combatSystem;
        _enemyTemplates = enemyTemplates;
    }

    public void Initialize() { }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphics = graphicsDevice;
        _font = contentManager.Load<SpriteFont>("Font");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        BuildButtons();
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
        _buttons.Clear();
    }

    public void Show() { }
    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        foreach (var btn in _buttons)
        {
            btn.IsHovered = btn.Bounds.Contains(input.MousePosition);

            if (input.MouseLeftReleased && btn.IsHovered && !_previousInput.MouseLeftPressed)
            {
                btn.OnClick();
            }
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null) return;

        spriteBatch.Begin();

        // Background
        spriteBatch.Draw(GetPixel(), _graphics.Viewport.Bounds, new Color(25, 15, 50));

        // Title
        var titleText = _localization.Get("game.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2(
            (_graphics.Viewport.Width - titleSize.X * 1.5f) / 2,
            80
        );
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        // Buttons
        foreach (var btn in _buttons)
        {
            var bgColor = btn.IsHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
            spriteBatch.Draw(GetPixel(), btn.Bounds, bgColor);

            var textSize = _font.MeasureString(btn.Text);
            var textPos = new Vector2(
                btn.Bounds.X + (btn.Bounds.Width - textSize.X) / 2,
                btn.Bounds.Y + (btn.Bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(_font, btn.Text, textPos, btn.IsHovered ? Color.White : new Color(200, 200, 220));
        }

        spriteBatch.End();
    }

    private void BuildButtons()
    {
        if (_graphics == null) return;

        var labels = new[]
        {
            _localization.Get("ui.main_menu.play"),
            _localization.Get("ui.main_menu.deck"),
            _localization.Get("ui.main_menu.settings"),
            _localization.Get("ui.main_menu.quit"),
        };

        var actions = new Action[]
        {
            () => _screenManager.ChangeScreen(new BattleScreen(_screenManager, _localization, _cardSystem, _combatSystem, _enemyTemplates)),
            () => { },
            () => { },
            () => Environment.Exit(0),
        };

        int btnWidth = 300;
        int btnHeight = 55;
        int spacing = 18;
        int startY = 260;
        int centerX = _graphics.Viewport.Width / 2;

        _buttons.Clear();
        for (int i = 0; i < labels.Length; i++)
        {
            var rect = new Rectangle(centerX - btnWidth / 2, startY + i * (btnHeight + spacing), btnWidth, btnHeight);
            _buttons.Add(new MenuButton(labels[i], rect, actions[i]));
        }
    }

    private Texture2D GetPixel()
    {
        return _pixel!;
    }

    private class MenuButton
    {
        public string Text { get; }
        public Rectangle Bounds { get; }
        public Action OnClick { get; }
        public bool IsHovered { get; set; }

        public MenuButton(string text, Rectangle bounds, Action onClick)
        {
            Text = text;
            Bounds = bounds;
            OnClick = onClick;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class GameOverScreen : IScreen
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

    private readonly bool _victory;
    private readonly int _score;
    private readonly int _gold;
    private readonly int _floor;

    private Rectangle _mainMenuRect;
    private bool _mainMenuHovered;

    public GameOverScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates, bool victory, int score, int gold, int floor)
    {
        _screenManager = screenManager;
        _localization = localization;
        _cardSystem = cardSystem;
        _combatSystem = combatSystem;
        _runManager = runManager;
        _enemyTemplates = enemyTemplates;
        _victory = victory;
        _score = score;
        _gold = gold;
        _floor = floor;
    }

    public void Initialize() { }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphics = graphicsDevice;
        _font = contentManager.Load<SpriteFont>("Font");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _mainMenuRect = new Rectangle(
            graphicsDevice.Viewport.Width / 2 - 150,
            graphicsDevice.Viewport.Height - 180,
            300, 55);
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
    }

    private bool _scoreSubmitted;

    public void Show()
    {
        if (_screenManager.BackendService == null || _scoreSubmitted) return;
        _scoreSubmitted = true;
        _ = Task.Run(async () =>
        {
            await _screenManager.BackendService.SubmitScoreAsync(
                "global",
                _screenManager.AuthService?.PlayerId ?? "anonymous",
                _score,
                _floor);
            await _screenManager.BackendService.SaveProgressAsync(_runManager.State);
        });
    }

    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        _mainMenuHovered = _mainMenuRect.Contains(input.MousePosition);
        if (_mainMenuHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
        {
            _screenManager.ChangeScreen(new MainMenuScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null || _pixel == null) return;

        spriteBatch.Begin();

        // Background
        var bgColor = _victory ? new Color(20, 35, 20) : new Color(35, 15, 15);
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, bgColor);

        // Title
        var titleKey = _victory ? "ui.game_over.victory" : "ui.game_over.defeat";
        var titleText = _localization.Get(titleKey);
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 2f) / 2, 100);
        var titleColor = _victory ? Color.Gold : Color.IndianRed;
        spriteBatch.DrawString(_font, titleText, titlePos, titleColor, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);

        // Stats
        var floorText = string.Format(_localization.Get("ui.game_over.floor"), _floor);
        var goldText = string.Format(_localization.Get("ui.game_over.gold"), _gold);
        var scoreText = string.Format(_localization.Get("ui.game_over.score"), _score);

        var stats = new[] { floorText, goldText, scoreText };
        float startY = titlePos.Y + 100;
        for (int i = 0; i < stats.Length; i++)
        {
            var size = _font.MeasureString(stats[i]);
            var pos = new Vector2((_graphics.Viewport.Width - size.X * 1.2f) / 2, startY + i * 60);
            spriteBatch.DrawString(_font, stats[i], pos, Color.LightGray, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        }

        // Main Menu button
        var btnColor = _mainMenuHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _mainMenuRect, btnColor);
        DrawBorder(spriteBatch, _mainMenuRect, new Color(100, 80, 150), 2);

        var menuText = _localization.Get("ui.game_over.main_menu");
        var menuSize = _font.MeasureString(menuText);
        var menuPos = new Vector2(
            _mainMenuRect.X + (_mainMenuRect.Width - menuSize.X) / 2,
            _mainMenuRect.Y + (_mainMenuRect.Height - menuSize.Y) / 2);
        spriteBatch.DrawString(_font, menuText, menuPos, Color.White);

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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class DeckScreen : IScreen
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

    private readonly List<CardDefinition> _deckCards = new();
    private readonly List<Rectangle> _cardRects = new();
    private Rectangle _backRect;
    private bool _backHovered;
    private int _hoveredCardIndex = -1;

    public DeckScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
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

        BuildDeck();
        LayoutCards();

        _backRect = new Rectangle(
            graphicsDevice.Viewport.Width / 2 - 150,
            graphicsDevice.Viewport.Height - 100,
            300, 55);
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
        _deckCards.Clear();
        _cardRects.Clear();
    }

    public void Show() { }
    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        _hoveredCardIndex = -1;
        for (int i = 0; i < _cardRects.Count; i++)
        {
            if (_cardRects[i].Contains(input.MousePosition))
            {
                _hoveredCardIndex = i;
                break;
            }
        }

        _backHovered = _backRect.Contains(input.MousePosition);
        if (_backHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
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
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(20, 20, 35));

        // Title
        var titleText = _localization.Get("ui.deck.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.5f) / 2, 30);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        // Card count
        var countText = string.Format(_localization.Get("ui.deck.count"), _deckCards.Count);
        var countSize = _font.MeasureString(countText);
        var countPos = new Vector2((_graphics.Viewport.Width - countSize.X) / 2, titlePos.Y + 50);
        spriteBatch.DrawString(_font, countText, countPos, Color.LightGray);

        // Cards
        for (int i = 0; i < _deckCards.Count && i < _cardRects.Count; i++)
        {
            var rect = _cardRects[i];
            var card = _deckCards[i];
            bool hovered = i == _hoveredCardIndex;

            var cardBg = hovered ? new Color(55, 70, 55) : new Color(40, 50, 40);
            var borderColor = card.Type switch
            {
                CardType.Attack => new Color(180, 60, 60),
                CardType.Skill => new Color(60, 120, 180),
                CardType.Power => new Color(180, 140, 60),
                _ => Color.Gray,
            };

            // Card background
            spriteBatch.Draw(_pixel, rect, cardBg);
            // Border
            DrawBorder(spriteBatch, rect, borderColor, 2);

            // Cost circle (top right)
            var costRect = new Rectangle(rect.Right - 24, rect.Y + 6, 18, 18);
            spriteBatch.Draw(_pixel, costRect, Color.DarkSlateBlue);
            var costText = card.Cost.ToString();
            var costSize = _font.MeasureString(costText);
            spriteBatch.DrawString(_font, costText, new Vector2(costRect.X + (18 - costSize.X) / 2, costRect.Y + (18 - costSize.Y) / 2), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            // Name
            var nameText = _localization.Get(card.NameKey);
            var nameSize = _font.MeasureString(nameText);
            var namePos = new Vector2(rect.X + (rect.Width - nameSize.X * 0.75f) / 2, rect.Y + 28);
            spriteBatch.DrawString(_font, nameText, namePos, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // Description
            var effectArgs = card.Effects.Select(e => (object)e.Value).ToArray();
            var descText = _localization.Get(card.DescriptionKey, effectArgs);
            var descSize = _font.MeasureString(descText);
            var descPos = new Vector2(rect.X + (rect.Width - descSize.X * 0.65f) / 2, rect.Y + 70);
            spriteBatch.DrawString(_font, descText, descPos, new Color(200, 200, 200), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        // Back button
        var btnColor = _backHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _backRect, btnColor);
        DrawBorder(spriteBatch, _backRect, new Color(100, 80, 150), 2);

        var backText = _localization.Get("ui.deck.back");
        var backSize = _font.MeasureString(backText);
        var backPos = new Vector2(
            _backRect.X + (_backRect.Width - backSize.X) / 2,
            _backRect.Y + (_backRect.Height - backSize.Y) / 2);
        spriteBatch.DrawString(_font, backText, backPos, Color.White);

        spriteBatch.End();
    }

    private void BuildDeck()
    {
        _deckCards.Clear();

        var ids = _runManager.State.IsActive
            ? _runManager.State.DeckCardIds
            : _cardSystem.GetStarterDeck().Select(c => c.Id).ToList();

        foreach (var id in ids)
        {
            var card = _cardSystem.GetCard(id);
            if (card != null)
                _deckCards.Add(card);
        }
    }

    private void LayoutCards()
    {
        if (_graphics == null) return;

        int cardWidth = 140;
        int cardHeight = 170;
        int gapX = 20;
        int gapY = 20;
        int startY = 120;
        int maxPerRow = Math.Max(1, (_graphics.Viewport.Width - 40) / (cardWidth + gapX));

        _cardRects.Clear();
        for (int i = 0; i < _deckCards.Count; i++)
        {
            int row = i / maxPerRow;
            int col = i % maxPerRow;
            int rowWidth = Math.Min(maxPerRow, _deckCards.Count - row * maxPerRow) * cardWidth + (Math.Min(maxPerRow, _deckCards.Count - row * maxPerRow) - 1) * gapX;
            int startX = (_graphics.Viewport.Width - rowWidth) / 2;
            _cardRects.Add(new Rectangle(startX + col * (cardWidth + gapX), startY + row * (cardHeight + gapY), cardWidth, cardHeight));
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }
}

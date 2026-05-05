using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class ShopScreen : IScreen
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

    private readonly List<ShopItem> _items = new();
    private Rectangle _continueRect;
    private bool _continueHovered;

    public ShopScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
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

        BuildItems();

        _continueRect = new Rectangle(
            graphicsDevice.Viewport.Width / 2 - 150,
            graphicsDevice.Viewport.Height - 120,
            300, 55);
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
        _items.Clear();
    }

    public void Show() { }
    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        foreach (var item in _items)
        {
            if (item.Sold) continue;
            item.IsHovered = item.Bounds.Contains(input.MousePosition);

            if (item.IsHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
            {
                if (_runManager.State.Gold >= item.Cost)
                {
                    _runManager.State.Gold -= item.Cost;
                    item.OnBuy?.Invoke();
                    item.Sold = true;
                }
            }
        }

        _continueHovered = _continueRect.Contains(input.MousePosition);
        if (_continueHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
        {
            ContinueToNextFloor();
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null || _pixel == null) return;

        spriteBatch.Begin();

        // Background
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(25, 20, 45));

        // Title
        var titleText = _localization.Get("ui.shop.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.8f) / 2, 60);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);

        // Gold display
        var goldText = string.Format(_localization.Get("ui.shop.gold"), _runManager.State.Gold);
        var goldSize = _font.MeasureString(goldText);
        var goldPos = new Vector2((_graphics.Viewport.Width - goldSize.X * 1.2f) / 2, titlePos.Y + 70);
        spriteBatch.DrawString(_font, goldText, goldPos, Color.Gold, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        // Items
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var rect = item.Bounds;

            var bgColor = item.Sold ? new Color(40, 40, 40) : (item.IsHovered ? new Color(60, 50, 90) : new Color(45, 40, 70));
            spriteBatch.Draw(_pixel, rect, bgColor);
            DrawBorder(spriteBatch, rect, new Color(100, 90, 140), 2);

            // Name
            var nameText = _localization.Get(item.NameKey);
            var nameSize = _font.MeasureString(nameText);
            var namePos = new Vector2(rect.X + (rect.Width - nameSize.X * 0.9f) / 2, rect.Y + 25);
            spriteBatch.DrawString(_font, nameText, namePos, item.Sold ? Color.Gray : Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            // Description
            var descText = item.DescriptionArgs.Length > 0
                ? _localization.Get(item.DescriptionKey, item.DescriptionArgs)
                : _localization.Get(item.DescriptionKey);
            var descSize = _font.MeasureString(descText);
            var descPos = new Vector2(rect.X + (rect.Width - descSize.X * 0.75f) / 2, rect.Y + 70);
            spriteBatch.DrawString(_font, descText, descPos, item.Sold ? Color.Gray : new Color(200, 200, 200), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            // Cost / Buy button area
            var btnY = rect.Y + rect.Height - 55;
            var btnRect = new Rectangle(rect.X + 20, btnY, rect.Width - 40, 40);

            if (item.Sold)
            {
                var soldText = _localization.Get("ui.shop.sold_out");
                var soldSize = _font.MeasureString(soldText);
                var soldPos = new Vector2(rect.X + (rect.Width - soldSize.X * 0.85f) / 2, btnY + 8);
                spriteBatch.DrawString(_font, soldText, soldPos, Color.Gray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            }
            else
            {
                var canAfford = _runManager.State.Gold >= item.Cost;
                var btnColor = item.IsHovered && canAfford ? new Color(60, 140, 60) : (canAfford ? new Color(40, 110, 40) : new Color(80, 40, 40));
                spriteBatch.Draw(_pixel, btnRect, btnColor);
                DrawBorder(spriteBatch, btnRect, canAfford ? Color.LightGreen : Color.IndianRed, 2);

                var buyText = string.Format(_localization.Get("ui.shop.buy"), item.Cost);
                var buySize = _font.MeasureString(buyText);
                var buyPos = new Vector2(btnRect.X + (btnRect.Width - buySize.X) / 2, btnRect.Y + (btnRect.Height - buySize.Y) / 2);
                spriteBatch.DrawString(_font, buyText, buyPos, canAfford ? Color.White : Color.Gray);
            }
        }

        // Continue button
        var contColor = _continueHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _continueRect, contColor);
        DrawBorder(spriteBatch, _continueRect, new Color(100, 80, 150), 2);

        var contText = _localization.Get("ui.shop.continue");
        var contSize = _font.MeasureString(contText);
        var contPos = new Vector2(
            _continueRect.X + (_continueRect.Width - contSize.X) / 2,
            _continueRect.Y + (_continueRect.Height - contSize.Y) / 2);
        spriteBatch.DrawString(_font, contText, contPos, Color.White);

        spriteBatch.End();
    }

    private void BuildItems()
    {
        if (_graphics == null) return;

        _items.Clear();
        var random = new Random();

        // 1. Card offer: random card not currently in deck, weighted by rarity
        var allCards = _cardSystem.GetAllCards().ToList();
        var deckIds = _runManager.State.DeckCardIds.ToHashSet();
        var candidates = allCards.Where(c => !deckIds.Contains(c.Id)).ToList();
        if (candidates.Count == 0) candidates = allCards; // fallback if deck has everything

        var offeredCard = candidates[random.Next(candidates.Count)];
        int cardCost = offeredCard.Rarity switch
        {
            CardRarity.Common => 50,
            CardRarity.Uncommon => 80,
            CardRarity.Rare => 120,
            CardRarity.Legendary => 200,
            _ => 50
        };

        _items.Add(new ShopItem
        {
            NameKey = offeredCard.NameKey,
            DescriptionKey = offeredCard.DescriptionKey,
            Cost = cardCost,
            OnBuy = () => _runManager.AddCardToDeck(offeredCard.Id),
        });

        // 2. Heal offer
        _items.Add(new ShopItem
        {
            NameKey = "ui.shop.heal_name",
            DescriptionKey = "ui.shop.heal_desc",
            DescriptionArgs = new object[] { 10 },
            Cost = 40,
            OnBuy = () => _runManager.HealPlayer(10),
        });

        // 3. Max health upgrade
        _items.Add(new ShopItem
        {
            NameKey = "ui.shop.max_health_name",
            DescriptionKey = "ui.shop.max_health_desc",
            DescriptionArgs = new object[] { 5 },
            Cost = 60,
            OnBuy = () =>
            {
                _runManager.State.PlayerMaxHealth += 5;
                _runManager.State.PlayerCurrentHealth += 5;
            },
        });

        // Layout items horizontally
        int itemWidth = 220;
        int itemHeight = 260;
        int gap = 30;
        int totalWidth = _items.Count * itemWidth + (_items.Count - 1) * gap;
        int startX = (_graphics.Viewport.Width - totalWidth) / 2;
        int startY = 220;

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Bounds = new Rectangle(startX + i * (itemWidth + gap), startY, itemWidth, itemHeight);
        }
    }

    private void ContinueToNextFloor()
    {
        _runManager.AdvanceFloor();
        var nextType = _runManager.GetCurrentFloorType();

        if (nextType == FloorType.Combat || nextType == FloorType.Boss)
        {
            _screenManager.ChangeScreen(new BattleScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        }
        else if (nextType == FloorType.Shop)
        {
            // Rare: two shops in a row — just create another shop screen
            _screenManager.ChangeScreen(new ShopScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        }
        else
        {
            _screenManager.ChangeScreen(new EventScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class ShopItem
    {
        public string NameKey { get; init; } = "";
        public string DescriptionKey { get; init; } = "";
        public object[] DescriptionArgs { get; init; } = Array.Empty<object>();
        public int Cost { get; init; }
        public bool Sold { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsHovered { get; set; }
        public Action? OnBuy { get; init; }
    }
}

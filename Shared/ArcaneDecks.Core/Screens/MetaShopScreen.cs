using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class MetaShopScreen : IScreen
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

    private readonly List<UpgradeItem> _items = new();
    private Rectangle _backRect;
    private bool _backHovered;

    public MetaShopScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
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

        _backRect = new Rectangle(
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
            item.IsHovered = item.Bounds.Contains(input.MousePosition);

            if (item.IsHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
            {
                var meta = _screenManager.MetaProgressionSystem;
                if (meta != null && meta.PurchaseUpgrade(item.UpgradeId))
                {
                    item.Level = meta.GetCurrentLevel(item.UpgradeId);
                    _ = SaveMetaAsync();
                }
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
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(20, 15, 40));

        // Title
        var titleText = _localization.Get("ui.meta.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.8f) / 2, 60);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);

        // Teeth display
        var meta = _screenManager.MetaProgressionSystem;
        var teeth = meta?.Data.GoblinTeeth ?? 0;
        var teethText = string.Format(_localization.Get("ui.meta.teeth"), teeth);
        var teethSize = _font.MeasureString(teethText);
        var teethPos = new Vector2((_graphics.Viewport.Width - teethSize.X * 1.2f) / 2, titlePos.Y + 70);
        spriteBatch.DrawString(_font, teethText, teethPos, new Color(200, 180, 100), 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        // Items
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var rect = item.Bounds;

            var bgColor = item.IsHovered ? new Color(60, 50, 90) : new Color(45, 40, 70);
            if (item.IsMaxed) bgColor = new Color(40, 50, 40);
            spriteBatch.Draw(_pixel, rect, bgColor);
            DrawBorder(spriteBatch, rect, new Color(100, 90, 140), 2);

            // Name
            var nameText = _localization.Get(item.NameKey);
            var nameSize = _font.MeasureString(nameText);
            var namePos = new Vector2(rect.X + (rect.Width - nameSize.X * 0.9f) / 2, rect.Y + 20);
            spriteBatch.DrawString(_font, nameText, namePos, item.IsMaxed ? Color.Gray : Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            // Description with current effect
            var effectTotal = item.Level * item.EffectPerLevel;
            var nextEffect = (item.Level + 1) * item.EffectPerLevel;
            var descText = item.IsMaxed
                ? string.Format(_localization.Get(item.DescKey), effectTotal)
                : string.Format(_localization.Get(item.DescKey) + " (+" + item.EffectPerLevel + ")", effectTotal);
            var descSize = _font.MeasureString(descText);
            var descPos = new Vector2(rect.X + (rect.Width - descSize.X * 0.7f) / 2, rect.Y + 65);
            spriteBatch.DrawString(_font, descText, descPos, item.IsMaxed ? Color.Gray : new Color(200, 200, 200), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // Level indicator
            var levelText = $"Lv {item.Level}/{item.MaxLevel}";
            var levelSize = _font.MeasureString(levelText);
            var levelPos = new Vector2(rect.X + (rect.Width - levelSize.X * 0.8f) / 2, rect.Y + 110);
            spriteBatch.DrawString(_font, levelText, levelPos, item.IsMaxed ? Color.Gold : Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // Buy button area
            var btnY = rect.Y + rect.Height - 55;
            var btnRect = new Rectangle(rect.X + 20, btnY, rect.Width - 40, 40);

            if (item.IsMaxed)
            {
                var maxedText = _localization.Get("ui.meta.maxed");
                var maxedSize = _font.MeasureString(maxedText);
                var maxedPos = new Vector2(rect.X + (rect.Width - maxedSize.X * 0.85f) / 2, btnY + 8);
                spriteBatch.DrawString(_font, maxedText, maxedPos, Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
            }
            else
            {
                var canAfford = meta != null && meta.Data.GoblinTeeth >= item.Cost;
                var btnColor = item.IsHovered && canAfford ? new Color(60, 140, 60) : (canAfford ? new Color(40, 110, 40) : new Color(80, 40, 40));
                spriteBatch.Draw(_pixel, btnRect, btnColor);
                DrawBorder(spriteBatch, btnRect, canAfford ? Color.LightGreen : Color.IndianRed, 2);

                var buyText = string.Format(_localization.Get("ui.meta.buy"), item.Cost);
                var buySize = _font.MeasureString(buyText);
                var buyPos = new Vector2(btnRect.X + (btnRect.Width - buySize.X) / 2, btnRect.Y + (btnRect.Height - buySize.Y) / 2);
                spriteBatch.DrawString(_font, buyText, buyPos, canAfford ? Color.White : Color.Gray);
            }
        }

        // Back button
        var backColor = _backHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _backRect, backColor);
        DrawBorder(spriteBatch, _backRect, new Color(100, 80, 150), 2);

        var backText = _localization.Get("ui.meta.back");
        var backSize = _font.MeasureString(backText);
        var backPos = new Vector2(
            _backRect.X + (_backRect.Width - backSize.X) / 2,
            _backRect.Y + (_backRect.Height - backSize.Y) / 2);
        spriteBatch.DrawString(_font, backText, backPos, Color.White);

        spriteBatch.End();
    }

    private void BuildItems()
    {
        if (_graphics == null) return;
        _items.Clear();

        var meta = _screenManager.MetaProgressionSystem;
        foreach (var upgrade in MetaProgressionSystem.Upgrades)
        {
            var level = meta?.GetCurrentLevel(upgrade.Id) ?? 0;
            _items.Add(new UpgradeItem
            {
                UpgradeId = upgrade.Id,
                NameKey = upgrade.NameKey,
                DescKey = upgrade.DescriptionKey,
                MaxLevel = upgrade.MaxLevel,
                EffectPerLevel = upgrade.EffectPerLevel,
                Level = level,
                Cost = meta?.GetCost(upgrade.Id) ?? upgrade.BaseCost,
                IsMaxed = level >= upgrade.MaxLevel
            });
        }

        int itemWidth = 260;
        int itemHeight = 280;
        int gap = 40;
        int totalWidth = _items.Count * itemWidth + (_items.Count - 1) * gap;
        int startX = (_graphics.Viewport.Width - totalWidth) / 2;
        int startY = 220;

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Bounds = new Rectangle(startX + i * (itemWidth + gap), startY, itemWidth, itemHeight);
        }
    }

    private async System.Threading.Tasks.Task SaveMetaAsync()
    {
        if (_screenManager.BackendService == null || _screenManager.MetaProgressionSystem == null) return;
        var dto = _screenManager.MetaProgressionSystem.ToDto();
        await _screenManager.BackendService.SaveMetaProgressAsync(dto);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class UpgradeItem
    {
        public string UpgradeId { get; init; } = "";
        public string NameKey { get; init; } = "";
        public string DescKey { get; init; } = "";
        public int MaxLevel { get; init; }
        public int EffectPerLevel { get; init; }
        public int Level { get; set; }
        public int Cost { get; set; }
        public bool IsMaxed { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsHovered { get; set; }
    }
}

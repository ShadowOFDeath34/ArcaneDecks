#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class IAPShopScreen : IScreen
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

    private readonly List<IAPProduct> _products = new();
    private readonly List<IAPProductButton> _buttons = new();
    private bool _isLoading = true;
    private string _statusText = "";
    private double _statusTimer = 0;

    private Rectangle _backRect;
    private bool _backHovered;

    private static readonly string[] DefaultProductIds = new[]
    {
        "remove_ads",
        "starter_pack",
        "premium_currency_100",
        "premium_currency_500",
        "premium_currency_1000"
    };

    public IAPShopScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
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

        _backRect = new Rectangle(40, 40, 140, 50);
        _statusText = _localization.Get("ui.iap.loading");

        _ = Task.Run(async () =>
        {
            var iap = _screenManager.IAPService;
            if (iap == null || !iap.IsInitialized)
            {
                _statusText = _localization.Get("ui.iap.unavailable");
                _isLoading = false;
                return;
            }

            try
            {
                var queried = await iap.QueryProductsAsync(DefaultProductIds);
                lock (_products)
                {
                    _products.Clear();
                    _products.AddRange(queried);
                }
                _isLoading = false;
                if (_products.Count == 0)
                {
                    _statusText = _localization.Get("ui.iap.no_products");
                }
            }
            catch
            {
                _statusText = _localization.Get("ui.iap.error");
                _isLoading = false;
            }
        });
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
        _products.Clear();
        _buttons.Clear();
    }

    public void Show()
    {
        var iap = _screenManager.IAPService;
        if (iap != null)
        {
            iap.PurchaseCompleted += OnPurchaseCompleted;
            iap.PurchaseFailed += OnPurchaseFailed;
        }
    }

    public void Hide()
    {
        var iap = _screenManager.IAPService;
        if (iap != null)
        {
            iap.PurchaseCompleted -= OnPurchaseCompleted;
            iap.PurchaseFailed -= OnPurchaseFailed;
        }
    }

    private void OnPurchaseCompleted(string productId)
    {
        _statusText = string.Format(_localization.Get("ui.iap.purchase_success"), productId);
        _statusTimer = 3.0;
    }

    private void OnPurchaseFailed(string productId, string reason)
    {
        _statusText = string.Format(_localization.Get("ui.iap.purchase_failed"), reason);
        _statusTimer = 3.0;
    }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null) return;

        if (_statusTimer > 0)
        {
            _statusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_statusTimer <= 0) _statusText = "";
        }

        BuildButtonsIfNeeded();

        foreach (var btn in _buttons)
        {
            btn.IsHovered = btn.Bounds.Contains(input.MousePosition);
            if (btn.IsHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
            {
                _screenManager.IAPService?.Purchase(btn.Product.Id);
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
        var titleText = _localization.Get("ui.iap.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.8f) / 2, 40);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);

        // Back button
        var backColor = _backHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _backRect, backColor);
        DrawBorder(spriteBatch, _backRect, new Color(100, 80, 150), 2);
        var backText = _localization.Get("ui.iap.back");
        var backSize = _font.MeasureString(backText);
        var backPos = new Vector2(
            _backRect.X + (_backRect.Width - backSize.X) / 2,
            _backRect.Y + (_backRect.Height - backSize.Y) / 2);
        spriteBatch.DrawString(_font, backText, backPos, Color.White);

        if (_isLoading)
        {
            var loadSize = _font.MeasureString(_statusText);
            var loadPos = new Vector2((_graphics.Viewport.Width - loadSize.X) / 2, _graphics.Viewport.Height / 2);
            spriteBatch.DrawString(_font, _statusText, loadPos, Color.LightGray);
        }
        else if (_products.Count == 0)
        {
            var emptySize = _font.MeasureString(_statusText);
            var emptyPos = new Vector2((_graphics.Viewport.Width - emptySize.X) / 2, _graphics.Viewport.Height / 2);
            spriteBatch.DrawString(_font, _statusText, emptyPos, Color.Gray);
        }
        else
        {
            foreach (var btn in _buttons)
            {
                var rect = btn.Bounds;
                var bgColor = btn.IsHovered ? new Color(60, 50, 90) : new Color(45, 40, 70);
                spriteBatch.Draw(_pixel, rect, bgColor);
                DrawBorder(spriteBatch, rect, new Color(100, 90, 140), 2);

                // Name
                var nameSize = _font.MeasureString(btn.Product.Name);
                var namePos = new Vector2(rect.X + (rect.Width - nameSize.X * 0.9f) / 2, rect.Y + 20);
                spriteBatch.DrawString(_font, btn.Product.Name, namePos, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

                // Description
                var descSize = _font.MeasureString(btn.Product.Description);
                var descPos = new Vector2(rect.X + (rect.Width - descSize.X * 0.75f) / 2, rect.Y + 70);
                spriteBatch.DrawString(_font, btn.Product.Description, descPos, new Color(200, 200, 200), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

                // Price button
                var priceRect = new Rectangle(rect.X + 20, rect.Y + rect.Height - 55, rect.Width - 40, 40);
                var priceColor = btn.IsHovered ? new Color(60, 140, 60) : new Color(40, 110, 40);
                spriteBatch.Draw(_pixel, priceRect, priceColor);
                DrawBorder(spriteBatch, priceRect, Color.LightGreen, 2);

                var priceSize = _font.MeasureString(btn.Product.Price);
                var pricePos = new Vector2(priceRect.X + (priceRect.Width - priceSize.X) / 2, priceRect.Y + (priceRect.Height - priceSize.Y) / 2);
                spriteBatch.DrawString(_font, btn.Product.Price, pricePos, Color.White);
            }
        }

        // Status toast
        if (!string.IsNullOrEmpty(_statusText) && _statusTimer > 0)
        {
            var toastSize = _font.MeasureString(_statusText);
            var toastPos = new Vector2((_graphics.Viewport.Width - toastSize.X) / 2, _graphics.Viewport.Height - 100);
            spriteBatch.DrawString(_font, _statusText, toastPos, _statusText.Contains(_localization.Get("ui.iap.purchase_success").Split(' ')[0]) ? Color.LightGreen : Color.IndianRed);
        }

        spriteBatch.End();
    }

    private void BuildButtonsIfNeeded()
    {
        if (_graphics == null || _buttons.Count == _products.Count) return;

        _buttons.Clear();
        int itemWidth = 240;
        int itemHeight = 220;
        int gap = 24;
        int totalWidth = _products.Count * itemWidth + (_products.Count - 1) * gap;
        int startX = (_graphics.Viewport.Width - totalWidth) / 2;
        int startY = 180;

        for (int i = 0; i < _products.Count; i++)
        {
            var rect = new Rectangle(startX + i * (itemWidth + gap), startY, itemWidth, itemHeight);
            _buttons.Add(new IAPProductButton(_products[i], rect));
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class IAPProductButton
    {
        public IAPProduct Product { get; }
        public Rectangle Bounds { get; }
        public bool IsHovered { get; set; }

        public IAPProductButton(IAPProduct product, Rectangle bounds)
        {
            Product = product;
            Bounds = bounds;
        }
    }
}

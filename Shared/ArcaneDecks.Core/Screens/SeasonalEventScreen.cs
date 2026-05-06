using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class SeasonalEventScreen : IScreen
{
    private readonly ScreenManager _screenManager;
    private readonly ILocalizationService _localization;
    private readonly CardSystem _cardSystem;
    private readonly CombatSystem _combatSystem;
    private readonly RunManager _runManager;
    private readonly List<EnemyTemplate> _enemyTemplates;
    private readonly List<SeasonalEventDto> _events;

    private SpriteFont? _font;
    private GraphicsDevice? _graphics;
    private Texture2D? _pixel;
    private InputState _previousInput;

    private readonly List<EventButton> _buttons = new();
    private readonly List<ClaimButton> _claimButtons = new();
    private Rectangle _backRect;
    private bool _backHovered;

    private readonly Dictionary<string, bool> _claimResults = new();

    public SeasonalEventScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates, List<SeasonalEventDto> events)
    {
        _screenManager = screenManager;
        _localization = localization;
        _cardSystem = cardSystem;
        _combatSystem = combatSystem;
        _runManager = runManager;
        _enemyTemplates = enemyTemplates;
        _events = events;
    }

    public void Initialize() { }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphics = graphicsDevice;
        _font = contentManager.Load<SpriteFont>("Font");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        BuildLayout();
    }

    public void UnloadContent()
    {
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
        _buttons.Clear();
        _claimButtons.Clear();
    }

    public void Show()
    {
        _screenManager.AnalyticsService?.TrackEvent("screen_view", new Dictionary<string, object> { ["screen"] = "seasonal_events" });
    }

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

        foreach (var btn in _claimButtons)
        {
            btn.IsHovered = btn.Bounds.Contains(input.MousePosition);
            if (input.MouseLeftReleased && btn.IsHovered && !_previousInput.MouseLeftPressed)
            {
                btn.OnClick();
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
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(20, 12, 40));

        // Title
        var titleText = _localization.Get("ui.seasonal.title");
        var titleSize = _font.MeasureString(titleText);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.5f) / 2, 40);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        if (_events.Count == 0)
        {
            var noText = _localization.Get("ui.seasonal.no_events");
            var noSize = _font.MeasureString(noText);
            var noPos = new Vector2((_graphics.Viewport.Width - noSize.X) / 2, _graphics.Viewport.Height / 2 - 20);
            spriteBatch.DrawString(_font, noText, noPos, Color.Gray);
        }
        else
        {
            // Draw each event panel
            for (int i = 0; i < _events.Count; i++)
            {
                DrawEventPanel(spriteBatch, i, _events[i]);
            }
        }

        // Back button
        var backColor = _backHovered ? new Color(70, 50, 120) : new Color(45, 30, 80);
        spriteBatch.Draw(_pixel, _backRect, backColor);
        DrawBorder(spriteBatch, _backRect, new Color(100, 80, 150), 2);

        var backText = _localization.Get("ui.seasonal.back");
        var backSize = _font.MeasureString(backText);
        var backPos = new Vector2(
            _backRect.X + (_backRect.Width - backSize.X) / 2,
            _backRect.Y + (_backRect.Height - backSize.Y) / 2);
        spriteBatch.DrawString(_font, backText, backPos, Color.White);

        spriteBatch.End();
    }

    private void DrawEventPanel(SpriteBatch spriteBatch, int index, SeasonalEventDto ev)
    {
        if (_font == null || _pixel == null || _graphics == null) return;

        int panelWidth = 520;
        int panelHeight = 160;
        int startY = 110 + index * (panelHeight + 20);
        int centerX = _graphics.Viewport.Width / 2;
        var panelRect = new Rectangle(centerX - panelWidth / 2, startY, panelWidth, panelHeight);

        // Panel background
        spriteBatch.Draw(_pixel, panelRect, new Color(35, 22, 60));
        DrawBorder(spriteBatch, panelRect, new Color(80, 60, 120), 2);

        // Event name
        var namePos = new Vector2(panelRect.X + 20, panelRect.Y + 14);
        spriteBatch.DrawString(_font, ev.Name, namePos, Color.Gold, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);

        // Description
        var descPos = new Vector2(panelRect.X + 20, panelRect.Y + 42);
        spriteBatch.DrawString(_font, ev.Description, descPos, new Color(180, 170, 200), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        // Ends at
        var endsText = string.Format(_localization.Get("ui.seasonal.ends_at"), ev.EndAt);
        var endsPos = new Vector2(panelRect.X + 20, panelRect.Y + 66);
        spriteBatch.DrawString(_font, endsText, endsPos, new Color(150, 140, 170), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        // Reward
        var rewardText = string.Format(_localization.Get("ui.seasonal.reward_teeth"), ev.RewardTeeth);
        if (!string.IsNullOrEmpty(ev.RewardCardId))
        {
            var card = _cardSystem.GetCard(ev.RewardCardId);
            var cardName = card != null ? _localization.Get(card.NameKey) : ev.RewardCardId;
            rewardText += $"  |  {string.Format(_localization.Get("ui.seasonal.reward_card"), cardName)}";
        }
        var rewardPos = new Vector2(panelRect.X + 20, panelRect.Y + 88);
        spriteBatch.DrawString(_font, rewardText, rewardPos, new Color(200, 180, 100), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        // Best score if available
        /* if (_playerStats.TryGetValue(ev.Id, out var stats) && stats.RunsCompleted > 0)
        {
            var statsText = $"Best: {stats.BestScore} (Floor {stats.BestFloor}) — Runs: {stats.RunsCompleted}";
            var statsPos = new Vector2(panelRect.X + 20, panelRect.Y + 110);
            spriteBatch.DrawString(_font, statsText, statsPos, new Color(120, 200, 120), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        } */

        // Join button
        var joinBtn = _buttons.Find(b => b.Tag == ev.Id);
        if (joinBtn != null)
        {
            var joinColor = joinBtn.IsHovered ? new Color(60, 120, 60) : new Color(40, 90, 40);
            spriteBatch.Draw(_pixel, joinBtn.Bounds, joinColor);
            DrawBorder(spriteBatch, joinBtn.Bounds, new Color(80, 160, 80), 2);
            var joinText = _localization.Get("ui.seasonal.join");
            var joinSize = _font.MeasureString(joinText);
            var joinPos = new Vector2(
                joinBtn.Bounds.X + (joinBtn.Bounds.Width - joinSize.X) / 2,
                joinBtn.Bounds.Y + (joinBtn.Bounds.Height - joinSize.Y) / 2);
            spriteBatch.DrawString(_font, joinText, joinPos, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        // Claim button
        var claimBtn = _claimButtons.Find(b => b.Tag == ev.Id);
        if (claimBtn != null && !_claimResults.ContainsKey(ev.Id))
        {
            var claimColor = claimBtn.IsHovered ? new Color(120, 100, 40) : new Color(90, 75, 30);
            spriteBatch.Draw(_pixel, claimBtn.Bounds, claimColor);
            DrawBorder(spriteBatch, claimBtn.Bounds, new Color(160, 140, 60), 2);
            var claimText = $"Claim {ev.RewardTeeth}";
            var claimSize = _font.MeasureString(claimText);
            var claimPos = new Vector2(
                claimBtn.Bounds.X + (claimBtn.Bounds.Width - claimSize.X) / 2,
                claimBtn.Bounds.Y + (claimBtn.Bounds.Height - claimSize.Y) / 2);
            spriteBatch.DrawString(_font, claimText, claimPos, Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }
        else if (_claimResults.TryGetValue(ev.Id, out var claimed) && claimed)
        {
            var claimedText = "Claimed";
            var claimedSize = _font.MeasureString(claimedText);
            var claimedPos = new Vector2(panelRect.X + panelRect.Width - 110, panelRect.Y + panelRect.Height - 36);
            spriteBatch.DrawString(_font, claimedText, claimedPos, new Color(120, 200, 120), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }
    }

    private void BuildLayout()
    {
        if (_graphics == null) return;

        int panelWidth = 520;
        int btnWidth = 100;
        int btnHeight = 36;
        int centerX = _graphics.Viewport.Width / 2;

        _buttons.Clear();
        _claimButtons.Clear();

        for (int i = 0; i < _events.Count; i++)
        {
            int startY = 110 + i * (160 + 20);
            var ev = _events[i];
            var joinRect = new Rectangle(centerX + panelWidth / 2 - btnWidth - 20, startY + 110, btnWidth, btnHeight);
            _buttons.Add(new EventButton(ev.Id, joinRect, () =>
            {
                _screenManager.AnalyticsService?.TrackEvent("run_started", new Dictionary<string, object> { ["type"] = "seasonal", ["event_key"] = ev.EventKey });
                _runManager.StartSeasonalRun(ev.Id, ev.EventKey, ev.Rules);
                _screenManager.ChangeScreen(new BattleScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
            }));

            var claimRect = new Rectangle(centerX + panelWidth / 2 - btnWidth * 2 - 30, startY + 110, btnWidth, btnHeight);
            _claimButtons.Add(new ClaimButton(ev.Id, claimRect, () =>
            {
                _ = Task.Run(async () =>
                {
                    if (_screenManager.BackendService == null) return;
                    var result = await _screenManager.BackendService.ClaimSeasonalEventRewardAsync(ev.Id);
                    if (result?.Success == true)
                    {
                        _claimResults[ev.Id] = true;
                        if (_screenManager.MetaProgressionSystem != null)
                        {
                            _screenManager.MetaProgressionSystem.Data.GoblinTeeth += result.RewardTeeth;
                        }
                        // Card reward can be applied here when unlock system is added
                    }
                });
            }));
        }

        _backRect = new Rectangle(centerX - 100, _graphics.Viewport.Height - 80, 200, 50);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class EventButton
    {
        public string Tag { get; }
        public Rectangle Bounds { get; }
        public Action OnClick { get; }
        public bool IsHovered { get; set; }

        public EventButton(string tag, Rectangle bounds, Action onClick)
        {
            Tag = tag;
            Bounds = bounds;
            OnClick = onClick;
        }
    }

    private class ClaimButton
    {
        public string Tag { get; }
        public Rectangle Bounds { get; }
        public Action OnClick { get; }
        public bool IsHovered { get; set; }

        public ClaimButton(string tag, Rectangle bounds, Action onClick)
        {
            Tag = tag;
            Bounds = bounds;
            OnClick = onClick;
        }
    }
}

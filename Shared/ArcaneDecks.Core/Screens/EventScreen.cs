using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class EventScreen : IScreen
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

    private readonly EventData _eventData;
    private readonly List<EventChoiceButton> _buttons = new();

    public EventScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, RunManager runManager, List<EnemyTemplate> enemyTemplates)
    {
        _screenManager = screenManager;
        _localization = localization;
        _cardSystem = cardSystem;
        _combatSystem = combatSystem;
        _runManager = runManager;
        _enemyTemplates = enemyTemplates;
        _eventData = PickRandomEvent();
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
                ApplyOutcome(btn.Outcome);
                ContinueToNextFloor();
            }
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null || _pixel == null) return;

        spriteBatch.Begin();

        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(20, 20, 35));

        var title = _localization.Get(_eventData.TitleKey);
        var titleSize = _font.MeasureString(title);
        var titlePos = new Vector2((_graphics.Viewport.Width - titleSize.X * 1.4f) / 2, 100);
        spriteBatch.DrawString(_font, title, titlePos, Color.Gold, 0f, Vector2.Zero, 1.4f, SpriteEffects.None, 0f);

        var desc = _localization.Get(_eventData.DescriptionKey);
        var descSize = _font.MeasureString(desc);
        float descScale = descSize.X > _graphics.Viewport.Width * 0.8f ? 0.9f : 1.0f;
        var descPos = new Vector2((_graphics.Viewport.Width - descSize.X * descScale) / 2, titlePos.Y + 70);
        spriteBatch.DrawString(_font, desc, descPos, Color.LightGray, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);

        foreach (var btn in _buttons)
        {
            var bgColor = btn.IsHovered ? new Color(60, 80, 60) : new Color(40, 55, 40);
            spriteBatch.Draw(_pixel, btn.Bounds, bgColor);
            DrawBorder(spriteBatch, btn.Bounds, btn.IsHovered ? new Color(100, 140, 100) : new Color(70, 100, 70), 2);

            var textSize = _font.MeasureString(btn.Text);
            var textPos = new Vector2(
                btn.Bounds.X + (btn.Bounds.Width - textSize.X) / 2,
                btn.Bounds.Y + (btn.Bounds.Height - textSize.Y) / 2);
            spriteBatch.DrawString(_font, btn.Text, textPos, Color.White);
        }

        spriteBatch.End();
    }

    private void BuildButtons()
    {
        if (_graphics == null) return;

        int btnWidth = 340;
        int btnHeight = 60;
        int spacing = 20;
        int startY = 340;
        int centerX = _graphics.Viewport.Width / 2;

        _buttons.Clear();
        for (int i = 0; i < _eventData.Choices.Count; i++)
        {
            var choice = _eventData.Choices[i];
            var rect = new Rectangle(centerX - btnWidth / 2, startY + i * (btnHeight + spacing), btnWidth, btnHeight);
            _buttons.Add(new EventChoiceButton(_localization.Get(choice.LabelKey), rect, choice.Outcome));
        }
    }

    private void ApplyOutcome(EventOutcome outcome)
    {
        switch (outcome.Type)
        {
            case EventOutcomeType.Heal:
                _runManager.HealPlayer(outcome.Value);
                break;
            case EventOutcomeType.Damage:
                _runManager.DamagePlayer(outcome.Value);
                break;
            case EventOutcomeType.GainGold:
                _runManager.State.Gold += outcome.Value;
                break;
            case EventOutcomeType.AddCard:
                if (!string.IsNullOrEmpty(outcome.CardId))
                    _runManager.AddCardToDeck(outcome.CardId);
                break;
            case EventOutcomeType.RemoveCard:
                if (!string.IsNullOrEmpty(outcome.CardId))
                    _runManager.RemoveCardFromDeck(outcome.CardId);
                break;
        }
    }

    private void ContinueToNextFloor()
    {
        _runManager.AdvanceFloor();
        var nextType = _runManager.GetCurrentFloorType();

        if (nextType == FloorType.Combat || nextType == FloorType.Boss)
            _screenManager.ChangeScreen(new BattleScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        else if (nextType == FloorType.Shop)
            _screenManager.ChangeScreen(new ShopScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
        else
            _screenManager.ChangeScreen(new EventScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, _enemyTemplates));
    }

    private EventData PickRandomEvent()
    {
        var events = new List<EventData>
        {
            new()
            {
                TitleKey = "event.mysterious_shrine.title",
                DescriptionKey = "event.mysterious_shrine.desc",
                Choices = new()
                {
                    new EventChoice("event.mysterious_shrine.choice_pray", new EventOutcome(EventOutcomeType.Heal, 10)),
                    new EventChoice("event.mysterious_shrine.choice_steal", new EventOutcome(EventOutcomeType.GainGold, 25))
                }
            },
            new()
            {
                TitleKey = "event.goblin_merchant.title",
                DescriptionKey = "event.goblin_merchant.desc",
                Choices = new()
                {
                    new EventChoice("event.goblin_merchant.choice_buy", new EventOutcome(EventOutcomeType.RemoveCard, 0, cardId:"defend")),
                    new EventChoice("event.goblin_merchant.choice_ignore", new EventOutcome(EventOutcomeType.GainGold, 5))
                }
            },
            new()
            {
                TitleKey = "event.cursed_altar.title",
                DescriptionKey = "event.cursed_altar.desc",
                Choices = new()
                {
                    new EventChoice("event.cursed_altar.choice_sacrifice", new EventOutcome(EventOutcomeType.Damage, 8)),
                    new EventChoice("event.cursed_altar.choice_refuse", new EventOutcome(EventOutcomeType.Heal, 3))
                }
            },
            new()
            {
                TitleKey = "event.rune_cache.title",
                DescriptionKey = "event.rune_cache.desc",
                Choices = new()
                {
                    new EventChoice("event.rune_cache.choice_take", new EventOutcome(EventOutcomeType.AddCard, 0, cardId:"strike")),
                    new EventChoice("event.rune_cache.choice_sell", new EventOutcome(EventOutcomeType.GainGold, 15))
                }
            }
        };

        var random = new Random();
        return events[random.Next(events.Count)];
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private class EventData
    {
        public string TitleKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public List<EventChoice> Choices { get; set; } = new();
    }

    private class EventChoice
    {
        public string LabelKey { get; set; } = "";
        public EventOutcome Outcome { get; set; }

        public EventChoice(string labelKey, EventOutcome outcome)
        {
            LabelKey = labelKey;
            Outcome = outcome;
        }
    }

    private class EventOutcome
    {
        public EventOutcomeType Type { get; set; }
        public int Value { get; set; }
        public string? CardId { get; set; }

        public EventOutcome(EventOutcomeType type, int value, string? cardId = null)
        {
            Type = type;
            Value = value;
            CardId = cardId;
        }
    }

    private enum EventOutcomeType { Heal, Damage, GainGold, AddCard, RemoveCard }

    private class EventChoiceButton
    {
        public string Text { get; }
        public Rectangle Bounds { get; }
        public EventOutcome Outcome { get; }
        public bool IsHovered { get; set; }

        public EventChoiceButton(string text, Rectangle bounds, EventOutcome outcome)
        {
            Text = text;
            Bounds = bounds;
            Outcome = outcome;
        }
    }
}

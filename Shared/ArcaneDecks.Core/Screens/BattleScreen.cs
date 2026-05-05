using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Screens;

public class BattleScreen : IScreen
{
    private readonly ScreenManager _screenManager;
    private readonly ILocalizationService _localization;
    private readonly CardSystem _cardSystem;
    private readonly CombatSystem _combatSystem;
    private readonly List<EnemyTemplate> _enemyTemplates;

    private SpriteFont? _font;
    private GraphicsDevice? _graphics;
    private Texture2D? _pixel;
    private InputState _previousInput;

    private CombatEntity? _player;
    private CombatEntity? _enemy;
    private readonly List<CardDefinition> _hand = new();
    private bool _combatOver;
    private string _resultText = "";

    private readonly List<Rectangle> _cardRects = new();
    private Rectangle _endTurnRect;
    private int _hoveredCardIndex = -1;
    private bool _endTurnHovered;

    public BattleScreen(ScreenManager screenManager, ILocalizationService localization, CardSystem cardSystem, CombatSystem combatSystem, List<EnemyTemplate> enemyTemplates)
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

        _combatOver = false;
        _resultText = "";
        _hoveredCardIndex = -1;

        _player = new CombatEntity
        {
            NameKey = "entity.player.name",
            MaxHealth = 50,
            CurrentHealth = 50,
            IsPlayer = true,
        };

        var template = _enemyTemplates.Count > 0
            ? _enemyTemplates[new Random().Next(_enemyTemplates.Count)]
            : new EnemyTemplate { Id = "goblin_grunt", NameKey = "entity.goblin_grunt.name", MaxHealth = 30, Damage = 7, Armor = 0 };

        _enemy = new CombatEntity
        {
            NameKey = template.NameKey,
            MaxHealth = template.MaxHealth,
            CurrentHealth = template.MaxHealth,
            Damage = template.Damage,
            Armor = template.Armor,
        };

        _combatSystem.StartCombat(_player, new[] { _enemy });
        _combatSystem.OnCombatEnded += OnCombatEnded;
        _combatSystem.OnEntityDied += OnEntityDied;

        _hand.Clear();
        var random = new Random();
        var allCards = _cardSystem.GetAllCards().ToList();
        for (int i = 0; i < 5 && allCards.Count > 0; i++)
        {
            var card = allCards[random.Next(allCards.Count)];
            _hand.Add(card);
        }

        LayoutCards();
        _endTurnRect = new Rectangle(graphicsDevice.Viewport.Width - 170, graphicsDevice.Viewport.Height - 120, 140, 50);
    }

    public void UnloadContent()
    {
        _combatSystem.OnCombatEnded -= OnCombatEnded;
        _combatSystem.OnEntityDied -= OnEntityDied;
        _pixel?.Dispose();
        _pixel = null;
        _font = null;
        _graphics = null;
    }

    public void Show() { }
    public void Hide() { }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_graphics == null || _combatSystem.Player == null) return;

        if (_combatOver)
        {
            if (input.MouseLeftReleased && !_previousInput.MouseLeftPressed)
            {
                _screenManager.ChangeScreen(new MainMenuScreen(_screenManager, _localization, _cardSystem, _combatSystem, _enemyTemplates));
            }
            _previousInput = input;
            return;
        }

        _hoveredCardIndex = -1;
        for (int i = 0; i < _cardRects.Count; i++)
        {
            if (_cardRects[i].Contains(input.MousePosition))
            {
                _hoveredCardIndex = i;
                if (input.MouseLeftReleased && !_previousInput.MouseLeftPressed && _combatSystem.IsPlayerTurn)
                {
                    var card = _hand[i];
                    var target = _combatSystem.GetAliveEnemies().FirstOrDefault();
                    if (target != null && _combatSystem.PlayCard(card, target))
                    {
                        _hand.RemoveAt(i);
                        _cardRects.RemoveAt(i);
                        LayoutCards();
                        break;
                    }
                }
            }
        }

        _endTurnHovered = _endTurnRect.Contains(input.MousePosition);
        if (_endTurnHovered && input.MouseLeftReleased && !_previousInput.MouseLeftPressed && _combatSystem.IsPlayerTurn)
        {
            _combatSystem.EndPlayerTurn();
        }

        _previousInput = input;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_graphics == null || _font == null || _pixel == null || _player == null) return;

        spriteBatch.Begin();

        // Background
        spriteBatch.Draw(_pixel, _graphics.Viewport.Bounds, new Color(35, 50, 35));

        // Player panel (top left)
        DrawEntityPanel(spriteBatch, _player, 20, 20);

        // Enemy panel (top right)
        if (_enemy != null && !_enemy.IsDead)
        {
            DrawEntityPanel(spriteBatch, _enemy, _graphics.Viewport.Width - 220, 20);
        }

        // Turn indicator
        var turnText = _combatSystem.IsPlayerTurn
            ? _localization.Get("ui.combat.player_turn")
            : _localization.Get("ui.combat.enemy_turn");
        var turnSize = _font.MeasureString(turnText);
        var turnPos = new Vector2((_graphics.Viewport.Width - turnSize.X) / 2, 30);
        spriteBatch.DrawString(_font, turnText, turnPos, _combatSystem.IsPlayerTurn ? Color.LightGreen : Color.IndianRed);

        // Cards
        for (int i = 0; i < _hand.Count && i < _cardRects.Count; i++)
        {
            var rect = _cardRects[i];
            var card = _hand[i];
            bool hovered = i == _hoveredCardIndex && _combatSystem.IsPlayerTurn;
            var cardBg = hovered ? new Color(60, 80, 60) : new Color(40, 55, 40);
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
            DrawBorder(spriteBatch, rect, borderColor, 3);

            // Cost circle (top right)
            var costRect = new Rectangle(rect.Right - 30, rect.Y + 8, 22, 22);
            spriteBatch.Draw(_pixel, costRect, Color.DarkSlateBlue);
            var costText = card.Cost.ToString();
            var costSize = _font.MeasureString(costText);
            spriteBatch.DrawString(_font, costText, new Vector2(costRect.X + (22 - costSize.X) / 2, costRect.Y + (22 - costSize.Y) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // Name
            var nameText = _localization.Get(card.NameKey);
            var nameSize = _font.MeasureString(nameText);
            var namePos = new Vector2(rect.X + (rect.Width - nameSize.X * 0.9f) / 2, rect.Y + 40);
            spriteBatch.DrawString(_font, nameText, namePos, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            // Description
            var effectArgs = card.Effects.Select(e => (object)e.Value).ToArray();
            var descText = _localization.Get(card.DescriptionKey, effectArgs);
            var descSize = _font.MeasureString(descText);
            var descPos = new Vector2(rect.X + (rect.Width - descSize.X * 0.75f) / 2, rect.Y + 100);
            spriteBatch.DrawString(_font, descText, descPos, new Color(200, 200, 200), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }

        // End Turn button
        if (_combatSystem.IsPlayerTurn && !_combatOver)
        {
            var btnColor = _endTurnHovered ? new Color(60, 140, 60) : new Color(40, 100, 40);
            spriteBatch.Draw(_pixel, _endTurnRect, btnColor);
            DrawBorder(spriteBatch, _endTurnRect, Color.LightGreen, 2);
            var endText = _localization.Get("ui.combat.end_turn");
            var endSize = _font.MeasureString(endText);
            var endPos = new Vector2(_endTurnRect.X + (_endTurnRect.Width - endSize.X) / 2, _endTurnRect.Y + (_endTurnRect.Height - endSize.Y) / 2);
            spriteBatch.DrawString(_font, endText, endPos, Color.White);
        }

        // Combat result overlay
        if (_combatOver)
        {
            var overlay = new Rectangle(0, 0, _graphics.Viewport.Width, _graphics.Viewport.Height);
            spriteBatch.Draw(_pixel, overlay, new Color(0, 0, 0, 160));

            var resultSize = _font.MeasureString(_resultText);
            var resultPos = new Vector2((_graphics.Viewport.Width - resultSize.X * 2f) / 2, (_graphics.Viewport.Height - resultSize.Y * 2f) / 2);
            var resultColor = _resultText.Contains("Victory") || _resultText.Contains("Zafer") ? Color.Gold : Color.IndianRed;
            spriteBatch.DrawString(_font, _resultText, resultPos, resultColor, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);

            var hint = _localization.CurrentLanguage == "tr" ? "Devam etmek için tıkla" : "Click to continue";
            var hintSize = _font.MeasureString(hint);
            var hintPos = new Vector2((_graphics.Viewport.Width - hintSize.X) / 2, resultPos.Y + 80);
            spriteBatch.DrawString(_font, hint, hintPos, Color.LightGray);
        }

        spriteBatch.End();
    }

    private void DrawEntityPanel(SpriteBatch spriteBatch, CombatEntity entity, int x, int y)
    {
        var panelRect = new Rectangle(x, y, 200, 100);
        spriteBatch.Draw(_pixel!, panelRect, new Color(30, 30, 45));
        DrawBorder(spriteBatch, panelRect, new Color(80, 80, 100), 2);

        var nameText = _localization.Get(entity.NameKey);
        spriteBatch.DrawString(_font!, nameText, new Vector2(x + 10, y + 8), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        var hpText = $"HP: {entity.CurrentHealth}/{entity.MaxHealth}";
        spriteBatch.DrawString(_font!, hpText, new Vector2(x + 10, y + 38), Color.LightGreen);

        if (entity.Armor > 0)
        {
            var armorText = $"Armor: {entity.Armor}";
            spriteBatch.DrawString(_font!, armorText, new Vector2(x + 10, y + 58), new Color(160, 160, 220));
        }

        if (entity.IsPlayer)
        {
            var energyText = $"Energy: {_combatSystem.CurrentEnergy}/{_combatSystem.MaxEnergy}";
            spriteBatch.DrawString(_font!, energyText, new Vector2(x + 10, y + 78), new Color(220, 200, 100));
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel!, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void LayoutCards()
    {
        if (_graphics == null) return;
        int cardWidth = 140;
        int cardHeight = 180;
        int gap = 20;
        int totalWidth = _hand.Count * cardWidth + (_hand.Count - 1) * gap;
        int startX = (_graphics.Viewport.Width - totalWidth) / 2;
        int y = _graphics.Viewport.Height - 220;

        _cardRects.Clear();
        for (int i = 0; i < _hand.Count; i++)
        {
            _cardRects.Add(new Rectangle(startX + i * (cardWidth + gap), y, cardWidth, cardHeight));
        }
    }

    private static int GetEffectValue(CardDefinition card, EffectType type)
    {
        return card.Effects.FirstOrDefault(e => e.Type == type)?.Value ?? 0;
    }

    private void OnCombatEnded()
    {
        _combatOver = true;
        if (_combatSystem.Player != null && !_combatSystem.Player.IsDead)
        {
            _resultText = _localization.Get("ui.combat.win");
        }
        else
        {
            _resultText = _localization.Get("ui.combat.lose");
        }
    }

    private void OnEntityDied(CombatEntity entity)
    {
        if (_combatSystem.IsCombatOver())
        {
            _combatSystem.EndPlayerTurn(); // triggers OnCombatEnded via ProcessEnemyTurn
        }
    }
}

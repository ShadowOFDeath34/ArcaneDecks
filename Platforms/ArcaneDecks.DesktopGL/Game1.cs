using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;
using ArcaneDecks.Core.Screens;

namespace ArcaneDecks.DesktopGL;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private JsonLocalizationService _localization = null!;
    private CardSystem _cardSystem = null!;
    private CombatSystem _combatSystem = null!;
    private ScreenManager _screenManager = null!;
    private GameDataLoader _dataLoader = null!;

    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var basePath = Path.Combine(AppContext.BaseDirectory, "Localization");
        _localization = new JsonLocalizationService(basePath);

        _cardSystem = new CardSystem();
        _combatSystem = new CombatSystem();

        _dataLoader = new GameDataLoader(Path.Combine(AppContext.BaseDirectory, "Data"));
        foreach (var card in _dataLoader.LoadCards())
            _cardSystem.RegisterCard(card);

        var enemyTemplates = _dataLoader.LoadEnemies();

        _screenManager = new ScreenManager(GraphicsDevice, Content);
        _screenManager.ChangeScreen(new MainMenuScreen(_screenManager, _localization, _cardSystem, _combatSystem, enemyTemplates));
    }


    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        var input = new InputState(
            MouseLeftPressed: mouse.LeftButton == ButtonState.Pressed,
            MouseLeftReleased: _previousMouse.LeftButton == ButtonState.Pressed && mouse.LeftButton == ButtonState.Released,
            MousePosition: new Point(mouse.X, mouse.Y),
            KeyEscapePressed: keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape)
        );

        _screenManager.Update(gameTime, input);

        _previousMouse = mouse;
        _previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _screenManager.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ArcaneDecks.Core.Services;
using ArcaneDecks.Core.Systems;
using ArcaneDecks.Core.Screens;
using Sentry;

namespace ArcaneDecks.DesktopGL;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private JsonLocalizationService _localization = null!;
    private CardSystem _cardSystem = null!;
    private CombatSystem _combatSystem = null!;
    private RunManager _runManager = null!;
    private ScreenManager _screenManager = null!;
    private GameDataLoader _dataLoader = null!;
    private HttpAuthService _authService = null!;
    private HttpBackendService _backendService = null!;

    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;

        var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
        if (!string.IsNullOrEmpty(sentryDsn))
        {
            SentrySdk.Init(o =>
            {
                o.Dsn = sentryDsn;
                o.Release = "arcane-decks-desktop@1.0.0";
                o.Environment = Environment.GetEnvironmentVariable("ARCANE_ENV") ?? "production";
            });
        }

        Exiting += (_, _) => SentrySdk.Close();
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
        _runManager = new RunManager(_cardSystem);

        var metaProgression = new MetaProgressionSystem();
        _runManager.MetaProgressionSystem = metaProgression;

        _dataLoader = new GameDataLoader(Path.Combine(AppContext.BaseDirectory, "Data"));
        foreach (var card in _dataLoader.LoadCards())
            _cardSystem.RegisterCard(card);

        var enemyTemplates = _dataLoader.LoadEnemies();

        var apiBaseUrl = Environment.GetEnvironmentVariable("ARCANE_API_URL") ?? "http://localhost:3000";
        var httpClient = new HttpClient();
        var storagePath = Path.Combine(AppContext.BaseDirectory, "Save");

        _authService = new HttpAuthService(httpClient, apiBaseUrl);
        _authService.EnsureDeviceId(storagePath);
        _authService.LoadLocalCredentials(storagePath);

        _backendService = new HttpBackendService(httpClient, _authService, apiBaseUrl);

        var postHogKey = Environment.GetEnvironmentVariable("POSTHOG_API_KEY");
        var analyticsService = new PostHogAnalyticsService(postHogKey ?? string.Empty, _authService.DeviceId);

        var adService = new DesktopAdService();
        adService.Initialize();

        var iapService = new DesktopIAPService();
        iapService.Initialize();

        var steamService = new SteamworksService();
        steamService.Initialize();

        _screenManager = new ScreenManager(GraphicsDevice, Content)
        {
            AuthService = _authService,
            BackendService = _backendService,
            AdService = adService,
            IAPService = iapService,
            SteamService = steamService,
            AnalyticsService = analyticsService,
            MetaProgressionSystem = metaProgression
        };

        _ = Task.Run(async () =>
        {
            if (!_authService.IsAuthenticated)
                await _authService.AuthenticateAsync();
            _authService.SaveLocalCredentials(storagePath);

            var cloudState = await _backendService.LoadProgressAsync();
            if (cloudState != null)
            {
                _runManager.RestoreState(cloudState);
            }

            var metaDto = await _backendService.LoadMetaProgressAsync();
            if (metaDto != null)
            {
                metaProgression.LoadFromDto(metaDto);
            }
        });

        _screenManager.ChangeScreen(new MainMenuScreen(_screenManager, _localization, _cardSystem, _combatSystem, _runManager, enemyTemplates));
    }


    protected override void Update(GameTime gameTime)
    {
        try
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
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            throw;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _screenManager.Draw(gameTime, _spriteBatch);
            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            throw;
        }
    }

}

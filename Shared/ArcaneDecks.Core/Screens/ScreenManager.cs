using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.Core.Screens;

public class ScreenManager
{
    private IScreen? _currentScreen;
    private IScreen? _nextScreen;

    public GraphicsDevice GraphicsDevice { get; }
    public ContentManager Content { get; }
    public IAuthService? AuthService { get; set; }
    public IBackendService? BackendService { get; set; }

    public ScreenManager(GraphicsDevice graphicsDevice, ContentManager content)
    {
        GraphicsDevice = graphicsDevice;
        Content = content;
    }

    public void ChangeScreen(IScreen screen)
    {
        _nextScreen = screen;
    }

    public void Update(GameTime gameTime, InputState input)
    {
        if (_nextScreen != null)
        {
            _currentScreen?.Hide();
            _currentScreen?.UnloadContent();

            _currentScreen = _nextScreen;
            _nextScreen = null;

            _currentScreen.Initialize();
            _currentScreen.LoadContent(GraphicsDevice, Content);
            _currentScreen.Show();
        }

        _currentScreen?.Update(gameTime, input);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _currentScreen?.Draw(gameTime, spriteBatch);
    }
}

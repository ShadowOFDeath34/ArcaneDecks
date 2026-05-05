using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace ArcaneDecks.Core.Screens;

public interface IScreen
{
    void Initialize();
    void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager);
    void UnloadContent();
    void Update(GameTime gameTime, InputState input);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    void Show();
    void Hide();
}

public readonly record struct InputState(
    bool MouseLeftPressed,
    bool MouseLeftReleased,
    Point MousePosition,
    bool KeyEscapePressed
);

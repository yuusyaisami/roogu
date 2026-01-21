using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    Texture2D _player;
    Vector2 _pos = new(100, 100);


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _player = Content.Load<Texture2D>("slime"); // 拡張子不要
    }


    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float speed = 200f; // 1秒に200px

        Vector2 dir = Vector2.Zero;
        if (kb.IsKeyDown(Keys.Left)  || kb.IsKeyDown(Keys.A)) dir.X -= 1;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) dir.X += 1;
        if (kb.IsKeyDown(Keys.Up)    || kb.IsKeyDown(Keys.W)) dir.Y -= 1;
        if (kb.IsKeyDown(Keys.Down)  || kb.IsKeyDown(Keys.S)) dir.Y += 1;

        if (dir != Vector2.Zero)
            dir.Normalize();

        _pos += dir * speed * dt;

        base.Update(gameTime);
    }


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend
        );

        _spriteBatch.Draw(_player, _pos, null, Color.White, 0f, Vector2.Zero, 5.0f, SpriteEffects.None, 0f);
        _spriteBatch.End();


        base.Draw(gameTime);
    }
}

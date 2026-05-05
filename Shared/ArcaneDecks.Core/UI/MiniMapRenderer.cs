using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.UI;

public static class MiniMapRenderer
{
    private const int NodeSize = 18;
    private const int NodeSpacing = 4;
    private const int BorderThickness = 2;

    private static Color GetFloorColor(FloorType type, bool isPast, bool isCurrent, bool isFuture)
    {
        Color baseColor = type switch
        {
            FloorType.Combat => new Color(180, 60, 60),
            FloorType.Shop => new Color(220, 190, 60),
            FloorType.Boss => new Color(140, 60, 180),
            FloorType.Event => new Color(60, 180, 110),
            _ => Color.Gray,
        };

        if (isCurrent)
            return new Color(
                Math.Min(255, baseColor.R + 40),
                Math.Min(255, baseColor.G + 40),
                Math.Min(255, baseColor.B + 40));

        if (isPast)
            return new Color(baseColor.R / 2, baseColor.G / 2, baseColor.B / 2);

        if (isFuture)
            return new Color(baseColor.R / 4, baseColor.G / 4, baseColor.B / 4);

        return baseColor;
    }

    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, RunManager runManager, Vector2 position)
    {
        var floorPlan = runManager.State.FloorPlan;
        int current = runManager.State.CurrentFloor;

        for (int i = 0; i < floorPlan.Count; i++)
        {
            int x = (int)position.X + i * (NodeSize + NodeSpacing);
            int y = (int)position.Y;
            var rect = new Rectangle(x, y, NodeSize, NodeSize);

            bool isPast = i < current - 1;
            bool isCurrent = i == current - 1;
            bool isFuture = i > current - 1;

            var color = GetFloorColor(floorPlan[i], isPast, isCurrent, isFuture);
            spriteBatch.Draw(pixel, rect, color);

            if (isCurrent)
            {
                DrawBorder(spriteBatch, pixel, rect, Color.White, BorderThickness);
            }
            else
            {
                DrawBorder(spriteBatch, pixel, rect, new Color(60, 60, 60), 1);
            }
        }
    }

    public static Vector2 MeasureSize(int floorCount)
    {
        int width = floorCount * NodeSize + (floorCount - 1) * NodeSpacing;
        int height = NodeSize;
        return new Vector2(width, height);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }
}

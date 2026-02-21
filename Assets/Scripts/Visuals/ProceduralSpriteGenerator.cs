using UnityEngine;
using System.Collections.Generic;
using Deadlight.Data;

namespace Deadlight.Visuals
{
    public static class ProceduralSpriteGenerator
    {
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        #region Color Palettes
        
        public static class Palette
        {
            public static readonly Color SkinLight = new Color(0.93f, 0.75f, 0.6f);
            public static readonly Color SkinDark = new Color(0.78f, 0.58f, 0.44f);
            public static readonly Color ZombieSkin = new Color(0.45f, 0.55f, 0.4f);
            public static readonly Color ZombieSkinDark = new Color(0.35f, 0.42f, 0.32f);
            public static readonly Color Blood = new Color(0.6f, 0.1f, 0.1f);
            public static readonly Color BloodBright = new Color(0.8f, 0.15f, 0.1f);
            
            public static readonly Color ClothDark = new Color(0.2f, 0.22f, 0.25f);
            public static readonly Color ClothMid = new Color(0.35f, 0.38f, 0.4f);
            public static readonly Color ClothLight = new Color(0.5f, 0.52f, 0.55f);
            public static readonly Color MilitaryGreen = new Color(0.3f, 0.4f, 0.25f);
            public static readonly Color MilitaryBrown = new Color(0.45f, 0.35f, 0.25f);
            
            public static readonly Color RunnerRed = new Color(0.7f, 0.3f, 0.3f);
            public static readonly Color ExploderGreen = new Color(0.5f, 0.7f, 0.2f);
            public static readonly Color ExploderYellow = new Color(0.8f, 0.75f, 0.2f);
            public static readonly Color TankGray = new Color(0.4f, 0.42f, 0.45f);
            
            public static readonly Color GrassGreen = new Color(0.3f, 0.5f, 0.25f);
            public static readonly Color GrassDark = new Color(0.22f, 0.38f, 0.18f);
            public static readonly Color DirtBrown = new Color(0.45f, 0.35f, 0.25f);
            public static readonly Color Concrete = new Color(0.5f, 0.5f, 0.52f);
            public static readonly Color Asphalt = new Color(0.25f, 0.25f, 0.28f);
            
            public static readonly Color WoodLight = new Color(0.6f, 0.45f, 0.3f);
            public static readonly Color WoodDark = new Color(0.4f, 0.28f, 0.18f);
            public static readonly Color MetalLight = new Color(0.6f, 0.62f, 0.65f);
            public static readonly Color MetalDark = new Color(0.35f, 0.37f, 0.4f);
            public static readonly Color Rust = new Color(0.5f, 0.3f, 0.2f);
            
            public static readonly Color WindowBlue = new Color(0.4f, 0.5f, 0.7f);
            public static readonly Color WindowGlow = new Color(0.9f, 0.85f, 0.6f);
            public static readonly Color RoofRed = new Color(0.55f, 0.25f, 0.2f);
            public static readonly Color BrickRed = new Color(0.6f, 0.35f, 0.3f);
        }
        
        #endregion

        #region Player Sprites
        
        public static Sprite CreatePlayerSprite(int direction, int frame = 0)
        {
            string key = $"player_{direction}_{frame}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 32;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            switch (direction)
            {
                case 0: DrawPlayerDown(texture, frame); break;
                case 1: DrawPlayerUp(texture, frame); break;
                case 2: DrawPlayerLeft(texture, frame); break;
                case 3: DrawPlayerRight(texture, frame); break;
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        private static void DrawPlayerDown(Texture2D tex, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            
            DrawRect(tex, 12, 4 + bobOffset, 8, 10, Palette.MilitaryGreen);
            DrawRect(tex, 13, 5 + bobOffset, 6, 8, Palette.MilitaryBrown);
            
            DrawRect(tex, 9, 6 + bobOffset, 3, 6, Palette.MilitaryGreen);
            DrawRect(tex, 20, 6 + bobOffset, 3, 6, Palette.MilitaryGreen);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 12, 1, 3, 4, Palette.ClothDark);
            DrawRect(tex, 17 + legOffset, 1, 3, 4, Palette.ClothDark);
            
            DrawRect(tex, 13, 14 + bobOffset, 6, 6, Palette.SkinLight);
            DrawRect(tex, 14, 15 + bobOffset, 4, 4, Palette.SkinDark);
            
            DrawPixel(tex, 14, 17 + bobOffset, Color.black);
            DrawPixel(tex, 17, 17 + bobOffset, Color.black);
            
            DrawRect(tex, 11, 20 + bobOffset, 2, 3, Palette.SkinLight);
        }

        private static void DrawPlayerUp(Texture2D tex, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            
            DrawRect(tex, 12, 4 + bobOffset, 8, 10, Palette.MilitaryGreen);
            DrawRect(tex, 13, 5 + bobOffset, 6, 8, Palette.MilitaryBrown);
            
            DrawRect(tex, 9, 6 + bobOffset, 3, 6, Palette.MilitaryGreen);
            DrawRect(tex, 20, 6 + bobOffset, 3, 6, Palette.MilitaryGreen);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 12 + legOffset, 1, 3, 4, Palette.ClothDark);
            DrawRect(tex, 17, 1, 3, 4, Palette.ClothDark);
            
            DrawRect(tex, 13, 14 + bobOffset, 6, 6, Palette.SkinDark);
            
            DrawRect(tex, 11, 20 + bobOffset, 2, 3, Palette.SkinLight);
        }

        private static void DrawPlayerLeft(Texture2D tex, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            
            DrawRect(tex, 13, 4 + bobOffset, 6, 10, Palette.MilitaryGreen);
            DrawRect(tex, 14, 5 + bobOffset, 4, 8, Palette.MilitaryBrown);
            
            DrawRect(tex, 10, 7 + bobOffset, 3, 5, Palette.MilitaryGreen);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 13 + legOffset, 1, 3, 4, Palette.ClothDark);
            DrawRect(tex, 16, 1, 3, 4, Palette.ClothDark);
            
            DrawRect(tex, 13, 14 + bobOffset, 5, 6, Palette.SkinLight);
            DrawPixel(tex, 14, 17 + bobOffset, Color.black);
            
            DrawRect(tex, 9, 8 + bobOffset, 2, 3, Palette.SkinLight);
        }

        private static void DrawPlayerRight(Texture2D tex, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            
            DrawRect(tex, 13, 4 + bobOffset, 6, 10, Palette.MilitaryGreen);
            DrawRect(tex, 14, 5 + bobOffset, 4, 8, Palette.MilitaryBrown);
            
            DrawRect(tex, 19, 7 + bobOffset, 3, 5, Palette.MilitaryGreen);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 13, 1, 3, 4, Palette.ClothDark);
            DrawRect(tex, 16 - legOffset, 1, 3, 4, Palette.ClothDark);
            
            DrawRect(tex, 14, 14 + bobOffset, 5, 6, Palette.SkinLight);
            DrawPixel(tex, 17, 17 + bobOffset, Color.black);
            
            DrawRect(tex, 21, 8 + bobOffset, 2, 3, Palette.SkinLight);
        }

        #endregion

        #region Zombie Sprites

        public enum ZombieType { Basic, Runner, Exploder, Tank }

        public static Sprite CreateZombieSprite(ZombieType type, int direction, int frame = 0)
        {
            string key = $"zombie_{type}_{direction}_{frame}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = type == ZombieType.Tank ? 48 : 32;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            switch (type)
            {
                case ZombieType.Basic: DrawBasicZombie(texture, direction, frame); break;
                case ZombieType.Runner: DrawRunnerZombie(texture, direction, frame); break;
                case ZombieType.Exploder: DrawExploderZombie(texture, direction, frame); break;
                case ZombieType.Tank: DrawTankZombie(texture, direction, frame); break;
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        private static void DrawBasicZombie(Texture2D tex, int direction, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            int armOffset = (frame % 2 == 0) ? 0 : 1;
            
            DrawRect(tex, 12, 3 + bobOffset, 8, 10, Palette.ClothDark);
            DrawRect(tex, 14, 4 + bobOffset, 4, 3, Palette.Blood);
            
            DrawRect(tex, 8 - armOffset, 5 + bobOffset, 4, 5, Palette.ZombieSkin);
            DrawRect(tex, 20 + armOffset, 5 + bobOffset, 4, 5, Palette.ZombieSkin);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 12 + legOffset, 0, 3, 4, Palette.ClothMid);
            DrawRect(tex, 17, 0, 3, 4, Palette.ClothMid);
            
            DrawRect(tex, 12, 13 + bobOffset, 8, 7, Palette.ZombieSkin);
            DrawRect(tex, 13, 14 + bobOffset, 6, 5, Palette.ZombieSkinDark);
            
            if (direction == 0)
            {
                DrawPixel(tex, 14, 17 + bobOffset, Palette.Blood);
                DrawPixel(tex, 17, 17 + bobOffset, Palette.Blood);
                DrawRect(tex, 14, 14 + bobOffset, 4, 2, Palette.Blood);
            }
        }

        private static void DrawRunnerZombie(Texture2D tex, int direction, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 2 : 0;
            int leanOffset = 2;
            
            DrawRect(tex, 10 + leanOffset, 5 + bobOffset, 7, 8, Palette.ClothDark);
            DrawRect(tex, 11 + leanOffset, 6 + bobOffset, 5, 3, Palette.RunnerRed);
            
            DrawRect(tex, 6 + leanOffset, 7 + bobOffset, 4, 4, Palette.ZombieSkin);
            DrawRect(tex, 17 + leanOffset, 6 + bobOffset, 5, 3, Palette.ZombieSkin);
            
            int legSpread = (frame % 2 == 0) ? 2 : 0;
            DrawRect(tex, 10 - legSpread, 1, 3, 5, Palette.ClothMid);
            DrawRect(tex, 17 + legSpread, 1, 3, 5, Palette.ClothMid);
            
            DrawRect(tex, 11 + leanOffset, 13 + bobOffset, 6, 6, Palette.ZombieSkin);
            
            if (direction == 0)
            {
                DrawPixel(tex, 13 + leanOffset, 16 + bobOffset, new Color(0.9f, 0.2f, 0.2f));
                DrawPixel(tex, 15 + leanOffset, 16 + bobOffset, new Color(0.9f, 0.2f, 0.2f));
            }
        }

        private static void DrawExploderZombie(Texture2D tex, int direction, int frame)
        {
            int pulseOffset = (frame % 4);
            float pulse = 1f + pulseOffset * 0.05f;
            
            int bodyW = Mathf.RoundToInt(12 * pulse);
            int bodyH = Mathf.RoundToInt(14 * pulse);
            int bodyX = 16 - bodyW / 2;
            int bodyY = 2;
            
            DrawRect(tex, bodyX, bodyY, bodyW, bodyH, Palette.ZombieSkin);
            
            Color pustuleColor = Color.Lerp(Palette.ExploderGreen, Palette.ExploderYellow, pulseOffset * 0.25f);
            DrawCircle(tex, 12, 8, 2, pustuleColor);
            DrawCircle(tex, 18, 10, 3, pustuleColor);
            DrawCircle(tex, 14, 5, 2, pustuleColor);
            DrawCircle(tex, 20, 6, 2, pustuleColor);
            
            DrawRect(tex, 8, 6, 3, 4, Palette.ZombieSkin);
            DrawRect(tex, 21, 6, 3, 4, Palette.ZombieSkin);
            
            DrawRect(tex, 12, 0, 3, 3, Palette.ClothDark);
            DrawRect(tex, 17, 0, 3, 3, Palette.ClothDark);
            
            DrawRect(tex, 12, 16, 8, 6, Palette.ZombieSkinDark);
            
            if (direction == 0)
            {
                DrawPixel(tex, 14, 19, Color.black);
                DrawPixel(tex, 17, 19, Color.black);
            }
        }

        private static void DrawTankZombie(Texture2D tex, int direction, int frame)
        {
            int bobOffset = (frame % 2 == 1) ? 1 : 0;
            
            DrawRect(tex, 14, 6 + bobOffset, 20, 18, Palette.TankGray);
            DrawRect(tex, 16, 8 + bobOffset, 16, 14, Palette.ZombieSkin);
            
            DrawRect(tex, 8, 10 + bobOffset, 6, 10, Palette.ZombieSkin);
            DrawRect(tex, 34, 10 + bobOffset, 6, 10, Palette.ZombieSkin);
            
            DrawRect(tex, 6, 12 + bobOffset, 3, 6, Palette.ZombieSkinDark);
            DrawRect(tex, 39, 12 + bobOffset, 3, 6, Palette.ZombieSkinDark);
            
            int legOffset = (frame % 2 == 0) ? 0 : 1;
            DrawRect(tex, 16 + legOffset, 1, 6, 6, Palette.ClothDark);
            DrawRect(tex, 26, 1, 6, 6, Palette.ClothDark);
            
            DrawRect(tex, 16, 24 + bobOffset, 16, 10, Palette.ZombieSkinDark);
            
            DrawRect(tex, 18, 28 + bobOffset, 4, 4, Palette.TankGray);
            DrawRect(tex, 26, 28 + bobOffset, 4, 4, Palette.TankGray);
            
            if (direction == 0)
            {
                DrawPixel(tex, 20, 30 + bobOffset, new Color(0.9f, 0.3f, 0.2f));
                DrawPixel(tex, 27, 30 + bobOffset, new Color(0.9f, 0.3f, 0.2f));
            }
        }

        #endregion

        #region Environment Sprites

        public static Sprite CreateGroundTile(int variant)
        {
            string key = $"ground_{variant}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 32;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Color baseColor, detailColor;
            switch (variant)
            {
                case 0: baseColor = Palette.GrassGreen; detailColor = Palette.GrassDark; break;
                case 1: baseColor = Palette.DirtBrown; detailColor = new Color(0.35f, 0.28f, 0.2f); break;
                case 2: baseColor = Palette.Concrete; detailColor = new Color(0.45f, 0.45f, 0.47f); break;
                case 3: baseColor = Palette.Asphalt; detailColor = new Color(0.2f, 0.2f, 0.22f); break;
                default: baseColor = Palette.GrassGreen; detailColor = Palette.GrassDark; break;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.3f + variant * 10, y * 0.3f + variant * 10);
                    Color col = Color.Lerp(baseColor, detailColor, noise * 0.5f);
                    texture.SetPixel(x, y, col);
                }
            }

            if (variant == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    int gx = Random.Range(2, size - 2);
                    int gy = Random.Range(2, size - 2);
                    texture.SetPixel(gx, gy, new Color(0.2f, 0.35f, 0.15f));
                    texture.SetPixel(gx, gy + 1, new Color(0.25f, 0.4f, 0.18f));
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateBuildingSprite(int variant)
        {
            string key = $"building_{variant}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = 64;
            int height = 80;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            Color wallColor = variant switch
            {
                0 => Palette.BrickRed,
                1 => new Color(0.7f, 0.65f, 0.55f),
                2 => Palette.Concrete,
                _ => Palette.BrickRed
            };

            DrawRect(texture, 0, 0, width, height - 12, wallColor);
            DrawRect(texture, 1, 1, width - 2, height - 14, Color.Lerp(wallColor, Color.black, 0.1f));

            DrawRect(texture, 0, height - 12, width, 12, Palette.RoofRed);
            for (int x = 0; x < width; x += 8)
            {
                DrawRect(texture, x, height - 12, 4, 12, Color.Lerp(Palette.RoofRed, Color.black, 0.15f));
            }

            int windowY = 20;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int wx = 8 + col * 18;
                    int wy = windowY + row * 22;
                    DrawRect(texture, wx, wy, 12, 16, Palette.WindowBlue);
                    DrawRect(texture, wx + 1, wy + 1, 10, 14, new Color(0.3f, 0.35f, 0.5f));
                    DrawRect(texture, wx + 6, wy, 1, 16, wallColor);
                    DrawRect(texture, wx, wy + 8, 12, 1, wallColor);
                }
            }

            DrawRect(texture, 22, 0, 20, 18, Palette.WoodDark);
            DrawRect(texture, 24, 2, 16, 14, Palette.WoodLight);
            DrawRect(texture, 31, 2, 2, 14, Palette.WoodDark);

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateTreeSprite()
        {
            string key = "tree";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 48;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            DrawRect(texture, 21, 0, 6, 20, Palette.WoodDark);
            DrawRect(texture, 22, 1, 4, 18, Palette.WoodLight);

            Color leafDark = new Color(0.15f, 0.35f, 0.12f);
            Color leafMid = new Color(0.2f, 0.45f, 0.18f);
            Color leafLight = new Color(0.28f, 0.55f, 0.22f);

            DrawCircle(texture, 24, 32, 14, leafDark);
            DrawCircle(texture, 20, 28, 10, leafMid);
            DrawCircle(texture, 28, 28, 10, leafMid);
            DrawCircle(texture, 24, 38, 8, leafLight);
            DrawCircle(texture, 18, 34, 6, leafLight);
            DrawCircle(texture, 30, 34, 6, leafLight);

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateRockSprite()
        {
            string key = "rock";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 24;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            Color rockDark = new Color(0.35f, 0.35f, 0.38f);
            Color rockMid = new Color(0.5f, 0.5f, 0.53f);
            Color rockLight = new Color(0.65f, 0.65f, 0.68f);

            DrawCircle(texture, 12, 8, 10, rockDark);
            DrawCircle(texture, 10, 10, 6, rockMid);
            DrawCircle(texture, 8, 12, 3, rockLight);

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateCrateSprite()
        {
            string key = "crate";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 24;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            DrawRect(texture, 2, 2, 20, 20, Palette.WoodDark);
            DrawRect(texture, 4, 4, 16, 16, Palette.WoodLight);
            
            DrawRect(texture, 2, 10, 20, 2, Palette.WoodDark);
            DrawRect(texture, 10, 2, 2, 20, Palette.WoodDark);
            
            DrawRect(texture, 2, 2, 4, 4, Palette.MetalDark);
            DrawRect(texture, 18, 2, 4, 4, Palette.MetalDark);
            DrawRect(texture, 2, 18, 4, 4, Palette.MetalDark);
            DrawRect(texture, 18, 18, 4, 4, Palette.MetalDark);

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateBarrelSprite(bool explosive = false)
        {
            string key = explosive ? "barrel_explosive" : "barrel";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 20;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            Color barrelColor = explosive ? new Color(0.7f, 0.2f, 0.15f) : Palette.MetalDark;
            Color barrelLight = explosive ? new Color(0.85f, 0.3f, 0.2f) : Palette.MetalLight;

            DrawRect(texture, 4, 1, 12, 18, barrelColor);
            DrawRect(texture, 6, 2, 8, 16, barrelLight);
            
            DrawRect(texture, 3, 2, 14, 2, Palette.MetalDark);
            DrawRect(texture, 3, 16, 14, 2, Palette.MetalDark);
            DrawRect(texture, 3, 9, 14, 2, Palette.MetalDark);

            if (explosive)
            {
                DrawRect(texture, 7, 6, 6, 6, Palette.ExploderYellow);
                DrawRect(texture, 9, 4, 2, 2, Palette.ExploderYellow);
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateCarSprite(int variant)
        {
            string key = $"car_{variant}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = 48;
            int height = 24;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            Color carColor = variant switch
            {
                0 => new Color(0.7f, 0.15f, 0.12f),
                1 => new Color(0.2f, 0.35f, 0.6f),
                2 => new Color(0.75f, 0.72f, 0.65f),
                _ => Palette.MetalDark
            };

            DrawRect(texture, 4, 4, 40, 16, carColor);
            DrawRect(texture, 8, 6, 32, 12, Color.Lerp(carColor, Color.black, 0.15f));
            
            DrawRect(texture, 10, 8, 12, 8, Palette.WindowBlue);
            DrawRect(texture, 26, 8, 12, 8, Palette.WindowBlue);
            
            DrawCircle(texture, 10, 4, 4, Color.black);
            DrawCircle(texture, 38, 4, 4, Color.black);
            DrawCircle(texture, 10, 4, 2, Palette.MetalDark);
            DrawCircle(texture, 38, 4, 2, Palette.MetalDark);
            
            if (variant == 3)
            {
                DrawRect(texture, 20, 12, 8, 4, Palette.Rust);
                DrawRect(texture, 6, 8, 4, 6, Palette.Rust);
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.2f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateWallSprite(bool horizontal, int length = 32)
        {
            string key = $"wall_{(horizontal ? "h" : "v")}_{length}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = horizontal ? length : 8;
            int height = horizontal ? 8 : length;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                    Color col = Color.Lerp(Palette.Concrete, new Color(0.4f, 0.4f, 0.42f), noise * 0.3f);
                    
                    if ((horizontal && (y == 0 || y == height - 1)) || (!horizontal && (x == 0 || x == width - 1)))
                    {
                        col = Color.Lerp(col, Color.black, 0.2f);
                    }
                    
                    texture.SetPixel(x, y, col);
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        #endregion

        #region Weapon and Pickup Sprites

        public static Sprite CreateWeaponSprite(string weaponType)
        {
            string key = $"weapon_{weaponType}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = 24;
            int height = 12;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            switch (weaponType.ToLower())
            {
                case "pistol":
                    DrawRect(texture, 2, 4, 16, 4, Palette.MetalDark);
                    DrawRect(texture, 6, 1, 4, 4, Palette.MetalDark);
                    DrawRect(texture, 4, 5, 12, 2, Palette.MetalLight);
                    break;
                    
                case "shotgun":
                    DrawRect(texture, 0, 5, 22, 3, Palette.MetalDark);
                    DrawRect(texture, 0, 4, 18, 1, Palette.WoodDark);
                    DrawRect(texture, 8, 2, 6, 3, Palette.MetalLight);
                    DrawRect(texture, 2, 6, 16, 1, Palette.MetalLight);
                    break;
                    
                case "rifle":
                    DrawRect(texture, 0, 4, 24, 4, Palette.MetalDark);
                    DrawRect(texture, 2, 2, 4, 3, Palette.MetalDark);
                    DrawRect(texture, 10, 1, 6, 4, Palette.MetalLight);
                    DrawRect(texture, 4, 5, 18, 2, Palette.MetalLight);
                    DrawRect(texture, 18, 6, 4, 2, Palette.WoodDark);
                    break;
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateWeaponIcon(WeaponType type)
        {
            string key = $"weaponicon_{type}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int w = 32, h = 16;
            var tex = new Texture2D(w, h);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex, Color.clear);

            switch (type)
            {
                case WeaponType.Pistol:
                    DrawRect(tex, 4, 6, 18, 4, Palette.MetalDark);
                    DrawRect(tex, 10, 2, 5, 5, Palette.MetalDark);
                    DrawRect(tex, 6, 7, 14, 2, Palette.MetalLight);
                    break;
                case WeaponType.Shotgun:
                    DrawRect(tex, 1, 7, 28, 3, Palette.MetalDark);
                    DrawRect(tex, 1, 6, 22, 1, Palette.WoodDark);
                    DrawRect(tex, 12, 4, 6, 3, Palette.MetalLight);
                    DrawRect(tex, 3, 8, 20, 1, Palette.MetalLight);
                    break;
                case WeaponType.SMG:
                    DrawRect(tex, 3, 6, 22, 4, Palette.MetalDark);
                    DrawRect(tex, 13, 3, 4, 4, Palette.MetalLight);
                    DrawRect(tex, 7, 7, 16, 2, Palette.MetalLight);
                    DrawRect(tex, 20, 4, 3, 2, Palette.MetalDark);
                    break;
                case WeaponType.AssaultRifle:
                    DrawRect(tex, 0, 6, 30, 4, Palette.MetalDark);
                    DrawRect(tex, 4, 3, 5, 4, Palette.MetalDark);
                    DrawRect(tex, 14, 2, 6, 4, Palette.MetalLight);
                    DrawRect(tex, 6, 7, 22, 2, Palette.MetalLight);
                    DrawRect(tex, 24, 8, 5, 2, Palette.WoodDark);
                    break;
                case WeaponType.SniperRifle:
                    DrawRect(tex, 0, 7, 32, 3, Palette.MetalDark);
                    DrawRect(tex, 20, 4, 4, 3, new Color(0.4f, 0.6f, 0.8f));
                    DrawRect(tex, 8, 3, 5, 4, Palette.MetalDark);
                    DrawRect(tex, 2, 8, 28, 1, Palette.MetalLight);
                    DrawRect(tex, 26, 9, 4, 2, Palette.WoodDark);
                    break;
                case WeaponType.GrenadeLauncher:
                    DrawRect(tex, 4, 5, 20, 6, Palette.MetalDark);
                    DrawRect(tex, 2, 6, 4, 4, new Color(0.4f, 0.5f, 0.3f));
                    DrawRect(tex, 8, 7, 14, 3, Palette.MetalLight);
                    DrawRect(tex, 12, 3, 6, 3, Palette.MetalDark);
                    break;
                case WeaponType.Flamethrower:
                    DrawRect(tex, 2, 5, 24, 5, Palette.MetalDark);
                    DrawRect(tex, 0, 6, 6, 3, new Color(0.6f, 0.3f, 0.2f));
                    DrawRect(tex, 20, 4, 4, 2, new Color(1f, 0.5f, 0.1f));
                    DrawRect(tex, 24, 3, 3, 3, new Color(1f, 0.3f, 0f));
                    DrawRect(tex, 8, 7, 16, 2, Palette.MetalLight);
                    break;
                case WeaponType.Railgun:
                    DrawRect(tex, 2, 5, 26, 5, new Color(0.25f, 0.3f, 0.45f));
                    DrawRect(tex, 0, 6, 4, 3, new Color(0.3f, 0.5f, 0.8f));
                    DrawRect(tex, 26, 6, 4, 3, new Color(0.4f, 0.7f, 1f));
                    DrawRect(tex, 10, 7, 18, 2, new Color(0.5f, 0.7f, 1f));
                    DrawRect(tex, 14, 3, 4, 3, new Color(0.3f, 0.5f, 0.8f));
                    break;
                default:
                    DrawRect(tex, 4, 6, 18, 4, Palette.MetalDark);
                    DrawRect(tex, 10, 2, 5, 5, Palette.MetalDark);
                    break;
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateArmorIcon(bool isHelmet)
        {
            string key = isHelmet ? "armoricon_helmet" : "armoricon_vest";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int s = 16;
            var tex = new Texture2D(s, s);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex, Color.clear);

            if (isHelmet)
            {
                for (int y = 4; y < 14; y++)
                {
                    int hw = y < 10 ? 6 : 6 - (y - 10);
                    if (hw < 2) hw = 2;
                    for (int x = 8 - hw; x < 8 + hw; x++)
                        if (x >= 0 && x < s) DrawPixel(tex, x, y, Palette.MetalLight);
                }
                DrawRect(tex, 5, 12, 6, 2, Palette.MetalDark);
            }
            else
            {
                for (int y = 2; y < 14; y++)
                {
                    float frac = y < 8 ? 1f : 1f - (y - 8f) / 8f;
                    int hw = Mathf.Max(2, Mathf.RoundToInt(6 * frac));
                    for (int x = 8 - hw; x < 8 + hw; x++)
                        if (x >= 0 && x < s) DrawPixel(tex, x, y, new Color(0.2f, 0.4f, 0.7f));
                }
                DrawRect(tex, 5, 10, 6, 2, new Color(0.15f, 0.3f, 0.6f));
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreatePickupSprite(string pickupType)
        {
            string key = $"pickup_{pickupType}";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int size = 16;
            var texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            switch (pickupType.ToLower())
            {
                case "health":
                    DrawRect(texture, 2, 2, 12, 12, Color.white);
                    DrawRect(texture, 6, 4, 4, 8, new Color(0.9f, 0.15f, 0.1f));
                    DrawRect(texture, 4, 6, 8, 4, new Color(0.9f, 0.15f, 0.1f));
                    break;
                    
                case "ammo":
                    DrawRect(texture, 3, 2, 4, 12, new Color(0.7f, 0.55f, 0.2f));
                    DrawRect(texture, 9, 2, 4, 12, new Color(0.7f, 0.55f, 0.2f));
                    DrawRect(texture, 4, 10, 2, 2, Palette.MetalLight);
                    DrawRect(texture, 10, 10, 2, 2, Palette.MetalLight);
                    break;
                    
                case "lore":
                    DrawRect(texture, 2, 1, 12, 14, new Color(0.9f, 0.88f, 0.8f));
                    DrawRect(texture, 4, 3, 8, 1, Palette.ClothDark);
                    DrawRect(texture, 4, 6, 8, 1, Palette.ClothDark);
                    DrawRect(texture, 4, 9, 6, 1, Palette.ClothDark);
                    break;
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateBulletSprite()
        {
            string key = "bullet";
            if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = 8;
            int height = 4;
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            ClearTexture(texture, Color.clear);

            DrawRect(texture, 0, 1, 6, 2, new Color(1f, 0.9f, 0.4f));
            DrawRect(texture, 6, 1, 2, 2, new Color(1f, 0.7f, 0.2f));
            texture.SetPixel(0, 1, new Color(1f, 0.95f, 0.7f));
            texture.SetPixel(0, 2, new Color(1f, 0.95f, 0.7f));

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0f, 0.5f), 32);
            spriteCache[key] = sprite;
            return sprite;
        }

        #endregion

        #region UI Sprites

        public static Sprite CreateHealthBarSprite(int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = (x == 0 || x == width - 1 || y == 0 || y == height - 1) 
                        ? Color.black : Color.white;
                    texture.SetPixel(x, y, col);
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0, 0.5f), 1);
        }

        #endregion

        #region Helper Methods

        private static void ClearTexture(Texture2D texture, Color color)
        {
            var pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height && py < texture.height; py++)
            {
                for (int px = x; px < x + width && px < texture.width; px++)
                {
                    if (px >= 0 && py >= 0)
                        texture.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                texture.SetPixel(x, y, color);
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                            texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        public static void ClearCache()
        {
            spriteCache.Clear();
        }

        #endregion
    }
}

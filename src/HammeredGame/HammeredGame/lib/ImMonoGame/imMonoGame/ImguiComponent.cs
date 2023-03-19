using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Num = System.Numerics;
using ImMonoGame.Thing;

namespace ImMonoGame.Thing
{
    public class ImguiComponent
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private Texture2D _xnaTexture;
        public IntPtr _imGuiTexture;
        private Game Game;
        public ImGuiEntity[] UIEntities;
        public float fontSize = 14f;
        public string Font = "";     
        public Theme Theme;

        public ImguiComponent(GraphicsDeviceManager graphics, Game game, ImGuiEntity[] Canvas)
        {
            this._graphics = graphics;
            this.Game = game;
            this.UIEntities = Canvas;
        }

        public void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(Game);
            if(Theme != null)
            {
                Theme.Initialize();
            }
            var io = ImGui.GetIO();
            io.Fonts.AddFontFromFileTTF(Font, fontSize);
            _imGuiRenderer.RebuildFontAtlas();
        }

        public void LoadContent()
        {
            // Texture loading example

            // First, load the texture as a Texture2D (can also be done using the XNA/FNA content pipeline)
            _xnaTexture = CreateTexture(_graphics.GraphicsDevice, 300, 150, pixel =>
            {
                var red = (pixel % 300) / 2;
                return new Color(red, 1, 1);
            });

            // Then, bind it to an ImGui-friendly pointer, that we can use during regular ImGui.** calls (see below)
            _imGuiTexture = _imGuiRenderer.BindTexture(_xnaTexture);


        }

        public void Draw(GameTime gameTime)
        {
            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            foreach (var Canvas in UIEntities)
            {
                if(Canvas != null)
                Canvas.UI();
            }

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();


        }

        private static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
        {
            //initialize a texture
            var texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (var pixel = 0; pixel < data.Length; pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            texture.SetData(data);
            return texture;
        }

        public static IntPtr texture2D_to_intPtr(Texture2D texture, ImguiComponent component)
        {
            var  _imGuiTexture = component._imGuiRenderer.BindTexture(texture);
            return _imGuiTexture;
        }

    }

}
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    public abstract class GameObject
    {
        public abstract void Update(GameTime gameTime);

        public abstract void Draw(Matrix view, Matrix projection);

        public void DrawModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D? tex)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    //effect.LightingEnabled = Keyboard.GetState().IsKeyUp(Keys.L);
                    effect.LightingEnabled = true;
                    effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                    effect.DirectionalLight0.Direction = new Vector3(1, 1, 0);
                    effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

                    if (tex != null)
                    {
                        effect.TextureEnabled = true;
                        effect.Texture = tex;
                    }
                }

                mesh.Draw();
            }
        }
    }
}

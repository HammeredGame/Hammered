using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    class Obstacle : GameObject
    {
        // Any Obstacle specific variables go here

        Input inp;
        Camera activeCamera;

        public Obstacle(Model model, Vector3 pos, float scale, Input inp, Camera cam, Texture2D t)
        {
            this.model = model;
            this.position = pos;
            this.scale = scale;
            this.rotation = Quaternion.Identity;

            this.inp = inp;
            this.activeCamera = cam;
            this.tex = t;
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            Vector3 position = this.GetPosition();
            Quaternion rotation = this.GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
            Matrix translationMatrix = Matrix.CreateTranslation(position);
            Matrix scaleMatrix = Matrix.CreateScale(scale, scale, scale);

            Matrix world = rotationMatrix * translationMatrix * scaleMatrix;

            DrawModel(model, world, view, projection, tex);
        }
    }
}


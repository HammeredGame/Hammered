using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    class WorldObject : GameObject
    {
        Input inp;
        Camera activeCamera;

        public WorldObject(Model model, Vector3 pos, float scale, Input inp, Camera cam, Texture2D t)
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

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        // TODO: Something's wrong here - this should be a function that could be common for all objects
        public override void Draw(Matrix view, Matrix projection)
        {
            Vector3 pos = this.GetPosition();
            Quaternion rot = this.GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            Matrix scaleMatrix = Matrix.CreateScale(scale, scale, scale);

            // Construct world matrix
            Matrix world = scaleMatrix * rotationMatrix * translationMatrix;

            // Given the above calculations are correct, we draw the model/mesh
            DrawModel(model, world, view, projection, tex);
        }
    }
}


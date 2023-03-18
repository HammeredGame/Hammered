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
        public Model model;

        public Vector3 _modelpos;
        public Quaternion _modelrot;
        public float scale;
        public Texture2D tex;

        private Vector3 _lightDirection = new Vector3(3, -2, 5);

        Input inp;
        Camera activeCamera;

        public WorldObject(Model model, Vector3 pos, float scale, Input inp, Camera cam, Texture2D t)
        {
            this.model = model;
            this._modelpos = pos;
            this.scale = scale;
            this._modelrot = Quaternion.Identity;

            _lightDirection.Normalize();
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
            Vector3 position = GetPosition();
            Quaternion rotation = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
            Matrix translationMatrix = Matrix.CreateTranslation(position);
            Matrix scaleMatrix = Matrix.CreateScale(scale, scale, scale);

            Matrix world = rotationMatrix * translationMatrix * scaleMatrix;

            DrawModel(model, world, view, projection, tex);
        }

        public Vector3 GetPosition()
        {
            return _modelpos;
        }

        public Quaternion GetRotation()
        {
            return _modelrot;
        }
    }
}


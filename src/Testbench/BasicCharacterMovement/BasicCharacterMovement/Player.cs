using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BasicCharacterMovement
{
    internal class Player
    {
        Model model;
        Vector3 pos;

        Matrix view;

        private Vector3 _lightDirection = new Vector3(3, -2, 5);
        private Vector3 _characterPosition = new Vector3(8, 1, -3);
        private Quaternion _characterRotation = Quaternion.Identity;

        public Player(Vector3 position)
        {
            view = Matrix.CreateLookAt(new Vector3(20, 13, -5), new Vector3(8, 0, -7), new Vector3(0, 1, 0));
            _lightDirection.Normalize();
        }

        public void loadContent(ContentManager c, Effect effect)
        {
            model = c.Load<Model>("placeholder_cube");
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = effect.Clone();
                }
            }
        }

        public void update(GameTime gameTime)
        {
            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 100.0f;

            KeyboardState keys = Keyboard.GetState();

            if (keys.IsKeyDown(Keys.W))
            {
                _characterPosition.X -= moveSpeed;
            }
            if (keys.IsKeyDown(Keys.S))
            {
                _characterPosition.X += moveSpeed;
            }
            if (keys.IsKeyDown(Keys.A))
            {
                _characterPosition.Z += moveSpeed;
            }
            if (keys.IsKeyDown(Keys.D))
            {
                _characterPosition.Z -= moveSpeed;
            }
        }

        public void draw(Matrix projection)
        {
            Matrix worldMatrix = Matrix.CreateScale(0.01f, 0.01f, 0.01f) *
                                 Matrix.CreateRotationY(MathHelper.Pi) *
                                 Matrix.CreateFromQuaternion(_characterRotation) *
                                 Matrix.CreateTranslation(_characterPosition);

            Matrix[] boneTransformations = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransformations);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Colored"];
                    currentEffect.Parameters["xWorld"].SetValue(boneTransformations[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(view);
                    currentEffect.Parameters["xProjection"].SetValue(projection);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(_lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                }
                mesh.Draw();
            }
        }
    }
}


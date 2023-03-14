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

        //Matrix view;

        private Vector3 _lightDirection = new Vector3(3, -2, 5);
        //private Vector3 _focusPoint = Vector3.Zero;
        //private Vector3 _cameraOffset = new Vector3(0f, 20f, 20f);
        private Vector3 _characterPosition = new Vector3(0, 0, 0);
        private Quaternion _characterRotation = Quaternion.Identity;
        //private Vector3 rotatedOffset;

        public Player(Vector3 position)
        {
            //rotatedOffset = Vector3.Transform(_cameraOffset, Matrix.CreateRotationY(MathHelper.PiOver2 * 0.5f));
            //view = Matrix.CreateLookAt(rotatedOffset, _focusPoint, Vector3.Up);
            //view = Matrix.CreateLookAt(new Vector3(0, -14.1759f, 10f), new Vector3(0, 0, 0), Vector3.Up);
            _lightDirection.Normalize();
        }

        public void loadContent(ContentManager c, Effect effect)
        {
            model = c.Load<Model>("placeholder_character2");
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = effect.Clone();
                }
            }
        }

        public void update(GameTime gameTime, Matrix view, Matrix projection, Viewport viewport)
        {
            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 100.0f;

            KeyboardState keys = Keyboard.GetState();

            Vector3 oldPos = _characterPosition;
            bool forwardDirty = false;

            if (keys.IsKeyDown(Keys.W))
            {
                _characterPosition.X -= moveSpeed;
                _characterPosition.Z -= moveSpeed;
                forwardDirty = true;
            }
            if (keys.IsKeyDown(Keys.S))
            {
                _characterPosition.X += moveSpeed;
                _characterPosition.Z += moveSpeed;
                forwardDirty = true;
            }
            if (keys.IsKeyDown(Keys.A))
            {
                _characterPosition.X -= moveSpeed;
                _characterPosition.Z += moveSpeed;
                forwardDirty = true;
            }
            if (keys.IsKeyDown(Keys.D))
            {
                _characterPosition.X += moveSpeed;
                _characterPosition.Z -= moveSpeed;
                forwardDirty = true;
            }

            // WASD based direction
            if (forwardDirty)
            {
                Vector3 lookDirection = _characterPosition - oldPos;
                float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                _characterRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
            }

            //// Mouse based rotation
            //MouseState mouseState = Mouse.GetState();
            //Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

            //// Clamp mouse coordinates to viewport
            //if (mousePos.X < 0) mousePos.X = 0;
            //if (mousePos.Y < 0) mousePos.Y = 0;
            //if (mousePos.X > 1280) mousePos.X = (short)1280;
            //if (mousePos.Y > 720) mousePos.Y = (short)720;

            ////bottom left corener to top right corner
            //Vector3 nearSource = new Vector3((float)mousePos.X, (float)mousePos.Y, 0.0f);
            //Vector3 farSource = new Vector3((float)mousePos.X, (float)mousePos.Y, 1.0f);

            //Vector3 nearPoint = viewport.Unproject(new Vector3(mousePos.X,
            //        mousePos.Y, 0.0f),
            //        projection,
            //        view,
            //        Matrix.Identity);

            //Vector3 farPoint = viewport.Unproject(new Vector3(mousePos.X,
            //        mousePos.Y, 1.0f),
            //        projection,
            //        view,
            //        Matrix.Identity);

            //Ray mouseRay = new Ray(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            //Plane plane = new Plane(Vector3.Up, -8.5f);

            //float denominator = Vector3.Dot(plane.Normal, mouseRay.Direction);
            //float numerator = Vector3.Dot(plane.Normal, mouseRay.Position) + plane.D;
            //float t = -(numerator / denominator);

            //Vector3 lookPosition = (nearPoint + mouseRay.Direction * t);
            //Vector3 lookDirection = lookPosition - _characterPosition;
            //float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);

            ////Vector3 charDirection = _characterPosition - nearPoint;
            //////Ray characterRay = new Ray(nearPoint, charDirection);

            ////Vector3 newDirection = direction - charDirection;
            ////float newAngle = (float)Math.Acos(Vector3.Dot(charDirection, newDirection));

            //_characterRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
        }

        public void draw(Matrix view, Matrix projection)
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


using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicCharacterMovement
{
    internal class Camera
    {
        Matrix view;

        private Vector3 _focusPoint = Vector3.Zero;
        private Vector3 _cameraOffset_default = new Vector3(0f, 20f, 20f);
        private Vector3 _cameraOffset_alt1 = new Vector3(0f, 20f, -20f);

        private void setCameraPosition(Vector3 _camOffset)
        {
            var rotatedOffset = Vector3.Transform(_camOffset, Matrix.CreateRotationY(MathHelper.PiOver2 * 0.5f));
            view = Matrix.CreateLookAt(rotatedOffset, _focusPoint, Vector3.Up);
        }

        public Camera(Vector3 position)
        {
            //var rotatedOffset = Vector3.Transform(_cameraOffset_default, Matrix.CreateRotationY(MathHelper.PiOver2 * 0.5f));
            //view = Matrix.CreateLookAt(rotatedOffset, _focusPoint, Vector3.Up);
            setCameraPosition(_cameraOffset_default);
        }

        public Matrix getViewMatrix()
        {
            return view;
        }

        public void update(GameTime gameTime)
        {
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.E))
            {
                setCameraPosition(_cameraOffset_alt1);
            }
            else if (keys.IsKeyDown(Keys.Q))
            {
                setCameraPosition(_cameraOffset_default);
            }
        }

        public void draw(Matrix projection)
        {
        }
    }
}

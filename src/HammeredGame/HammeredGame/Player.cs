using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;

namespace HammeredGame
{
    class Player : GameObject
    {
        public Model model;

        public Vector3 _characterPosition;
        public Quaternion _characterRotation;
        public float scale;
        public Texture2D tex;

        private float baseSpeed = 0.25f;
        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;
        private Vector3 _lightDirection = new Vector3(3, -2, 5);

        Input inp;
        Camera activeCamera;

        public Player(Model model, Vector3 pos, float scale, Input inp, Camera cam, Texture2D t)
        {
            this.model = model;
            this._characterPosition = pos;
            this.scale = scale;
            this._characterRotation = Quaternion.Identity;

            _lightDirection.Normalize();
            this.inp = inp;
            this.activeCamera = cam;
            this.tex = t;
        }

        public override void Update(GameTime gameTime)
        {
            Vector3 oldPos = _characterPosition;
            bool moveDirty = false;

            Vector3 forwardDirectionFromCamera = Vector3.Normalize(Vector3.Multiply(activeCamera.target - activeCamera.pos, new Vector3(1, 0, 1)));

            // Keyboard input
            if (inp.KeyDown(Keys.W))
            {
                player_vel += forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.S))
            {
                player_vel += -forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.A))
            {
                player_vel += -Vector3.Cross(forwardDirectionFromCamera, Vector3.Down); // should be up, but the game is vertically flipped right now
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.D))
            {
                player_vel += Vector3.Cross(forwardDirectionFromCamera, Vector3.Down);
                moveDirty = true;
            }

            // GamePad Control
            float MovePad_UpDown = 0;
            float MovePad_LeftRight = 0;
            if (inp.gp.IsConnected)
            {
                MovePad_LeftRight = inp.gp.ThumbSticks.Left.X;
                MovePad_UpDown = inp.gp.ThumbSticks.Left.Y;
                if ((MovePad_UpDown < -Input.DEADZONE) || (MovePad_UpDown > Input.DEADZONE) || (MovePad_LeftRight < -Input.DEADZONE) || (MovePad_LeftRight > Input.DEADZONE))
                {
                    player_vel.X = (MovePad_LeftRight * activeCamera.view.Right.X + MovePad_UpDown * activeCamera.view.Forward.X) * baseControllerSpeed; // left-right_control * right_from_camera + up-down_control * forward_from_camera
                    player_vel.Z = (MovePad_LeftRight * activeCamera.view.Right.Z + MovePad_UpDown * activeCamera.view.Forward.Z) * baseControllerSpeed; // use this formala along x and z motions for character movement
                    moveDirty = true;
                }
            }
            if (moveDirty)
            {

                // Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                // Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();
                player_vel *= baseSpeed;

                _characterPosition += player_vel;

                // At this point, also rotate the player to the direction of movement
                Vector3 lookDirection = _characterPosition - oldPos;
                float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                _characterRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
            } else
            {
                // No new keypresses or controller interactions this round, so
                // apply a gradual slowdown to any previous velocity
                player_vel.X *= 0.5f;
                player_vel.Z *= 0.5f;

                _characterPosition += player_vel;
            }

            //// Mouse based rotation (leaving this here temporarily, probably won't need this)
            #region TEMPORARY_MOUSE_BASED_ROTATION
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
            #endregion

        }

        public override void Draw(Matrix view, Matrix projection)
        {
            Vector3 position = GetPosition();
            Quaternion rotation = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
            Matrix translationMatrix = Matrix.CreateTranslation(position);
            Matrix scaleMatrix = Matrix.CreateScale(scale, 2 * scale, scale);

            Matrix world = rotationMatrix * translationMatrix * scaleMatrix;

            DrawModel(model, world, view, projection, tex);
        }

        public Vector3 GetPosition()
        {
            return _characterPosition;
        }

        public Quaternion GetRotation()
        {
            return _characterRotation;
        }
    }
}

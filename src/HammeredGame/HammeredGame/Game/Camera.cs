using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using HammeredGame.Core;

namespace HammeredGame.Game
{
    public class Camera
    {
        public const float FAR_PLANE = 500;
        public Vector3 Position, Target;                 // Camera position and target
        public Matrix ViewMatrix, ProjMatrix, ViewProjMatrix;        // View Matrix, Projection Matrix
        public Vector3 Up;                          // Vector that points up

        private Vector3 unit_direction;                     // direction of camera (normalized to distance of 1 unit)

        private Input inp;                                  // Input class for camera controls

        // Camera positions (TEMPORARY: Ideally, we get better positions AND read these in from XML)
        private Vector3 cameraPos1 = new Vector3(50f, 60f, -50f);
        private Vector3 cameraPos2 = new Vector3(-50f, 60f, -50f);
        private Vector3 cameraPos3 = new Vector3(-50f, 60f, 50f);
        private Vector3 cameraPos4 = new Vector3(50f, 60f, 50f);

        /// <summary>
        /// Initialize a new Camera object, placed at the predefined location 50,60,-50 and facing
        /// the direction of focusPoint, with the rotation determined by the upDirection.
        /// </summary>
        /// <param name="gpu"></param>
        /// <param name="focusPoint"></param>
        /// <param name="upDirection"></param>
        /// <param name="input"></param>
        public Camera(GraphicsDevice gpu, Vector3 focusPoint, Vector3 upDirection, Input input)
        {
            Up = upDirection;
            Position = cameraPos1;
            Target = focusPoint;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ProjMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, gpu.Viewport.AspectRatio, 0.1f, FAR_PLANE);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            inp = input;
            unit_direction = ViewMatrix.Forward; unit_direction.Normalize();
        }

        /// <summary>
        /// Move camera given a movement vector (Not really required for a static camera, but
        /// leaving this here in case we want any sort of moving camera functionality at any point)
        /// </summary>
        /// <param name="move"></param>
        public void MoveCamera(Vector3 move)
        {
            Position += move;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// Changes camera's target look-at position
        /// </summary>
        /// <param name="newTarget"></param>
        public void UpdateTarget(Vector3 newTarget)
        {
            Target = newTarget; //target.Y -= 10;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// TEMPORARY - needs to be modified Update the camera position and look-at Currently just
        /// switches between 4 predetermined positions given the corresponding keyboard input
        /// </summary>
        public void UpdateCamera()
        {
            #region TEMPORARY_CAMERA_CONTROLS
            if (inp.KeyDown(Keys.D1))
            {
                Position = cameraPos1;
            }
            if (inp.KeyDown(Keys.D2))
            {
                Position = cameraPos2;
            }
            if (inp.KeyDown(Keys.D3))
            {
                Position = cameraPos3;
            }
            if (inp.KeyDown(Keys.D4))
            {
                Position = cameraPos4;
            }
            #endregion

            UpdateTarget(Target); // UpdateTarget(level.pos);
        }
    }
}

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    internal class Camera
    {
        public const float CAM_HEIGHT_OFFSET = 80f;

        public const float FAR_PLANE = 500;
        public Vector3 pos, target;                 // Camera position and target
        public Matrix view, proj, view_proj;        // View Matrix, Projection Matrix
        public Vector3 up;                          // Vector that points up
        float current_angle;                        // player relative angle offset of camera (is this needed for us?)
        float angle_velocity;                       // Speed of above rotation
        float radius = 100.0f;                      // Distance of camera from player
        Vector3 unit_direction;                     // direction of camera (normalized to distance of 1 unit)

        Input inp;                                  // Input class for camera controls

        private Vector3 _focusPoint = Vector3.Zero;
        private Vector3 _cameraOffset_default = new Vector3(50f, -60f, -50f);
        private Vector3 _cameraOffset_alt1 = new Vector3(-50f, -50f, -30f);
        private Vector3 _cameraOffset_alt2 = new Vector3(-30f, -50f, 60f);
        private Vector3 _cameraOffset_alt3 = new Vector3(50f, -50f, 30f);

        public Camera(GraphicsDevice gpu, Vector3 upDirection, Input input)
        {
            up = upDirection;
            pos = _cameraOffset_default;
            target = _focusPoint;
            view = Matrix.CreateLookAt(pos, target, up);
            proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, gpu.Viewport.AspectRatio, 0.1f, FAR_PLANE);
            view_proj = view * proj;
            inp = input;
            unit_direction = view.Forward; unit_direction.Normalize();

            //setCameraPosition(_cameraOffset_default);
        }

        // Temporary
        public void MoveCamera(Vector3 move)
        {
            pos += move;
            view = Matrix.CreateLookAt(pos, target, up);
            view_proj = view * proj;
        }

        public void UpdateTarget(Vector3 newTarget)
        {
            target = newTarget; //target.Y -= 10;
            view = Matrix.CreateLookAt(pos, target, up);
            view_proj = view * proj;
        }

        // TEMPORARY - needs to be modified
        public void UpdateCamera()
        {
            #region TEMPORARY_CAMERA_CONTROLS
            if (inp.KeyDown(Keys.D1))
            {
                pos = _cameraOffset_default;
            }
            if (inp.KeyDown(Keys.D2))
            {
                pos = _cameraOffset_alt1;
            }
            if (inp.KeyDown(Keys.D3))
            {
                pos = _cameraOffset_alt2;
            }
            if (inp.KeyDown(Keys.D4))
            {
                pos = _cameraOffset_alt3;
            }
            #endregion

            UpdateTarget(_focusPoint); // UpdateTarget(level.pos);
        }
    }
}
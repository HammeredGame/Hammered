using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace HammeredGame.Game
{
    public class Camera : IImGui
    {
        public const float FAR_PLANE = 500;
        public Vector3 Position, Target;                 // Camera position and target
        public Matrix ViewMatrix, ProjMatrix, ViewProjMatrix;        // View Matrix, Projection Matrix
        public Vector3 Up;                          // Vector that points up

        // Core game services for access to the GPU aspect ratio and Input handlers
        private GameServices services;

        private Vector3 unit_direction;                     // direction of camera (normalized to distance of 1 unit)

        // Camera positions
        public Vector3[] StaticPositions = new Vector3[4]
        {
            new Vector3(300f, 200f, -300f),
            new Vector3(-300f, 200f, -300f),
            new Vector3(-300f, 200f, 300f),
            new Vector3(300f, 200f, 300f)
        };

        private int currentCameraPosIndex = 0;

        // Camera follow direction for follow mode
        private float followDistance = 49f;
        private float followAngle = 0.551f;
        private float fieldOfView = MathHelper.PiOver4;

        private Vector2 followDir = new Vector2(1, -1);

        public enum CameraMode
        {
            FourPointStatic,
            Follow
        }

        public CameraMode Mode;

        /// <summary>
        /// Initialize a new Camera object, placed at the predefined location 50,60,-50 and facing
        /// the direction of focusPoint, with the rotation determined by the upDirection.
        /// </summary>
        /// <param name="gpu"></param>
        /// <param name="focusPoint"></param>
        /// <param name="upDirection"></param>
        /// <param name="input"></param>
        public Camera(GameServices services, Vector3 focusPoint, Vector3 upDirection)
        {
            Up = upDirection;
            Position = StaticPositions[0];
            Target = focusPoint;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ProjMatrix = Matrix.CreatePerspectiveFieldOfView(fieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            unit_direction = ViewMatrix.Forward; unit_direction.Normalize();

            Mode = CameraMode.FourPointStatic;

            this.services = services;
        }

        /// <summary>
        /// Update the position and target of the camera to follow the player from a certain
        /// isometric direction specified by followDir2D, which is one of (1, 1), (-1, 1), (-1, -1),
        /// or (1, -1). This is multiplied onto the X and Z values of the camera position.
        /// </summary>
        /// <param name="player">Player to follow</param>
        /// <param name="followDir2D">One of the four diagonal directions</param>
        public void FollowPlayer(Player player, Vector2 followDir2D)
        {
            // Calculate the base follow offset position (from the player) using the followAngle,
            // between 0 (horizon) and 90 (top down)
            var sinCos = Math.SinCos(followAngle);
            Vector3 followOffset = new Vector3((float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos), (float)sinCos.Sin, (float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos));

            // Multiply by the diagonal direction
            followOffset.X *= followDir2D.X;
            followOffset.Z *= followDir2D.Y;

            Vector3 newPosition = player.Position + Vector3.Normalize(followOffset) * followDistance;
            UpdatePositionTarget(newPosition, player.Position);
        }

        /// <summary>
        /// Move camera to a new location, smoothly
        /// </summary>
        /// <param name="newPosition"></param>
        public void UpdatePosition(Vector3 newPosition)
        {
            Position = (newPosition - this.Position) / 10.0f + this.Position;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// Changes camera's target look-at position, smoothly
        /// </summary>
        /// <param name="newTarget"></param>
        public void UpdateTarget(Vector3 newTarget)
        {
            Target = (newTarget - this.Target) / 10.0f + this.Target;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// Move the camera to a new location and update its look-at position, smoothly. This exists
        /// as a convenience instead of calling UpdatePosition and UpdateTarget, which would
        /// calculate the view and projection matrices twice unnecessarily.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="newTarget"></param>
        public void UpdatePositionTarget(Vector3 newPosition, Vector3 newTarget)
        {
            Position = (newPosition - this.Position) / 10.0f + this.Position;
            Target = (newTarget - this.Target) / 10.0f + this.Target;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// TEMPORARY - needs to be modified Update the camera position and look-at Currently just
        /// switches between 4 predetermined positions given the corresponding keyboard input
        /// </summary>
        public void UpdateCamera(Player player)
        {
            if (Mode == CameraMode.Follow)
            {
                // In the case of follow mode, we modify the 2D vector that is multiplied onto the
                // base camera offset. This controls the 4 isometric directions that the camera can take.

                #region TEMPORARY_CAMERA_CONTROLS

                Input inp = services.GetService<Input>();
                if (inp.ButtonPress(Buttons.DPadUp) || inp.KeyDown(Keys.D1))
                {
                    followDir = new Vector2(1, -1);
                }
                if (inp.ButtonPress(Buttons.DPadLeft) || inp.KeyDown(Keys.D2))
                {
                    followDir = new Vector2(-1, -1);
                }
                if (inp.ButtonPress(Buttons.DPadDown) || inp.KeyDown(Keys.D3))
                {
                    followDir = new Vector2(-1, 1);
                }
                if (inp.ButtonPress(Buttons.DPadRight) || inp.KeyDown(Keys.D4))
                {
                    followDir = new Vector2(1, 1);
                }

                FollowPlayer(player, followDir);

                #endregion TEMPORARY_CAMERA_CONTROLS
            }
            else
            {
                // In static camera mode, we use the input to select the static camera.

                #region TEMPORARY_CAMERA_CONTROLS

                Input inp = services.GetService<Input>();
                if (inp.ButtonPress(Buttons.DPadUp) || inp.KeyDown(Keys.D1))
                {
                    currentCameraPosIndex = 0;
                }
                if (inp.ButtonPress(Buttons.DPadLeft) || inp.KeyDown(Keys.D2))
                {
                    currentCameraPosIndex = 1;
                }
                if (inp.ButtonPress(Buttons.DPadDown) || inp.KeyDown(Keys.D3))
                {
                    currentCameraPosIndex = 2;
                }
                if (inp.ButtonPress(Buttons.DPadRight) || inp.KeyDown(Keys.D4))
                {
                    currentCameraPosIndex = 3;
                }

                #endregion TEMPORARY_CAMERA_CONTROLS

                UpdatePositionTarget(StaticPositions[currentCameraPosIndex], Vector3.Zero);
            }
        }

        public void UI()
        {
            ImGui.Begin("Camera");
            ImGui.Text($"Camera Coordinates: {Position}");
            ImGui.Text($"Camera Focus: {Target}");

            ImGui.DragFloat("Field of View (rad): ", ref fieldOfView, 0.01f, 0f, MathHelper.Pi);
            ProjMatrix = Matrix.CreatePerspectiveFieldOfView(fieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);

            bool isStatic = Mode == CameraMode.FourPointStatic;
            if (ImGui.Checkbox("Static Camera Mode", ref isStatic))
            {
                Mode = isStatic ? CameraMode.FourPointStatic : CameraMode.Follow;
            }
            if (isStatic)
            {
                ImGui.SliderInt("Active Camera", ref currentCameraPosIndex, 0, 3);

                // imgui accepts system.numerics.vector3 and not XNA.vector3 so temporarily convert
                System.Numerics.Vector3 pos1 = StaticPositions[currentCameraPosIndex].ToNumerics();
                ImGui.DragFloat3($"Position for Camera {currentCameraPosIndex}", ref pos1);
                // other-way around can work implicitly
                StaticPositions[currentCameraPosIndex] = pos1;
            }
            else
            {
                ImGui.DragFloat("Follow Offset", ref followDistance);
                ImGui.DragFloat("Follow Angle", ref followAngle, 0.01f, 0, MathHelper.Pi / 2.0f);
            }
            ImGui.End();
        }
    }
}

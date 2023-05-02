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
        public const float FAR_PLANE = 1000;
        public Vector3 Position, Target;                 // Camera position and target
        public Matrix ViewMatrix, ProjMatrix, ViewProjMatrix;        // View Matrix, Projection Matrix
        public Vector3 Up;                          // Vector that points up

        // Core game services for access to the GPU aspect ratio and Input handlers
        private GameServices services;

        private Vector3 unit_direction;                     // direction of camera (normalized to distance of 1 unit)

        // Which of the 4 directions the camera is pointing to
        public int CurrentCameraDirIndex = 0;

        // Default camera positions
        public Vector3[] StaticPositions = new Vector3[4]
        {
            new Vector3(300f, 200f, -300f),
            new Vector3(-300f, 200f, -300f),
            new Vector3(-300f, 200f, 300f),
            new Vector3(300f, 200f, 300f)
        };

        // Camera follow properties, like distance, angle, and what to follow
        private GameObject followTarget;
        public float FollowDistance = 49f;
        public float FollowAngle = 0.551f;
        private Vector2 followDir2D = new Vector2(1, -1);

        public float FieldOfView = MathHelper.PiOver4;

        public enum CameraMode
        {
            FourPointStatic,
            Follow
        }

        public CameraMode Mode;

        public event EventHandler OnRotateLeft;
        public event EventHandler OnRotateRight;

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
            ProjMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            unit_direction = ViewMatrix.Forward; unit_direction.Normalize();

            Mode = CameraMode.FourPointStatic;

            this.services = services;
        }

        /// <summary>
        /// Set the game object for the camera to follow.
        /// </summary>
        /// <param name="target"></param>
        public void SetFollowTarget(GameObject target)
        {
            followTarget = target;
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
            var sinCos = Math.SinCos(FollowAngle);
            Vector3 followOffset = new Vector3((float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos), (float)sinCos.Sin, (float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos));

            // Multiply by the diagonal direction
            followOffset.X *= followDir2D.X;
            followOffset.Z *= followDir2D.Y;

            Vector3 newPosition = player.Position + Vector3.Normalize(followOffset) * FollowDistance;
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
            Position = Vector3.Lerp(this.Position, newPosition, 0.1f);
            Target = (newTarget - this.Target) / 10.0f + this.Target;
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
        }

        /// <summary>
        /// TEMPORARY - needs to be modified Update the camera position and look-at Currently just
        /// switches between 4 predetermined positions given the corresponding keyboard input
        /// </summary>
        public void UpdateCamera(bool isPaused)
        {
            if (Mode == CameraMode.Follow && followTarget != null && !isPaused)
            {
                // In player-follow camera mode, we use the input to rotate between four indices for
                // camera positions.
                if (UserAction.RotateCameraLeft.Pressed(services.GetService<Input>()))
                {
                    CurrentCameraDirIndex = (CurrentCameraDirIndex + 3) % 4;
                    OnRotateLeft?.Invoke(this, null);
                } else if (UserAction.RotateCameraRight.Pressed(services.GetService<Input>()))
                {
                    CurrentCameraDirIndex = (CurrentCameraDirIndex + 1) % 4;
                    OnRotateRight?.Invoke(this, null);
                }

                // A 2D vector will be multiplied onto the base camera offset. We find this from the
                // index. The 2D camera directions are (1,1)(-1,1)(-1,-1)(1,-1) for indices 0 1 2 3
                // respectively. So we can use this to calculate the 2D unit direction from the index.
                followDir2D = new Vector2(
                    1f - 2f * (float)Convert.ToInt32(CurrentCameraDirIndex % 3 != 0),
                    1f - 2f * (float)Convert.ToInt32(CurrentCameraDirIndex < 2)
                );

                // Calculate the base follow offset position (from the follow target) using the
                // followAngle, between 0 (horizon) and 90 (top down)
                var sinCos = Math.SinCos(FollowAngle);
                Vector3 followOffset = new Vector3((float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos), (float)sinCos.Sin, (float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos));

                // Multiply by the diagonal direction
                followOffset.X *= followDir2D.X;
                followOffset.Z *= followDir2D.Y;

                Vector3 newPosition = followTarget.Position + Vector3.Normalize(followOffset) * FollowDistance;

                ProjMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);

                UpdatePositionTarget(newPosition, followTarget.Position);
            }
            else if (Mode == CameraMode.FourPointStatic && !isPaused)
            {
                // In static camera mode, we use the input to select the static camera.

                if (UserAction.RotateCameraLeft.Pressed(services.GetService<Input>()))
                {
                    CurrentCameraDirIndex = (CurrentCameraDirIndex + 3) % 4;
                    OnRotateLeft?.Invoke(this, null);
                }
                else if (UserAction.RotateCameraRight.Pressed(services.GetService<Input>()))
                {
                    CurrentCameraDirIndex = (CurrentCameraDirIndex + 1) % 4;
                    OnRotateRight?.Invoke(this, null);
                }

                ProjMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);

                UpdatePositionTarget(StaticPositions[CurrentCameraDirIndex], Vector3.Zero);
            } else if (followTarget != null) {
                // In paused mode, make the camera really up close. We use const values here and
                // don't update the camera fields. This way we can reset to the previous values easily.
                const float tempFollowDistance = 23f;
                const float tempFollowAngle = 0.411f;
                const float tempFieldOfView = 0.965f;

                // Calculate the base follow offset position (from the follow target) using the
                // followAngle, between 0 (horizon) and 90 (top down)
                var sinCos = Math.SinCos(tempFollowAngle);
                Vector3 followOffset = new Vector3((float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos), (float)sinCos.Sin, (float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos));

                // Multiply by the diagonal direction
                followOffset.X *= followDir2D.X;
                followOffset.Z *= followDir2D.Y;

                Vector3 newPosition = followTarget.Position + Vector3.Normalize(followOffset) * tempFollowDistance;

                // Find which direction to offset the camera by so it fits in the right half of the
                // paused screen
                Vector3 unitScreenLeft = Vector3.Normalize(Vector3.Cross(followOffset, Vector3.UnitY));

                // Tween the projection matrix
                Matrix targetProjMatrix = Matrix.CreatePerspectiveFieldOfView(tempFieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);
                ProjMatrix = (targetProjMatrix - ProjMatrix) / 10f + ProjMatrix;

                UpdatePositionTarget(newPosition, followTarget.Position + unitScreenLeft * 5);
            }
        }

        public void UI()
        {
            ImGui.Begin("Camera");
            ImGui.Text($"Camera Coordinates: {Position}");
            ImGui.Text($"Camera Focus: {Target}");

            ImGui.DragFloat("Field of View (rad): ", ref FieldOfView, 0.01f, 0.01f, MathHelper.Pi - 0.01f);

            bool isStatic = Mode == CameraMode.FourPointStatic;
            if (ImGui.Checkbox("Static Camera Mode", ref isStatic))
            {
                Mode = isStatic ? CameraMode.FourPointStatic : CameraMode.Follow;
            }
            ImGui.SliderInt("Active Camera", ref CurrentCameraDirIndex, 0, 3);
            if (isStatic)
            {
                // imgui accepts system.numerics.vector3 and not XNA.vector3 so temporarily convert
                System.Numerics.Vector3 pos1 = StaticPositions[CurrentCameraDirIndex].ToNumerics();
                ImGui.DragFloat3($"Position for Camera {CurrentCameraDirIndex}", ref pos1);
                // other-way around can work implicitly
                StaticPositions[CurrentCameraDirIndex] = pos1;
            }
            else
            {
                ImGui.DragFloat("Follow Offset", ref FollowDistance);
                ImGui.DragFloat("Follow Angle", ref FollowAngle, 0.01f, 0, MathHelper.Pi / 2.0f);
            }
            ImGui.End();
        }
    }
}

﻿using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HammeredGame.Game
{
    public class Camera : IImGui
    {
        public const float FAR_PLANE = 1000;
        public Vector3 Position, Target;                 // Camera position and target
        public Matrix ViewMatrix, ProjMatrix, ViewProjMatrix;        // View Matrix, Projection Matrix
        public Vector3 Up;                          // Vector that points up

        public BoundingFrustum Frustum; // The view frustum

        // Core game services for access to the GPU aspect ratio and Input handlers
        private GameServices services;

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
        public float FollowAngleVertical = 0.551f;
        public float FollowAngleHorizontal = 0f;
        private Vector2 followDir2D = Vector2.UnitX;

        public float FieldOfView = MathHelper.PiOver4;

        // Camera shake properties, with a lot of help from https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/CameraShake_4_0

        // Whether the camera is currently shaking and the shake offset should be recalculated
        private bool isShaking = false;

        // The time since the camera started shaking, used to compare against shakeDuration, which
        // is the time in seconds that the camera should shake for
        private float shakeTimer;
        private float shakeDuration;

        // The maximum magnitude of the camera shake. Camera shakes gradually decrease as time goes on.
        private float shakeMagnitude;

        // The current offset of the camera due to shaking, which should be added to the position
        // and target when calculating matrices. This is zero when there is no shake going on.
        private Vector3 shakeOffset = Vector3.Zero;

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
            Frustum = new BoundingFrustum(ViewProjMatrix);

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
            var sinCos = Math.SinCos(FollowAngleVertical);
            Vector3 followOffset = new Vector3((float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos), (float)sinCos.Sin, (float)(Math.Cos(Math.PI / 4.0f) * sinCos.Cos));

            // Multiply by the diagonal direction
            followOffset.X *= followDir2D.X;
            followOffset.Z *= followDir2D.Y;

            Vector3 newPosition = player.Position + Vector3.Normalize(followOffset) * FollowDistance;
            UpdatePositionTarget(newPosition, player.Position);
        }

        /// <summary>
        /// Shakes the camera with a specific magnitude and duration.
        /// </summary>
        /// <param name="magnitude">The largest magnitude to apply to the shake.</param>
        /// <param name="duration">The length of time (in seconds) for which the shake should occur.</param>
        public void Shake(float magnitude, float duration)
        {
            // We're now shaking
            isShaking = true;

            // Store our magnitude and duration
            shakeMagnitude = magnitude;
            shakeDuration = duration;

            // Reset our timer
            shakeTimer = 0f;
        }

        /// <summary>
        /// Move camera to a new location, smoothly
        /// </summary>
        /// <param name="newPosition"></param>
        public void UpdatePosition(Vector3 newPosition)
        {
            Position = (newPosition - this.Position) / 10.0f + this.Position;
            ViewMatrix = Matrix.CreateLookAt(Position + shakeOffset, Target + shakeOffset, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            Frustum.Matrix = ViewProjMatrix;
        }

        /// <summary>
        /// Changes camera's target look-at position, smoothly
        /// </summary>
        /// <param name="newTarget"></param>
        public void UpdateTarget(Vector3 newTarget)
        {
            Target = (newTarget - this.Target) / 10.0f + this.Target;
            ViewMatrix = Matrix.CreateLookAt(Position + shakeOffset, Target + shakeOffset, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            Frustum.Matrix = ViewProjMatrix;
        }

        /// <summary>
        /// Move the camera to a new location and update its look-at position, smoothly. This exists
        /// as a convenience instead of calling UpdatePosition and UpdateTarget, which would
        /// calculate the view and projection matrices twice unnecessarily.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="newTarget"></param>
        public void UpdatePositionTarget(Vector3 newPosition, Vector3 newTarget, bool lerp = true)
        {
            Position = lerp ? Vector3.Lerp(this.Position, newPosition, 0.1f) : newPosition;
            Target = lerp ? ((newTarget - this.Target) / 10.0f + this.Target) : newTarget;
            ViewMatrix = Matrix.CreateLookAt(Position + shakeOffset, Target + shakeOffset, Up);
            ViewProjMatrix = ViewMatrix * ProjMatrix;
            Frustum.Matrix = ViewProjMatrix;
        }

        /// <summary>
        /// Update the camera for following a target, based on the horizontal angle argument and the
        /// camera's vertical follow angle and offset fields.
        /// </summary>
        /// <param name="horizontalAngle">Angle to follow on the ground-plane unit circle in Radians</param>
        private void UpdateCameraFollow(float horizontalAngle)
        {
            // Locate the 2D direction on the unit circle
            (float sin, float cos) = MathF.SinCos(horizontalAngle);
            followDir2D = new Vector2(sin, cos);

            // Calculate the base follow offset position (from the follow target) using the
            // followAngle, between 0 (horizon) and 90 (top down)
            var sinCos = MathF.SinCos(FollowAngleVertical);
            Vector3 followOffset = new Vector3(
                (float)(MathF.Cos(MathHelper.PiOver4) * sinCos.Cos),
                (float)sinCos.Sin,
                (float)(MathF.Cos(MathHelper.PiOver4) * sinCos.Cos));

            // Multiply by the diagonal direction
            followOffset.X *= followDir2D.X;
            followOffset.Z *= followDir2D.Y;

            Vector3 newPosition = followTarget.Position + Vector3.Normalize(followOffset) * FollowDistance;

            ProjMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);

            UpdatePositionTarget(newPosition, followTarget.Position);
        }

        /// <summary>
        /// Update the shake timer and recalculate the shake offset for this frame based on a
        /// gradually decreasing magnitude calculation. Implementation heavily borrowed from the XNA
        /// Sample: https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/CameraShake_4_0
        /// </summary>
        /// <param name="gameTime"></param>
        private void UpdateShake(GameTime gameTime)
        {
            // If we're shaking...
            if (isShaking)
            {
                // Move our timer ahead based on the elapsed time
                shakeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                // If we're at the max duration, we're not going to be shaking anymore
                if (shakeTimer >= shakeDuration)
                {
                    isShaking = false;
                    shakeTimer = shakeDuration;
                }

                // Compute our progress in a [0, 1] range
                float progress = shakeTimer / shakeDuration;

                // Compute our magnitude based on our maximum value and our progress. This causes
                // the shake to reduce in magnitude as time moves on, giving us a smooth transition
                // back to being stationary. We use progress * progress to have a non-linear fall
                // off of our magnitude. We could switch that with just progress if we want a linear
                // fall off.
                float magnitude = shakeMagnitude * (1f - (progress * progress));

                // Generate a new offset vector with three random values and our magnitude
                shakeOffset = new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle()) * magnitude;
            }
            else
            {
                shakeOffset = Vector3.Zero;
            }
        }

        public void UpdateCamera(GameTime gameTime, bool isPaused)
        {
            UpdateShake(gameTime);

            if ((Mode == CameraMode.Follow) && followTarget != null && !isPaused)
            {
                // In follow mode, users can use the directional inputs to rotate the camera along a
                // horizontal axis.

                // horizontal input between [-1, 1]
                float horizontalInput = UserAction.CameraMovement.GetValue(services.GetService<Input>()).X;

                FollowAngleHorizontal -= (services.GetService<UserSettings>().InvertCameraControls ? -1 : 1) * 0.04f * horizontalInput;

                if (horizontalInput > 0)
                {
                    OnRotateLeft?.Invoke(this, null);
                }
                else if (horizontalInput < 0)
                {
                    OnRotateRight?.Invoke(this, null);
                }

                UpdateCameraFollow(FollowAngleHorizontal);
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
            }
            else if (followTarget != null)
            {
                // In paused mode, make the camera really up close. We use const values here and
                // don't update the camera fields. This way we can reset to the previous values easily.
                const float tempFollowDistance = 23f;
                const float tempFollowAngle = 0.411f;
                const float tempFieldOfView = 0.965f;

                // Calculate the base follow offset position (from the follow target) using the
                // followAngle, between 0 (horizon) and 90 (top down)
                var sinCos = MathF.SinCos(tempFollowAngle);
                Vector3 followOffset = new Vector3(
                    (float)(MathF.Cos(MathHelper.PiOver4) * sinCos.Cos),
                    (float)sinCos.Sin,
                    (float)(MathF.Cos(MathHelper.PiOver4) * sinCos.Cos));

                // Multiply by the diagonal direction
                followOffset.X *= followDir2D.X;
                followOffset.Z *= followDir2D.Y;

                Vector3 newPosition = followTarget.Position + Vector3.Normalize(followOffset) * tempFollowDistance;

                // Find which direction to offset the camera by so it fits in the right half of the
                // paused screen
                Vector3 unitScreenLeft = Vector3.Normalize(Vector3.Cross(followOffset, Vector3.UnitY));

                // Tween the projection matrix for a smooth transition
                Matrix targetProjMatrix = Matrix.CreatePerspectiveFieldOfView(tempFieldOfView, services.GetService<GraphicsDevice>().Viewport.AspectRatio, 0.1f, FAR_PLANE);
                ProjMatrix = (targetProjMatrix - ProjMatrix) / 10f + ProjMatrix;

                UpdatePositionTarget(newPosition, followTarget.Position + unitScreenLeft * 6);
            }
        }

        public void UI()
        {
            ImGui.Begin("Camera");
            ImGui.Text($"Camera Coordinates: {Position}");
            ImGui.Text($"Camera Focus: {Target}");

            ImGui.DragFloat("Field of View (rad): ", ref FieldOfView, 0.01f, 0.01f, MathHelper.Pi - 0.01f);

            bool isStatic = Mode == CameraMode.FourPointStatic;
            if (ImGui.BeginCombo("Mode", Mode.ToString()))
            {
                if (ImGui.Selectable("Static four points"))
                {
                    Mode = CameraMode.FourPointStatic;
                }
                else if (ImGui.Selectable("Follow horizontal"))
                {
                    Mode = CameraMode.Follow;
                }
                ImGui.EndCombo();
            }

            if (Mode == CameraMode.FourPointStatic)
            {
                ImGui.SliderInt("Active Camera", ref CurrentCameraDirIndex, 0, 3);

                // imgui accepts system.numerics.vector3 and not XNA.vector3 so temporarily convert
                System.Numerics.Vector3 pos1 = StaticPositions[CurrentCameraDirIndex].ToNumerics();
                ImGui.DragFloat3($"Position for Camera {CurrentCameraDirIndex}", ref pos1);
                // other-way around can work implicitly
                StaticPositions[CurrentCameraDirIndex] = pos1;
            }

            if (Mode == CameraMode.Follow)
            {
                ImGui.DragFloat("Follow Offset", ref FollowDistance);
                ImGui.DragFloat("Follow Vertical Angle", ref FollowAngleVertical, 0.01f, 0, MathHelper.Pi / 2.0f);
                ImGui.DragFloat("Follow Horizontal Angle", ref FollowAngleHorizontal, 0.01f);
            }

            ImGui.End();
        }
    }
}

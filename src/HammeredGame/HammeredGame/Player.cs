﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using ImMonoGame.Thing;
using ImGuiNET;
using System.Collections.Generic;

namespace HammeredGame
{
    class Player : GameObject, IImGui
    {
        // Private variables specific to the player class
        private float baseSpeed = 0.25f;
        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;
        private List<GameObject> activeLevelObstacles;

        Input inp;
        Camera activeCamera;

        // Initialize player class
        public Player(Model model, Vector3 pos, float scale, Input inp, Camera cam, Texture2D t, List<GameObject> alo)
        {
            this.model = model;
            this.position = pos;
            this.scale = scale;
            this.rotation = Quaternion.Identity;

            this.inp = inp;
            this.activeCamera = cam;
            this.tex = t;
            this.activeLevelObstacles = alo;
        }

        // Update (called every tick)
        public override void Update(GameTime gameTime)
        {
            Vector3 oldPos = this.position;
            bool moveDirty = false;

            // Get forward direction
            Vector3 forwardDirectionFromCamera = Vector3.Normalize(Vector3.Multiply(activeCamera.target - activeCamera.pos, new Vector3(1, 0, 1)));

            // Keyboard input (W - forward, S - back, A - left, D - right)
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
                player_vel += -Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.D))
            {
                player_vel += Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }

            // GamePad Control (if controller is connected) - Left stick controls motion of player
            float MovePad_UpDown = 0;
            float MovePad_LeftRight = 0;
            if (inp.gp.IsConnected)
            {
                MovePad_LeftRight = inp.gp.ThumbSticks.Left.X;
                MovePad_UpDown = inp.gp.ThumbSticks.Left.Y;
                if ((MovePad_UpDown < -Input.DEADZONE) || (MovePad_UpDown > Input.DEADZONE) || (MovePad_LeftRight < -Input.DEADZONE) || (MovePad_LeftRight > Input.DEADZONE))
                {
                    //player_vel.X = (MovePad_LeftRight * activeCamera.view.Right.X + MovePad_UpDown * activeCamera.view.Forward.X) * baseControllerSpeed; // left-right_control * right_from_camera + up-down_control * forward_from_camera
                    //player_vel.Z = (MovePad_LeftRight * activeCamera.view.Right.Z + MovePad_UpDown * activeCamera.view.Forward.Z) * baseControllerSpeed; // use this formala along x and z motions for character movement
                    player_vel = (MovePad_LeftRight * Vector3.Cross(forwardDirectionFromCamera, Vector3.Up) + MovePad_UpDown * forwardDirectionFromCamera) * baseControllerSpeed;
                    moveDirty = true;
                }
            }

            // If there was movement, normalize speed and edit rotation of character model
            if (moveDirty)
            {

                // Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                // Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();
                player_vel *= baseSpeed;

                this.position += player_vel;

                // At this point, also rotate the player to the direction of movement
                Vector3 lookDirection = this.position - oldPos;
                float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                this.rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
            } else
            {
                // No new keypresses or controller interactions this round, so
                // apply a gradual slowdown to any previous velocity
                player_vel.X *= 0.5f;
                player_vel.Z *= 0.5f;

                this.position += player_vel;
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

            // Obstacle collision detection - will be modified/removed later
            // Check for any obstacles in the current level
            if (activeLevelObstacles == null) return;
            BoundingBox currbbox = this.GetBounds();
            foreach (GameObject gO in activeLevelObstacles)
            {
                // Very very basic collision detection
                // Check for collisions by checking for bounding box intersections
                if (gO != null && !gO.destroyed)
                {
                    BoundingBox checkbbox = gO.GetBounds();
                    if (currbbox.Intersects(checkbbox))
                    {
                        // If there is an intersection with an obstacle, reset movement
                        this.position = oldPos;
                    }
                }
            }

            // For the purposes of the functional minimum
            // Temporary BOUNDS to clamp player position within the bounds of the test terrain/ground
            // TODO: Ideally, this will be dynamically determined by the current active level's bounds
            // (determined within the xml / dynamic bounds detection ?)
            this.position = Vector3.Clamp(this.position, new Vector3(-30f, 0f, -30f), new Vector3(30f, 0f, 30f));

        }

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        // TODO: Something's wrong here - this should be a function that could be common for all objects
        public override void Draw(Matrix view, Matrix projection)
        {
            Vector3 pos = this.GetPosition();
            Quaternion rot = this.GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            // The scales seem to be off when importing the meshes into Monogame
            // Shouldn't need to be doing these magic transformations here
            Matrix scaleMatrix = Matrix.CreateScale(scale, 2 * scale, scale);

            // Issue is probably in the order of matrix multiplication here - need to modify
            Matrix world = rotationMatrix * translationMatrix * scaleMatrix;

            // Given the above calculations are correct, we draw the model/mesh
            DrawModel(model, world, view, projection, tex);
        }

        public void UI()
        {
            ImGui.SetNextWindowBgAlpha(0.3f);
            ImGui.Begin("Player Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            var numericPos = this.position.ToNumerics();
            ImGui.DragFloat3("Position", ref numericPos);
            this.position = numericPos;

            ImGui.End();
        }
    }
}

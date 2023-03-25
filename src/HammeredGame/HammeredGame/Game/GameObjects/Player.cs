using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using ImMonoGame.Thing;
using ImGuiNET;
using System.Collections.Generic;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>Player</c> class represents the playable character in the game. TODO: Should the class be renamed to 'Character' instead?
    ///
    /// The character is the main medium through which (whom?) the player can initiate interactions in the game.
    /// The player interacts with the character with the use of the keyboard or a controller.
    /// The actions the player can make with the character are:
    /// - Movement along the 3D space (capability to move along the height dimension is dependent on the environment)
    /// - Change the state of the hammer (for details see <c>Hammer</c> in "Hammer.cs" file)
    /// </summary>
    /// <remarks>
    /// Documentation: The <c>Player</c> instance will be mentioned as "character" in the following code.
    ///
    /// REMINDER (class tree): GameObject -> Player
    ///
    /// TODO: Should the class be renamed to 'Character' instead?
    ///
    /// Possible extension:
    /// <c>Hammer</c> instance be attached to a <c>Player</c> instance?
    /// This may allow multi-player capabilities or more complex puzzle-solving in the future
    /// <example>
    /// The character exchanges between different hammers in the same puzzle
    /// </example>
    /// </remarks>

    public class Player : GameObject, IImGui
    {
        // Private variables specific to the player class
        private float baseSpeed = 0.5f;
        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;

        public Vector3 oldPos;

        public Camera activeCamera;

        // TEMPORARY (FOR TESTING)
        public bool onTree = false;

        Input inp;

        // Initialize player class
        public Player(Model model, Vector3 pos, float scale, Texture2D t, Input inp, Camera cam) : base(model, pos, scale, t)
        {
            this.inp = inp;
            this.activeCamera = cam;
        }

        // Update (called every tick)
        public override void Update(GameTime gameTime)
        {
            ///<value>
            /// The variable <c>moveDirty</c> indicates whether there has been any input from the player
            /// with the intent to move the character.
            ///
            /// <remarks> Generally, "dirty flags" are used to indicate that some data has changed </remarks>
            ///</value>
            bool moveDirty = false;

            // Get forward direction
            Vector3 forwardDirectionFromCamera = Vector3.Normalize(Vector3.Multiply(activeCamera.Target - activeCamera.Position, new Vector3(1, 0, 1)));

            // Handling input from keyboard.
            moveDirty = this.KeyboardInput(forwardDirectionFromCamera);

            // Handling input from gamepad.
            if (inp.gp.IsConnected)
            {
                moveDirty = moveDirty || GamepadInput(forwardDirectionFromCamera);
            }


            // If there was movement, normalize speed and edit rotation of character model
            // Also account for collisions
            if (moveDirty)
            {
                // Set the player's old position (as of previous tick)
                // Memorizing this state has multiple uses, including:
                // a) velocity direction computation (current - old)
                // b) reverting to the previous position in case of an unwanted state (e.g. character cannot enter water)
                this.oldPos = this.position;

                // Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                // Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();
                player_vel *= baseSpeed;

                position += player_vel;

                // At this point, also rotate the player to the direction of movement
                Vector3 lookDirection = position - oldPos; lookDirection.Normalize(); // Normalizing for good measure.
                ///<remark>
                /// The "angle" variable and the subsequent "rotation" variable below
                /// currently handle rotations in the xz plane.
                /// There might be a need to decide whether the character rotation should account for slopes
                /// <example>When walking up an inclined piece of land, the character might be facing upwards.</example>
                ///</remark>
                float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

                // The bounding box of the character when they move (translated and/or rotated AABB)
                // is recomputed inside the "foreach" loop

                // Obstacle collision detection - will be modified/removed later
                foreach (EnvironmentObject gO in HammeredGame.activeLevelObstacles)
                {
                    // Very very basic collision detection
                    // Check for collisions by checking for bounding box intersections
                    if (gO != null && gO.isVisible())
                    {

                        // We only care for the bounding box of the character if there *is* an obstacle in the scene.
                        // Otherwise it is wasted computational time.
                        this.computeBounds();

                        // If the player intersects with another game object
                        // trigger the hitByPlayer function of that gameobject
                        if (this.boundingBox.Intersects(gO.boundingBox))
                        {
                            gO.hitByPlayer(this);
                            // TEMPORARY: if the player is not on tree
                            // and intersects with water (onGround returns if the player has hit a groundobject,
                            // currently water is the only ground object being considered for collisions),
                            // then set player back to old position
                            // There might be a better solution to this
                            if (gO.isGround && !this.onTree)
                            {
                                //System.Diagnostics.Debug.WriteLine(this.oldPos + " -> " + this.position);
                                this.position = this.oldPos;
                                //this.position = Vector3.Zero;
                            }
                        }
                        else
                        {
                            gO.notHitByPlayer(this);
                        }
                    }
                }
            }
            else
            {
                ///<remark>
                /// Leaving the following code chunk on purpose to remind us of possible bugs.
                /// It resulted in the character managing to move when the keys were released.
                /// Thus, the character could move inside water or execute other "illegal" moves.
                ///</remark>
                // No new keypresses or controller interactions this round, so
                // apply a gradual slowdown to any previous velocity
                //player_vel.X *= 0.5f;
                //player_vel.Z *= 0.5f;

                //position += player_vel;
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

            ///<remarks>
            /// This is a temporary measure which works for the rectangular map included in the tutorial level of the functional minimum.
            /// To capture map boundaries in an arbitrarily shaped world (e.g. island),
            /// the need to integrate an (external) physics library for precise bounding checks arises.
            ///
            /// TODO: Integrate an (external) physics library in the project.
            /// TODO: Remember to change the clamping values to match the final tutorial level that will be constructed.
            ///</remarks>
            this.position = Vector3.Clamp(this.position, new Vector3(-60f, -60f, -60f), new Vector3(60f, 60f, 60f));

        }

        private bool KeyboardInput(Vector3 forwardDirectionFromCamera)
        {
            // Adjust player velocity based on input
            // Keyboard input (W - forward, S - back, A - left, D - right)

            bool moveDirty = false;

            if (inp.KeyDown(Keys.W))
            {
                this.player_vel += forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.S))
            {
                this.player_vel += -forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.A))
            {
                this.player_vel += -Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }
            if (inp.KeyDown(Keys.D))
            {
                this.player_vel += Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }

            return moveDirty;

        }

        private bool GamepadInput(Vector3 forwardDirectionFromCamera)
        {
            bool moveDirty = false;

            float MovePad_UpDown = 0;
            float MovePad_LeftRight = 0;
            MovePad_LeftRight = inp.gp.ThumbSticks.Left.X;
            MovePad_UpDown = inp.gp.ThumbSticks.Left.Y;
            if (MovePad_UpDown < -Input.DEADZONE || MovePad_UpDown > Input.DEADZONE || MovePad_LeftRight < -Input.DEADZONE || MovePad_LeftRight > Input.DEADZONE)
            {
                player_vel = (MovePad_LeftRight * Vector3.Cross(forwardDirectionFromCamera, Vector3.Up) + MovePad_UpDown * forwardDirectionFromCamera) * baseControllerSpeed;
                moveDirty = true;
            }

            return moveDirty;
        }

        public void UI()
        {
            ImGui.SetNextWindowBgAlpha(0.3f);
            ImGui.Begin("Player Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            var numericPos = position.ToNumerics();
            ImGui.DragFloat3("Position", ref numericPos);
            position = numericPos;

            ImGui.End();
        }
    }
}

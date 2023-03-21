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
    class Hammer : GameObject
    {
        // Hammer specific variables
        private float hammerSpeed = 0.1f;
        private bool hammerDropped = false;
        private bool hammerEnroute = false;
        private Vector3 dropPos = Vector3.Zero;
        private Vector3 targetPos = Vector3.Zero;

        private List<GameObject> activeLevelObstacles;

        Input inp;
        Camera activeCamera;
        Player _player;

        public Hammer(Model model, Vector3 pos, float scale, Player p, Input inp, Camera cam, Texture2D t, List<GameObject> alo)
        {
            this.model = model;
            this.position = pos;
            this.scale = scale;
            this.rotation = Quaternion.Identity;

            this.inp = inp;
            this.activeCamera = cam;
            this.tex = t;
            this._player = p;
            this.activeLevelObstacles = alo;
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime)
        {
            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (!hammerDropped && !hammerEnroute)
            {
                this.position = this._player.GetPosition();
            }

            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (!hammerDropped && inp.KeyDown(Keys.E))
            {
                // Set boolean to indacte hammer is dropped
                this.hammerDropped = true;
                // Set drop position
                this.dropPos = this.position;
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // Otherwise 'Q' does nothing
            if (hammerDropped && inp.KeyDown(Keys.Q))
            {
                // Trigger hammer movement - hammer is enroute
                this.hammerEnroute = true;
            }

            // GamePad Control (A - Hammer drop, B - Hammer call back)
            // Same functionality as with above keyboard check
            if (inp.gp.IsConnected)
            {
                if (inp.ButtonPress(Buttons.A))
                {
                    this.hammerDropped = true;
                    this.dropPos = this.position;
                }
                if (inp.ButtonPress(Buttons.B))
                {
                    this.hammerEnroute = true;
                }
            }

            // If hammer is called back (successfully), update its position
            // and handle interactions along the way - ending once the hammer is back with player
            // TODO: this is currently just the hammer's position being updated with very naive collision checking
            // This is where the path finding should take place - so this will need to change for improved hammer mechanics
            if (this.hammerEnroute && this.hammerDropped)
            {
                // Update position
                this.position += this.hammerSpeed * (this._player.GetPosition() - this.position);
                
                // If position is close enough to player, end its traversal
                if ((this.position - this._player.GetPosition()).Length() < 0.5f)
                {
                    this.hammerDropped = false;
                    this.hammerEnroute = false;
                }

                // Check for any collisions along the way
                //BoundingBox currbbox = this.GetBounds();
                //foreach (GameObject gO in activeLevelObstacles)
                //{
                //    if (gO != null && !gO.destroyed)
                //    {
                //        BoundingBox checkbbox = gO.GetBounds();
                //        if (currbbox.Intersects(checkbbox))
                //        {
                //            // If hit obstacle - destroy it
                //            gO.destroyed = true;
                //        }
                //    }
                //}
            }
        }

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        // TODO: Something's wrong here - this should be a function that could be common for all objects
        public override void Draw(Matrix view, Matrix projection)
        {
            //if (!hammerDropped) return;

            Vector3 pos = this.GetPosition();
            Quaternion rot = this.GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            Matrix scaleMatrix = Matrix.CreateScale(scale, scale, scale);

            // Construct world matrix
            Matrix world = scaleMatrix * rotationMatrix * translationMatrix;

            // Given the above calculations are correct, we draw the model/mesh
            DrawModel(model, world, view, projection, tex);
        }
    }
}
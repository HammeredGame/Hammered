using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Classes.GameObjects
{
    public class Hammer : GameObject
    {
        // Hammer specific variables
        private float hammerSpeed = 0.1f;
        private bool hammerDropped = false;
        private bool hammerEnroute = false;

        public Vector3 oldPos;

        Input inp;
        Player _player;

        public Hammer(Model model, Vector3 pos, float scale, Player p, Input inp, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
            this.inp = inp;
            _player = p;
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime)
        {
            oldPos = this.position;
            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (!hammerDropped && !hammerEnroute)
            {
                position = _player.GetPosition();
            }

            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (!hammerDropped && inp.KeyDown(Keys.E))
            {
                // Set boolean to indacte hammer is dropped
                hammerDropped = true;
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // Otherwise 'Q' does nothing
            if (hammerDropped && inp.KeyDown(Keys.Q))
            {
                // Trigger hammer movement - hammer is enroute
                hammerEnroute = true;
            }

            // GamePad Control (A - Hammer drop, B - Hammer call back)
            // Same functionality as with above keyboard check
            if (inp.gp.IsConnected)
            {
                if (inp.ButtonPress(Buttons.A))
                {
                    hammerDropped = true;
                }
                if (inp.ButtonPress(Buttons.B))
                {
                    hammerEnroute = true;
                }
            }

            // If hammer is called back (successfully), update its position
            // and handle interactions along the way - ending once the hammer is back with player
            // TODO: this is currently just the hammer's position being updated with very naive collision checking
            // This is where the path finding should take place - so this will need to change for improved hammer mechanics
            if (hammerEnroute && hammerDropped)
            {
                // Update position
                position += hammerSpeed * (_player.GetPosition() - position);

                // If position is close enough to player, end its traversal
                if ((position - _player.GetPosition()).Length() < 0.5f)
                {
                    hammerDropped = false;
                    hammerEnroute = false;
                }

                // Check for any collisions along the way
                BoundingBox hammerbbox = this.GetBounds();
                List<EnvironmentObject> hitObjects = new List<EnvironmentObject>();
                foreach(EnvironmentObject gO in HammeredGame.activeLevelObstacles)
                {
                    if (gO != null && gO.isVisible())
                    {
                        BoundingBox objectbbox = gO.GetBounds();
                        if (hammerbbox.Intersects(objectbbox))
                        {
                            gO.hitByHammer(this);
                            hitObjects.Add(gO);
                        }
                    }
                }
            }
        }

        public void setEnroute(bool enroute)
        {
            hammerEnroute = enroute;
        }
    }
}
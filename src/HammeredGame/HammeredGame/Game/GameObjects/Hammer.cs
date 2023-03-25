using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>Hammer</c> class defines the properties and interactions specific to the core "Hammer" mechanic of the game.
    ///
    /// In addition to base <c>GameObject</c> properties, the hammer also has the following properties defined:
    /// - speed of the hammer (how fast it will travel, when called back to the player character -> <code>float hammerSpeed</code>
    /// - the current state of the hammer with respect to the keyboard/gamepad input + context within the scene -> <code>HammerState _hammerState</code>
    ///     -- follow the player character -> <code>HammerState.WithCharacter</code>
    ///     -- hammer is dropped (it will stay in the dropped location until called back to player) -> <code>HammerState.Dropped</code>
    ///     -- hammer is called back and must find its way back to the player  -> <code>HammerState.Enroute</code>
    ///
    /// An additional variable holding the hammer's position in the previous frame/tick is also provided -> <code>Vector3 oldPos</code>.
    /// This variable, along with the hammer state, helps in determining contextual interactions with certain other objects that may be in the scene.
    /// <example>
    /// Determining the falling direction of a tree or blocking the hammer if an unbreakable obstacle is in the way)
    /// </example>
    ///
    /// This class also has access to an instance of the <c>Player</c> class, mainly for the purpose of path finding, by keeping track of the position
    /// of the player within the level.
    /// </summary>
    ///

    /// <remark>
    /// TODO: Improved path finding - technical achievement of the game!
    /// </remark>
    public class Hammer : GameObject
    {
        public enum HammerState
        {
            WithCharacter,
            Dropped,
            Enroute
        }

        // Hammer specific variables
        private float hammerSpeed = 0.1f;
        private HammerState _hammerState;

        public Vector3 oldPos { get; private set; }

        private Input inp;
        private Player _player;

        public Hammer(Model model, Vector3 pos, float scale, Texture2D t, Input inp, Player p)
            : base(model, pos, scale, t)
        {
            this.inp = inp;
            _player = p;
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime)
        {
            oldPos = this.Position;

            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (_hammerState == HammerState.WithCharacter)
            {
                Position = _player.GetPosition();
            }

            // Get the input via keyboard or gamepad
            KeyboardInput(); GamePadInput();

            // If hammer is called back (successfully), update its position
            // and handle interactions along the way - ending once the hammer is back with player
            /// <remark>
            /// TODO: this is currently just the hammer's position being updated with very naive collision checking
            /// This is most likely where the path finding should take place - so this will need to change for improved hammer mechanics
            /// </remark>
            if (_hammerState != HammerState.WithCharacter)
            {
                if (_hammerState == HammerState.Enroute)
                {
                    // Update position
                    Position += hammerSpeed * (_player.GetPosition() - Position);

                    // If position is close enough to player, end its traversal
                    if ((Position - _player.GetPosition()).Length() < 0.5f)
                    {
                        _hammerState = HammerState.WithCharacter;
                    }

                    this.ComputeBounds();
                }

                // Check for any collisions along the way
                //BoundingBox hammerbbox = this.GetBounds();
                foreach(EnvironmentObject gO in HammeredGame.ActiveLevelObstacles)
                {
                    if (gO != null && gO.IsVisible())
                    {
                        //BoundingBox objectbbox = gO.GetBounds();
                        if (this.BoundingBox.Intersects(gO.BoundingBox))
                        {
                            gO.hitByHammer(this);
                        }
                        else
                        {
                            gO.notHitByHammer(this);
                        }
                    }
                }
            }
        }

        public void KeyboardInput()
        {
            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (_hammerState == HammerState.WithCharacter && inp.KeyDown(Keys.E))
            {
                _hammerState = HammerState.Dropped;
                this.ComputeBounds();
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // Otherwise 'Q' does nothing
            if (_hammerState == HammerState.Dropped && inp.KeyDown(Keys.Q))
            {
                _hammerState = HammerState.Enroute;
            }
        }

        public void GamePadInput()
        {
            // GamePad Control (A - Hammer drop, B - Hammer call back)
            // Same functionality as with above keyboard check
            if (inp.GamePadState.IsConnected)
            {
                if (_hammerState == HammerState.WithCharacter && inp.ButtonPress(Buttons.A))
                {
                    _hammerState = HammerState.Dropped;
                    this.ComputeBounds();
                }
                if (_hammerState == HammerState.Dropped && inp.ButtonPress(Buttons.B))
                {
                    _hammerState = HammerState.Enroute;
                }
            }
        }

        public bool isEnroute()
        {
            return (_hammerState == HammerState.Enroute);
        }

        public void setState(HammerState newState)
        {
            _hammerState = newState;
        }
    }
}

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
    /// <para/>
    /// In addition to base <c>GameObject</c> properties, the hammer also has the following properties defined:
    /// - speed of the hammer (how fast it will travel, when called back to the player character -> <code>float hammerSpeed</code>
    /// - the current state of the hammer with respect to the keyboard/gamepad input + context within the scene -> <code>HammerState _hammerState</code>
    ///     -- follow the player character -> <code>HammerState.WithCharacter</code>
    ///     -- hammer is dropped (it will stay in the dropped location until called back to player) -> <code>HammerState.Dropped</code>
    ///     -- hammer is called back and must find its way back to the player  -> <code>HammerState.Enroute</code>
    /// <para/>
    /// An additional variable holding the hammer's position in the previous frame/tick is also provided -> <code>Vector3 oldPos</code>.
    /// This variable, along with the hammer state, helps in determining contextual interactions with certain other objects that may be in the scene.
    /// <example>
    /// Determining the falling direction of a tree or blocking the hammer if an unbreakable obstacle is in the way)
    /// </example>
    /// <para/>
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
        private float hammerSpeed = 0.2f;
        private HammerState hammerState;

        public Vector3 OldPosition { get; private set; }

        private Player player;

        public Hammer(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale)
            : base(services, model, t, pos, rotation, scale)
        {
        }

        public void SetOwnerPlayer(Player player)
        {
            this.player = player;
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime)
        {
            OldPosition = this.Position;

            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (hammerState == HammerState.WithCharacter && player != null)
            {
                Position = player.GetPosition();
            }

            // Get the input via keyboard or gamepad
            KeyboardInput(); GamePadInput();

            // If hammer is called back (successfully), update its position
            // and handle interactions along the way - ending once the hammer is back with player
            /// <remark>
            /// TODO: this is currently just the hammer's position being updated with very naive collision checking
            /// This is most likely where the path finding should take place - so this will need to change for improved hammer mechanics
            /// </remark>
            if (hammerState != HammerState.WithCharacter)
            {
                if (hammerState == HammerState.Enroute && player != null)
                {
                    // Update position
                    Position += hammerSpeed * (player.GetPosition() - Position);

                    // If position is close enough to player, end its traversal
                    if ((Position - player.GetPosition()).Length() < 0.5f)
                    {
                        hammerState = HammerState.WithCharacter;
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
                        if (this.BoundingBox.Intersects(gO.BoundingBox) && hammerState != HammerState.WithCharacter)
                        {
                            gO.TouchingHammer(this);
                        }
                        else
                        {
                            gO.NotTouchingHammer(this);
                        }
                    }
                }
            }
        }

        public void KeyboardInput()
        {
            Input input = Services.GetService<Input>();
            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (hammerState == HammerState.WithCharacter && input.KeyDown(Keys.E))
            {
                hammerState = HammerState.Dropped;
                this.ComputeBounds();
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // And if the owner player is defined
            // Otherwise 'Q' does nothing
            if (hammerState == HammerState.Dropped && player != null && input.KeyDown(Keys.Q))
            {
                hammerState = HammerState.Enroute;
            }
        }

        public void GamePadInput()
        {
            Input input = Services.GetService<Input>();
            // GamePad Control (A - Hammer drop, B - Hammer call back)
            // Same functionality as with above keyboard check
            if (input.GamePadState.IsConnected)
            {
                if (hammerState == HammerState.WithCharacter && input.ButtonPress(Buttons.A))
                {
                    hammerState = HammerState.Dropped;
                    this.ComputeBounds();
                }
                if (hammerState == HammerState.Dropped && player != null && input.ButtonPress(Buttons.B))
                {
                    hammerState = HammerState.Enroute;
                }
            }
        }

        public bool IsEnroute()
        {
            return hammerState == HammerState.Enroute;
        }

        public void SetState(HammerState newState)
        {
            hammerState = newState;
        }
    }
}

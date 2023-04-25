using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.Entities;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Microsoft.Xna.Framework.Audio;

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
        private float hammerSpeed = 7f;
        private HammerState hammerState;

        public Vector3 OldPosition { get; private set; }

        private Player player;

        private List<SoundEffect> hammer_sfx = new List<SoundEffect>();
        //how long till trigger next sound
        //TimeSpan audioDelay = TimeSpan.Zero;

        private AudioManager audioManager;

        public event EventHandler OnSummon;
        public event EventHandler OnCollision;
        public event EventHandler OnDrop;

        public Hammer(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity)
            : base(services, model, t, pos, rotation, scale, entity)
        {
            hammerState = HammerState.WithCharacter;
            hammer_sfx = Services.GetService<List<SoundEffect>>();
            audioManager = Services.GetService<AudioManager>();

            if (this.Entity != null)
            {
                // Adding a tag to the entity, to allow us to potentially filter and
                // view bounding volumes (for debugging)
                this.Entity.Tag = "HammerBounds";

                // Setting the entity's collision information tag to the game object itself.
                // This will help in checking for specific collisions in object-specific
                // collision handling.
                this.Entity.CollisionInformation.Tag = this;

                // Set hammer to continuous collision detection
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;

                // Set the entity's collision rule to 'NoBroadPhase' -->
                // This will ensure that the hammer will not be considered for collision
                // constraint solving while attached to the player character
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;

                // Set the entity's local inverse intertia tensor --> this ensures that the
                // player character doesn't just fall over due to gravity
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                this.Entity.Material.KineticFriction = 1.0f;

                // Add entity to the level's active physics space
                this.ActiveSpace.Add(this.Entity);

                // Initialize the collision handlers based on the associated collision events
                this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
            }
        }

        // Collision Handling Event for any initial collisions detected
        private void Events_DetectingInitialCollision(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is Player) return;
                OnCollision?.Invoke(this, null);

                Input input = Services.GetService<Input>();
                if (input.GamePadState.IsConnected)
                {
                    input.VibrateController(0.75f, 0.75f);
                    // TODO: Add asynchronous wait here? (to have the vibration last a little longer?)
                    // DONE!
                    Services.GetService<ScriptUtils>().WaitMilliseconds(50).ContinueWith((_) => input.StopControllerVibration());
                }
            }
        }

        public void SetOwnerPlayer(Player player)
        {
            this.player = player;
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            OldPosition = this.Position;

            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (hammerState == HammerState.WithCharacter && player != null)
            {
                Position = player.Position;
            }

            // Get the input via keyboard or gamepad
            HandleInput();

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
                    // Update Hammer's Linear Velocity
                    this.Entity.LinearVelocity = hammerSpeed * (player.Entity.Position - Entity.Position);

                    //// Update position
                    //Position += hammerSpeed * (player.GetPosition() - Position);

                    //// If position is close enough to player, end its traversal
                    //if ((Position - player.GetPosition()).Length() < 0.5f)
                    //{
                    //    hammerState = HammerState.WithCharacter;
                    //}

                    //this.ComputeBounds();
                }

                //// Check for any collisions along the way
                ////BoundingBox hammerbbox = this.GetBounds();
                //foreach(EnvironmentObject gO in HammeredGame.ActiveLevelObstacles)
                //{
                //    if (gO != null && gO.IsVisible())
                //    {
                //        //BoundingBox objectbbox = gO.GetBounds();
                //        if (this.BoundingBox.Intersects(gO.BoundingBox) && hammerState != HammerState.WithCharacter)
                //        {
                //            gO.TouchingHammer(this);
                //        }
                //        else
                //        {
                //            gO.NotTouchingHammer(this);
                //        }
                //    }
                //}

            }
        }

        public void HandleInput()
        {
            Input input = Services.GetService<Input>();
            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (hammerState == HammerState.WithCharacter && UserAction.DropHammer.Pressed(input))
            {
                //hammerState = HammerState.Dropped;
                DropHammer();
                //this.ComputeBounds();
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // And if the owner player is defined
            // And the hammer has a physics entity attached to it
            // Otherwise 'Q' does nothing
            else if (hammerState == HammerState.Dropped && player != null && Entity != null && UserAction.SummonHammer.Pressed(input))
            {
                hammerState = HammerState.Enroute;
                OnSummon?.Invoke(this, null);

                // When hammer is enroute, the physics engine shouldn't solve for
                // collision constraints with it --> rather we want to manually
                // handle collisions
                this.Entity.BecomeKinematic();
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
            }
        }

        public void DropHammer()
        {
            // Set hammer state to dropped
            hammerState = HammerState.Dropped;

            hammer_sfx[1].Play();

            //audioManager.Play3DSound("Audio/hammer_drop", false);

            OnDrop?.Invoke(this, null);

            if (this.Entity != null)
            {
                // Add a lot of mass to the hammer, so it becomes a dynamic entity
                // --> this was to ensure that the hammer interacts properly with
                // pressure plates... However, this may also be the cause of other
                // issues, so this bit of code may need to be tweaked.
                this.Entity.BecomeDynamic(10000);
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                // Only gravitational force being applied to the entity, velocity in the other
                // directions are zeroed out --> hammer is dropped, so it shouldn't move
                this.Entity.LinearVelocity = new BEPUutilities.Vector3(0, -98.1f, 0);

                // Normal collisions to ensure the physics engine solves collision constraint with
                // this entity --> Also, probably a cause for issues
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            }
        }

        public bool IsEnroute()
        {
            //sound effect instance to try and manipulate the pitch, but not working
            if (hammerState == HammerState.Enroute)
            {
                SoundEffectInstance whoosh = hammer_sfx[2].CreateInstance();
                whoosh.Play();
            }
            return hammerState == HammerState.Enroute;
        }

        public void SetState(HammerState newState)
        {
            hammerState = newState;
        }
    }
}

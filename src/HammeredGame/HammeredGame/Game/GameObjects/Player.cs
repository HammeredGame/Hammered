using Aether.Animation;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using HammeredGame.Core.Particles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using HammeredGame.Graphics;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>Player</c> class represents the playable character in the game. TODO: Should the class be renamed to 'Character' instead?
    /// <para />
    /// The character is the main medium through which (whom?) the player can initiate interactions in the game.
    /// The player interacts with the character with the use of the keyboard or a controller.
    /// The actions the player can make with the character are:
    /// - Movement along the 3D space (capability to move along the height dimension is dependent on the environment)
    /// - Change the state of the hammer (for details <see cref="Hammer"/>)
    /// </summary>
    /// <remarks>
    /// Documentation: The <c>Player</c> instance will be mentioned as "character" in the following code.
    /// <para />
    /// REMINDER (class tree): GameObject -> Player
    /// <para />
    /// TODO: Should the class be renamed to 'Character' instead?
    /// <para />
    /// Possible extension:
    /// <c>Hammer</c> instance be attached to a <c>Player</c> instance?
    /// This may allow multi-player capabilities or more complex puzzle-solving in the future
    /// <example>
    /// The character exchanges between different hammers in the same puzzle
    /// </example>
    /// </remarks>

    public class Player : GameObject, IImGui
    {
        // Variables specific to the player class
        public bool InputEnabled = true;

        public float PlayerSpeedModifier = 1f;
        private float baseSpeed = 50f;

        // The following variables control the stepping behaviour of the player: the ankle height is
        // where a first raycast is performed from, for checking if there is an object in front of
        // the player; the knee height is where the second raycast is performed from, to check if
        // there is no object at that height. When both conditions meet, the player moves upwards.
        private float steppingAnkleHeight = 0.1f;
        private float steppingKneeHeight = 5f;

        // The following variables control the maximum length of the raycasts performed for the
        // stepping. Higher values mean that the player detects a step from further away, which may
        // lead to the player floating in the air. Lower values mean that the player needs to be
        // really close to step up, which can lead to a pause in movement.
        //
        // The knee raycast is longer, so the player can step up on a slope.
        private float steppingAnkleRaycastLength = 4f;
        private float steppingKneeRaycastLength = 6f;

        // If we just cast a single ray in the forward direction (of movement), then we won't be
        // able to climb up steps/slopes when we are at an angle. So we cast it in two more
        // directions, and if any of them meet the condition, we move up.
        private float[] steppingCheckAdditionalAngles = new float[]
        {
            MathHelper.ToRadians(-45),
            MathHelper.ToRadians(45)
        };

        // How much in world units to move the player up when the obstacle in front is step-able.
        // Too small values will make the movement slow, while too high and the player will
        // seemingly jump up and drop down, which is jarring.
        private float steppingSingleUpdateHeight = 1f;

        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;
        private bool previously_moving = false;
        private bool waking_up = false;
        private TimeSpan wakeup_time_passed = TimeSpan.Zero;

        public enum PlayerOnSurfaceState
        {
            OnGround,
            OnTree,
            OnRock
        }

        // Last known ground position, used to reset player's position
        // if the player comes into contact with a water object
        private Vector3 lastGroundPosition;

        // Variable to keep track of what surface the player is currently standing on
        public PlayerOnSurfaceState StandingOn { get; set; }


        // TEMPORARY (FOR TESTING)
        public bool ReachedGoal = false;

        public Camera ActiveCamera;

        public Animations Animations;

        TimeSpan timeDelay = TimeSpan.Zero;

        private readonly ParticleSystem victoryStarParticles;

        public event EventHandler OnHammerRetrieved;

        // Initialize player class
        public Player(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            // We want a some stars when the player reaches the goal in each level!
            victoryStarParticles = new ParticleSystem(new ParticleSettings()
            {
                Model = services.GetService<ContentManager>().Load<Model>("Meshes/Primitives/star"),
                Texture = services.GetService<ContentManager>().Load<Texture2D>("key_texture"),
                Duration = TimeSpan.FromMilliseconds(500),

                MinStartSize = 1f,
                MaxStartSize = 1f,

                // They will shrink to nothing over the course of their lifetime
                MinEndSize = 0f,
                MaxEndSize = 0f,

                // Horizontal velocity will be individually assigned to particles when they spawn
                MinHorizontalVelocity = 0f,
                MaxHorizontalVelocity = 0f,

                // Vertical velocity will be present for all particles
                MinVerticalVelocity = 30f,
                MaxVerticalVelocity = 30f,

                MinStartRotation = Quaternion.Identity,
                MaxStartRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.PiOver2),

                // Affected a bit by gravity
                Gravity = ActiveSpace.ForceUpdater.Gravity * 0.5f
            }, services.GetService<GraphicsDevice>(), services.GetService<ContentManager>(), ActiveSpace);

            if (this.Entity != null)
            {
                // Adding a tag to the entity, to allow us to potentially filter and
                // view bounding volumes (for debugging)
                this.Entity.Tag = "PlayerBounds";

                // Setting the entity's collision information tag to the game object itself.
                // This will help in checking for specific collisions in object-specific
                // collision handling. --> See the Events_DetectingInitialCollision function in
                // this file to see an example of how this might be used
                this.Entity.CollisionInformation.Tag = this;

                // Making the character a continuous object prevents it from flying through
                // walls which would be pretty jarring from a player's perspective.
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;

                // Set the entity's local inverse intertia tensor --> this ensures that the
                // player character doesn't just fall over due to gravity
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                // Increase the entity's kinetic friction variable --> currently being used to
                // reduce the character sliding along the surface. Also, not setting it too high
                // since higher values make the player get stuck on certain parts of an uneven ground mesh.
                // TODO: May want the flat ground meshes be as even and flat as possible
                // (except ramps/stairs/ladders to reach higher elevations --> these can maybe be
                // handled separately within collision handling <-- more testing needed for these settings)
                this.Entity.Material.KineticFriction = 2.0f;

                // Add the entity to the level's physics space - this ensures that this game object
                // will be considered for collision constraint solving (handled by the physics engine)
                this.ActiveSpace.Add(this.Entity);

                // Initialize the collision handlers based on the associated collision events
                this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
                this.Entity.CollisionInformation.Events.PairTouching += Events_PairTouching;
                this.Entity.CollisionInformation.Events.ContactCreated += Events_ContactCreated;

                Animations = this.Model.GetAnimations();
                var clip_idle = Animations.Clips["Armature|idle"];
                Animations.SetClip(clip_idle);

                //player_sfx = Services.GetService<List<SoundEffect>>();
                //listener = Services.GetService<AudioListener>();
                //emitter = Services.GetService<AudioEmitter>();

                //set player as the listener for all sfx
                Services.GetService<AudioManager>().listener.Position = this.Position;

                //emit footsteps
                this.AudioEmitter = new AudioEmitter();
                this.AudioEmitter.Position = this.Position;


            }

            // Initial position should be on/over ground
            this.lastGroundPosition = this.Position;
            this.StandingOn = PlayerOnSurfaceState.OnGround;
        }

        private void Events_ContactCreated(EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, BEPUphysics.CollisionTests.ContactData contact)
        {
            // If the player touches water, return the player to the last
            // known ground position
            // Comment this section out, if testing requires walking on water
            if (other.Tag is Water)
            {
                this.Position = this.lastGroundPosition;
                this.StandingOn = PlayerOnSurfaceState.OnGround;
            }
        }

        private void Events_PairTouching(EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            // Make some checks to identify if the last ground position should be updated
            if (other.Tag is Ground)
            {
                if (this.StandingOn != PlayerOnSurfaceState.OnGround)
                {
                    this.StandingOn = PlayerOnSurfaceState.OnGround;
                }

                // If the player is also touching water, then don't update ground position
                foreach (var contactPair in sender.Pairs)
                {
                    if (contactPair.CollidableA.Tag is Water || contactPair.CollidableB.Tag is Water)
                    {
                        return;
                    }
                }

                // If player isn't falling, update last known ground position
                // Falling is currently being determined via a linear y velocity threshold
                //if (Math.Abs(this.Entity.LinearVelocity.Y) < 2.5f)
                if (Math.Abs(this.Position.Y - this.lastGroundPosition.Y) < 0.5f)
                {
                    this.lastGroundPosition = this.Position;
                }
            }
            else if (other.Tag is Hammer)
            {
                // If player character collides with hammer, set hammer to with character state
                // This should only happen when hammer is called back
                var hammer = other.Tag as Hammer;
                if (hammer.IsEnroute())
                {
                    hammer.SetState(Hammer.HammerState.WithCharacter);
                    OnHammerRetrieved?.Invoke(this, null);
                    hammer.Entity.BecomeKinematic();
                    hammer.Entity.LinearVelocity = BEPUutilities.Vector3.Zero;
                    hammer.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;
                }
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
                //We hit an entity!

                // If player character collides with hammer, set hammer to with character state
                // This should only happen when hammer is called back
                if (other.Tag is Hammer)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        hammer.SetState(Hammer.HammerState.WithCharacter);
                        OnHammerRetrieved?.Invoke(this, null);
                        otherEntityInformation.Entity.BecomeKinematic();
                        otherEntityInformation.Entity.LinearVelocity = BEPUutilities.Vector3.Zero;
                        otherEntityInformation.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;
                    }
                }
            }
        }

        /// <summary>
        /// Set the active camera in use, which determines the movement vector for the player.
        /// </summary>
        /// <param name="camera"></param>
        public void SetActiveCamera(Camera camera)
        {
            ActiveCamera = camera;
        }

        public void TriggerWakeUp()
        {
            waking_up = true;
            var clip_wakeup = Animations.Clips[key: "Armature|wakeup"];
            Animations.SetClip(clip_wakeup);
        }

        // Update (called every tick)
        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Zero out the player velocity vector (to remove the possibility of
            // previous computations accumulating/carrying over)
            player_vel = Vector3.Zero;

            // moveDirty indicates whether there has been any input from the player with the intent
            // to move the character.
            bool moveDirty = false;
            Vector3 forwardDirection = Vector3.Zero;
            if (InputEnabled && ActiveCamera != null && screenHasFocus)
            {
                // Get the unit vector (parallel to the y=0 ground plane) in the direction deemed
                // "forward" from the current camera perspective. Calculated by projecting the vector of
                // the current camera position to the player position, onto the ground, and normalising it.
                forwardDirection = Vector3.Normalize(Vector3.Multiply(ActiveCamera.Target - ActiveCamera.Position, new Vector3(1, 0, 1)));

                // Handling input from keyboard.
                moveDirty = this.HandleInput(forwardDirection);
            }

            // If there was movement input, normalize speed and edit rotation of character model
            // Also account for collisions
            if (moveDirty && this.Entity != null && player_vel != Vector3.Zero)
            {
                // Normalize to length 1 regardless of direction, so that diagonals aren't faster
                // than straight. We do this only within moveDirty, since otherwise player_vel can
                // be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();

                // Check if there is an obstacle in front of us that should be stepped over. We use
                // the unit forward velocity here.
                bool shouldStepUp = CheckIfShouldStepUp(player_vel);

                // Apply player speed modifier
                player_vel *= baseSpeed * PlayerSpeedModifier;

                this.Entity.LinearVelocity = new Vector3(player_vel.X, this.Entity.LinearVelocity.Y, player_vel.Z).ToBepu();

                // If there was an obstacle in front of us, modify the Position of the player to go
                // up by a slight amount every frame until the condition is no longer true.
                // Modifying position is better than velocity, since the latter can make the player
                // shoot up into the air for unknown reasons.
                if (shouldStepUp)
                {
                    this.Entity.Position += Vector3.Up.ToBepu() * steppingSingleUpdateHeight;
                }

                // At this point, also rotate the player to the direction of movement

                ///<remark>
                /// The "angle" variable and the subsequent "rotation" variable below
                /// currently handle rotations in the xz plane.
                /// There might be a need to decide whether the character rotation should account for slopes
                /// <example>When walking up an inclined piece of land, the character might be facing upwards.</example>
                ///</remark>
                float angle = (float)Math.Atan2(player_vel.X, player_vel.Z);
                this.Entity.Orientation = BEPUutilities.Quaternion.Slerp(this.Entity.Orientation, BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.UnitY, angle), 0.25f);

                timeDelay -= gameTime.ElapsedGameTime;
                if (timeDelay < TimeSpan.Zero)
                {
                    Services.GetService<AudioManager>().Play3DSound("Audio/balanced/soft_step", false, this.AudioEmitter, 1);
                    timeDelay += TimeSpan.FromSeconds(0.21f);
                }

                if(!previously_moving)
                {
                    // Start running animation when player starts moving
                    var clip_run = Animations.Clips["Armature|run"];
                    Animations.SetClip(clip_run);
                    previously_moving = true;
                    //SoundEffectInstance step = player_sfx[0].CreateInstance();
                    //step.IsLooped = true;
                    //step.Play();
                }
            }
            else
            {
                if(previously_moving)
                {
                    // Start idle animation when player stops moving
                    var clip_idle = Animations.Clips["Armature|idle"];
                    Animations.SetClip(clip_idle);
                    previously_moving = false;
                }
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

            // Play wake up animation (not optimal to handle it here...)
            if(waking_up)
            {
                if(wakeup_time_passed < (Animations.CurrentClip.Duration * 0.98))
                {
                    Animations.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);
                    wakeup_time_passed += gameTime.ElapsedGameTime;
                } else
                {
                    // Stop wake up animation when end of clip has been reached
                    waking_up = false;
                    // Start idle animation
                    var clip_idle = Animations.Clips["Armature|idle"];
                    Animations.SetClip(clip_idle);
                }

            } else
            {
                Animations.Update(gameTime.ElapsedGameTime * 1.2f, true, Matrix.Identity);
            }

            Services.GetService<AudioManager>().listener.Position = this.Position;
            Services.GetService<AudioManager>().listener.Forward = forwardDirection;

            victoryStarParticles.Update(gameTime);

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

            #endregion TEMPORARY_MOUSE_BASED_ROTATION

        }

        /// <summary>
        /// Returns whether the player should move slightly upwards because the object in front of
        /// them is a step-able thing and it's also not very high. We use two ray-casts, one from
        /// the ankle and one from the knee. If the ankle ray hits something but the knee ray
        /// doesn't, then we can step up. This implementation is courtesy of a Unity tutorial on
        /// stepping: https://www.youtube.com/watch?v=DrFk5Q_IwG0
        /// </summary>
        private bool CheckIfShouldStepUp(Vector3 movementDirection)
        {
            // We want to ignore ray-cast hits (i.e. climbing up) on some entities. Ignoring the
            // player and the hammer is ultra-important, since otherwise the ray-casts will always
            // return the player, or maybe the hammer if it's being held by the player, and not any
            // of the actual obstacles to step over.
            static bool rayHitFilter(BEPUphysics.BroadPhaseEntries.BroadPhaseEntry entry)
            {
                return
                    // Ignore empty game objects like bounds or triggers
                    entry.Tag is not EmptyGameObject &&
                    // Ignore objects that don't have a solver like decoration objects
                    entry.CollisionRules.Personal != BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver &&
                    // Ignore the player itself and the hammer
                    entry.Tag is not Player &&
                    entry.Tag is not Hammer;
            }

            // Assuming that the player's position origin is at the center of the box, we need to
            // move it down by half the height to get the foot position.
            Vector3 playerFootPosition = Position + Vector3.Down * (Entity as Box).HalfHeight;

            BEPUutilities.Ray rayAnkle;
            BEPUutilities.Ray rayKnee;

            rayAnkle = new BEPUutilities.Ray((playerFootPosition + Vector3.Up * steppingAnkleHeight).ToBepu(), movementDirection.ToBepu());
            rayKnee = new BEPUutilities.Ray((playerFootPosition + Vector3.Up * steppingKneeHeight).ToBepu(), movementDirection.ToBepu());

            // Cast a ray from the ankle to see if there's an obstacle
            if (ActiveSpace.RayCast(rayAnkle, steppingAnkleRaycastLength, rayHitFilter, out _))
            {
                // Cast a ray from the knee to see if the obstacle is not there (we can climb!)
                if (!ActiveSpace.RayCast(rayKnee, steppingKneeRaycastLength, rayHitFilter, out _))
                {
                     return true;
                }
            }

            // Additionally, check any other angles that are not the forward direction so that we
            // can climb when facing the obstacle from an angle.
            foreach (float angle in steppingCheckAdditionalAngles)
            {
                rayAnkle.Direction = Vector3.Transform(movementDirection, Matrix.CreateRotationY(angle)).ToBepu();
                rayKnee.Direction = Vector3.Transform(movementDirection, Matrix.CreateRotationY(angle)).ToBepu();

                // Cast a ray from the ankle to see if there's an obstacle
                if (ActiveSpace.RayCast(rayAnkle, steppingAnkleRaycastLength, rayHitFilter, out _))
                {
                    // Cast a ray from the knee to see if the obstacle is not there (we can climb!)
                    if (!ActiveSpace.RayCast(rayKnee, steppingKneeRaycastLength, rayHitFilter, out _))
                    {
                        return true;
                    }
                }
            }

            // No conditions met, we can't climb
            return false;
        }

        private bool HandleInput(Vector3 forwardDirectionFromCamera)
        {
            bool moveDirty = false;
            Input input = Services.GetService<Input>();

            // Returns [-1, 1] on X and Y axis, continuous on controllers and discrete {-1, 0, 1} on keyboard.
            Vector2 inputAmount = UserAction.Movement.GetValue(input);

            float MovePad_LeftRight = inputAmount.X;
            float MovePad_UpDown = inputAmount.Y;
            if (MovePad_UpDown < -Input.DEADZONE || MovePad_UpDown > Input.DEADZONE || MovePad_LeftRight < -Input.DEADZONE || MovePad_LeftRight > Input.DEADZONE)
            {
                player_vel = (MovePad_LeftRight * Vector3.Cross(forwardDirectionFromCamera, Vector3.Up) + MovePad_UpDown * forwardDirectionFromCamera);
                moveDirty = true;
            }

            return moveDirty;
        }

        /// <summary>
        /// Show some stars! Should be called when the level is cleared :)
        /// </summary>
        public void ShowVictoryStars()
        {
            if (Entity is Box box) {
                victoryStarParticles.AddParticle(Position + box.Height * Vector3.Up, Vector3.Left * 30);
                victoryStarParticles.AddParticle(Position + box.Height * Vector3.Up, Vector3.Right * 30);
                victoryStarParticles.AddParticle(Position + box.Height * Vector3.Up, Vector3.Forward * 30);
                victoryStarParticles.AddParticle(Position + box.Height * Vector3.Up, Vector3.Backward * 30);
                victoryStarParticles.AddParticle(Position + box.Height * Vector3.Up, Vector3.Zero);

                Services.GetService<AudioManager>().Play3DSound("Audio/balanced/victory", false, AudioEmitter, 1f);
            }
        }

        public override void Draw(GameTime gameTime, Matrix view, Matrix projection, Vector3 cameraPosition, SceneLightSetup lights)
        {
            // Animate mesh
            //Matrix[] transforms = new Matrix[this.Model.Bones.Count];
            //this.Model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in this.Model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    //BasicEffect)part.Effect).SpecularColor = Vector3.Zero;
                    //((SkinnedEffect)part.Effect).SpecularColor = Vector3.Zero;
                    //ConfigureEffectMatrices((IEffectMatrices)part.Effect, Matrix.Identity, view, projection);
                    //ConfigureEffectLighting((IEffectLights)part.Effect);
                    part.UpdateVertices(Animations.AnimationTransforms); // animate vertices on CPU
                    //((SkinnedEffect)part.Effect).SetBoneTransforms(animations.AnimationTransforms);// animate vertices on GPU
                }
            }

            if (Visible)
            {
                DrawModel(gameTime, Model, view, projection, cameraPosition, Texture, lights);
            }

            victoryStarParticles.CopyShadowMapParametersFrom(Effect);
            victoryStarParticles.Draw(gameTime, view, projection, cameraPosition, lights);
        }

        new public void UI()
        {
            base.UI();
            ImGui.Separator();
            ImGui.DragFloat("Base Speed", ref baseSpeed, 0.01f);
            ImGui.DragFloat("Base Controller Speed", ref baseControllerSpeed, 0.01f);
        }

    }
}

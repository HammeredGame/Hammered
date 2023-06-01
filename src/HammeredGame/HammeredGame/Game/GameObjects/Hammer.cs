using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HammeredGame.Core;
using BEPUphysics.PositionUpdating;
using BEPUphysics.Entities;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using HammeredGame.Game.PathPlanning.Grid;
using HammeredGame.Game.PathPlanning.TurnSmoothing;
using Vector3 = Microsoft.Xna.Framework.Vector3; // How is it that this ambigouity results in an error after adding comments???
using Quaternion = Microsoft.Xna.Framework.Quaternion; // How is it that this ambigouity results in an error after adding comments???
using Microsoft.Xna.Framework.Audio;
using Aether.Animation;
using ImGuiNET;
using ImMonoGame.Thing;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core.Particles;
using Microsoft.Xna.Framework.Content;
using HammeredGame.Graphics;

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
    public class Hammer : GameObject, IImGui
    {
        public enum HammerState
        {
            WithCharacter,
            Dropped,
            Enroute
        }

        // Hammer specific variables
        public bool InputEnabled = true;

        private float maximumHammerSpeed = 150f;
        public float currentHammerSpeed { get; private set; } = 0.0f;
        private float hammerSpeedEscalationStep = 2f;

        private HammerState hammerState;

        public Vector3 OldPosition { get; private set; }

        private Player player;

        /// <summary>
        /// The <c>grid</c> instance is an imaginary partition of a (3D) orthogonal parallelepiped enclosing the whole
        /// (currently active) scene into identical (uniform) cubes.
        /// Its functionality is the computation of the shortest path between the hammer (<c>this</c>)
        /// and the Player instance <c>player</c> while avoiding any obstacles in the scene.
        /// </summary>
        private UniformGrid grid;

        private readonly Queue<BEPUutilities.Vector3> route = new();
        private BEPUutilities.Vector3 nextRoutePosition;

        //private List<SoundEffect> hammer_sfx = new List<SoundEffect>();
        //how long till trigger next sound
        //TimeSpan audioDelay = TimeSpan.Zero;

        public event EventHandler OnSummon;
        public event EventHandler OnCollision;
        public event EventHandler OnDrop;

        // When held by the player, we have a very specific rotation we want to keep (in addition to
        // the rotations of the player).
        private Quaternion rotationWhenHeldByPlayer = new Quaternion(0.050f, 0.250f, 0.576f, 0.777f);

        // Wind cutting trails
        private readonly ParticleSystem windParticles;

        // Dust particles when the hammer is dropped
        private readonly ParticleSystem dustParticles;

        private TimeSpan lastVibrationTimestamp;

        public Hammer(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity)
            : base(services, model, t, pos, rotation, scale, entity)
        {
            hammerState = HammerState.WithCharacter;

            windParticles = new(new ParticleSettings()
            {
                Model = Services.GetService<ContentManager>().Load<Model>("Meshes/Primitives/unit_icosphere"),
                Texture = Services.GetService<ContentManager>().Load<Texture2D>("Meshes/Primitives/1x1_white"),
                Duration = TimeSpan.FromMilliseconds(300),
                // We spawn many small ones to make it look sorta continuous
                MaxParticles = 500,
                MinStartSize = 0.4f,
                MaxStartSize = 0.4f,
                MinEndSize = 0.1f,
                MaxEndSize = 0.1f,
                // The particles get affected by the hammer velocity by a little bit
                EmitterVelocitySensitivity = 0.8f,
                MaxVerticalVelocity = 0,
                // Air particles shouldn't bounce off walls/ground, they should just go through them
                IgnoreCollisionResponses = true,
                AffectedByLight = false
            }, services.GetService<GraphicsDevice>(), services.GetService<ContentManager>(), ActiveSpace);


            dustParticles = new(new ParticleSettings()
            {
                Model = Services.GetService<ContentManager>().Load<Model>("Meshes/Primitives/unit_icosphere"),
                Texture = Services.GetService<ContentManager>().Load<Texture2D>("Meshes/Primitives/1x1_white"),
                Duration = TimeSpan.FromMilliseconds(700),
                MaxParticles = 15,
                MinStartSize = 1f,
                MaxStartSize = 2f,
                MinEndSize = 0.1f,
                MaxEndSize = 0.1f,
                EmitterVelocitySensitivity = 0f,
                MaxVerticalVelocity = 3f,
                MinVerticalVelocity = 0.5f,
                MinHorizontalVelocity = -5f,
                MaxHorizontalVelocity = 5f,
                // Air particles shouldn't bounce off walls/ground, they should just go through them
                IgnoreCollisionResponses = true,
                AffectedByLight = true
            }, services.GetService<GraphicsDevice>(), services.GetService<ContentManager>(), ActiveSpace);

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

                // Move rotation and position pivot to the bottom of the handle, so that the when
                // it's held, everything is animated with the handle as the pivot
                this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, (this.Entity as Box).HalfHeight, 0);

                this.AudioEmitter = new AudioEmitter();
                this.AudioEmitter.Position = this.Position;
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

                if (other.Tag is Door)
                {
                    ComputeShortestPath();
                }


                OnCollision?.Invoke(this, null);

                //Input input = Services.GetService<Input>();
                //if (input.GamePadState.IsConnected)
                //{
                //    input.VibrateController(0.75f, 0.75f);
                //    // TODO: Add asynchronous wait here? (to have the vibration last a little longer?)
                //    // DONE!
                //    Services.GetService<ScriptUtils>().WaitMilliseconds(50).ContinueWith((_) => input.StopControllerVibration());
                //}
            }
        }

        public void SetOwnerPlayer(Player player)
        {
            this.player = player;

            var hammerGroup = new CollisionGroup();
            var playerGroup = new CollisionGroup();
            CollisionGroupPair pair = new CollisionGroupPair(hammerGroup, playerGroup);
            CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            this.player.Entity.CollisionInformation.CollisionRules.Group = playerGroup;
            this.Entity.CollisionInformation.CollisionRules.Group = hammerGroup;
        }

        public void SetSceneUniformGrid(UniformGrid grid) { this.grid = grid; }

        // Update function (called every tick)
        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (hammerState == HammerState.WithCharacter && player != null)
            {
                // Get the player's bone index for the right hand
                int boneIndexForRightHand = player.Model.Bones["mixamorig:RightHand"].Index;

                // Retrieve the world space position for the hand by multiplying the bone-to-object
                // transformation and the object-to-world transformation. Order of multiplication
                // doesn't matter here since we're only using the Translation property which is additive.
                Position = (player.Animations.WorldTransforms[boneIndexForRightHand] * player.GetWorldMatrix()).Translation;

                // For the rotation, we have a specific order we want to follow, since Quaternion
                // multiplication builds on top of each other. First, we'll rotate it in the world
                // space player rotation, then in the new axis that we now treat as the
                // object-space, we'll do an object-space bone rotation, and in the new axis that's
                // now aligned with the bone, we'll do a custom tweaked-rotation so that the hammer
                // is held in the correct direction.
                Rotation = player.Rotation * Quaternion.CreateFromRotationMatrix(player.Animations.WorldTransforms[boneIndexForRightHand]) * rotationWhenHeldByPlayer;

                player.PlayerSpeedModifier = 1.0f;
                Services.GetService<Input>().StopControllerVibration();
            }

            // Get the input via keyboard or gamepad
            if (InputEnabled && player != null && screenHasFocus) HandleInput();

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
                    // this.Entity.LinearVelocity = hammerSpeed * (player.Entity.Position - Entity.Position);
                    BEPUutilities.Vector3 currentToNextPosition = this.nextRoutePosition - Entity.Position;
                    float distanceBetweenCurrentAndNext = currentToNextPosition.Length();
                    currentToNextPosition.Normalize();
                    // If the two points are too far apart
                    Vector3 oldPosition = Position;
                    if (distanceBetweenCurrentAndNext > 1.5) // Hard to find the "magic value" to achieve both natural turn smoothing and stability...
                    {
                        if (route.Count() > 0)
                        {
                            this.UpdateQuadraticBezierPosition();
                        }
                        else
                        {
                            //this.Entity.LinearVelocity = hammerSpeed * currentToNextPosition; // Hammer shaking in constant spot.

                            // Hammer following the player after the (initial) shortest path has been followed.
                            BEPUutilities.Vector3 VectorToCharacter = this.player.Entity.Position - this.Entity.Position; VectorToCharacter.Normalize();
                            currentHammerSpeed = Math.Min(currentHammerSpeed + hammerSpeedEscalationStep, maximumHammerSpeed);

                            this.Entity.LinearVelocity = currentHammerSpeed * VectorToCharacter;
                        }


                    }
                    // If the hammer hasn't reached its destination, travel towards the next position of the route.
                    else if (route.Count() > 0)
                    {
                        this.nextRoutePosition = route.Peek(); route.Dequeue();
                        if (route.Count() > 1) // There are more obstacles to avoid before achieving a straight path.
                            this.UpdateQuadraticBezierCurve();
                    }

                    // Make the hammer face the direction of travel (todo: this doesn't seem to work consistently?)
                    if (Entity.LinearVelocity != BEPUutilities.Vector3.Zero)
                    {
                        // Only add particles if there's a velocity (to avoid showing particles on a
                        // scripted teleport for example). We adjust the number of particles based
                        // on the distance from the position at the previous frame. This is a
                        // workaround because we can't use LinearVelocity (only represents accurate
                        // velocity for straight line paths) and we can't use currentHammerSpeed
                        // (also only for straight line paths), and BezierCurveVelocity seems to
                        // inaccurately return extremely small values too.
                        //
                        // The log function is used to scale the distance down to a reasonable
                        // amount that is good on both low speeds and high speeds.
                        AddWindParticles(gameTime, (int)(Math.Log((Position - OldPosition).Length()) * 50));

                        // Find the direction of travel, and a perpendicular vector to it
                        Vector3 towardTravel = Vector3.Normalize(Entity.LinearVelocity.ToXNA());
                        // Why is it named "tangent", even though it's perpendicular?
                        Vector3 tangent = Vector3.Cross(towardTravel, Vector3.Up);

                        if (tangent == Vector3.Zero)
                        {
                            // The first attempt at a tangent failed since we were going straight
                            // up, try a different vector -- we can't fail both since they're not parallel.
                            tangent = Vector3.Cross(towardTravel, Vector3.Right);
                        }

                        // Construct a transformation to rotate the hammer's up direction (the side
                        // with the heavy magic bit) towards the travelling direction. It'd be nice
                        // to construct a Quaternion directly from the forward and up direction
                        // (Unity can do that) but XNA doesn't seem to have that, so we have to do
                        // it the long way.
                        Matrix transform = Matrix.CreateFromAxisAngle(tangent, (float)(Math.PI + Math.Acos((float)Vector3.Dot(towardTravel, Vector3.Up))));

                        Rotation = Quaternion.Slerp(Rotation, Quaternion.CreateFromRotationMatrix(transform), 0.25f);
                    }

                    // Slight shake on the camera depending on the distance to the player
                    player.ActiveCamera.Shake(5f / Math.Max((Position - player.Position).Length(), 5), 0.2f);

                    // We also want to shake the controller a little bit like the camera, but
                    // calling VibtrateController on every frame is quite expensive -- so we'll use
                    // a timer and only call it every 0.5 seconds to update the shake value based on
                    // the distance to the player.
                    if (gameTime.TotalGameTime > lastVibrationTimestamp + TimeSpan.FromMilliseconds(100f))
                    {
                        // Maximum 0.6 shake, scaled by the distance to the player
                        Services.GetService<Input>().VibrateController(
                            0.6f / Math.Max(MathF.Log((Position - player.Position).Length()), 0.6f),
                            0.6f / Math.Max(MathF.Log((Position - player.Position).Length()), 0.6f));
                        lastVibrationTimestamp = gameTime.TotalGameTime;
                    }

                    // Slow down the player
                    player.PlayerSpeedModifier = 0.5f;
                }
            }

            this.AudioEmitter.Position = Position;
            this.windParticles.Update(gameTime);
            this.dustParticles.Update(gameTime);
            OldPosition = this.Position;
        }

        public void HandleInput()
        {
            Input input = Services.GetService<Input>();
            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (hammerState == HammerState.WithCharacter && UserAction.DropHammer.Pressed(input))
            {
                //hammerState = HammerState.Dropped;
                // Make it rotate so it's up-side down, with the heavy magic part on the bottom
                Rotation = player.Rotation * Quaternion.CreateFromYawPitchRoll(0, MathHelper.Pi, 0);
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
                Services.GetService<AudioManager>().Play3DSound("Audio/balanced/loud_fly", false, this.AudioEmitter, 1);


                this.currentHammerSpeed = 0f;

                // The hammer, when called back, will follow the shortest path from the point where it was dropped towards
                // the point the player called it FROM (it does not follow the player).
                ComputeShortestPath();

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

            //hammer_sfx[1].Play();
            Services.GetService<AudioManager>().Play3DSound("Audio/balanced/hammer_drop_b", false, this.AudioEmitter, 1);

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

                this.Entity.Position += new BEPUutilities.Vector3(0f, (this.Entity as Box).Height, 0f);
                // Only gravitational force being applied to the entity, velocity in the other
                // directions are zeroed out --> hammer is dropped, so it shouldn't move
                this.Entity.LinearVelocity = new BEPUutilities.Vector3(0, -98.1f, 0);

                // Normal collisions to ensure the physics engine solves collision constraint with
                // this entity --> Also, probably a cause for issues
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;
            }

            // We want to spawn some particles at the location of the ground. If we spawn it at the
            // entity location at this point, it'll still be in the air (plus will spawn at the
            // handle/origin, which is not the side that hits the ground). To solve this, we ray
            // cast towards the ground to find the probable point of contact.
            BEPUutilities.Ray ray = new(this.Entity.Position, BEPUutilities.Vector3.Down);

            // We only want to know if the ray hits something meaningful, so we'll filter out
            // anything that has NoSolver as its rule, or is a player or the hammer itself
            static bool filter(BEPUphysics.BroadPhaseEntries.BroadPhaseEntry entry) {
                return entry.CollisionRules.Personal != CollisionRule.NoSolver &&
                    entry.Tag is not Player &&
                    entry.Tag is not Hammer;
            }

            if (ActiveSpace.RayCast(ray, filter, out BEPUphysics.RayCastResult result))
            {
                for (int i = 0; i < 30; i++)
                {
                    dustParticles.AddParticle(result.HitData.Location.ToXNA(), Vector3.Zero);
                }
            }
        }

        public bool IsEnroute()
        {

            //sound effect instance to try and manipulate the pitch, but not working
            if (hammerState == HammerState.Enroute)
            {
                //SoundEffectInstance whoosh = hammer_sfx[2].CreateInstance();
                //whoosh.Play();

                //Services.GetService<AudioManager>().Play3DSound("Audio/balanced/catch_b", false, this.AudioEmitter, 1);

            }
            return hammerState == HammerState.Enroute;
        }

        public bool IsWithCharacter()
        {
            return (hammerState == HammerState.WithCharacter);
        }

        public void SetState(HammerState newState)
        {
            hammerState = newState;
        }

        /// <summary>
        /// Add wind particles based on the hammer outline.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="numberParticles">Number of particles to spawn</param>
        private void AddWindParticles(GameTime gameTime, int numberParticles)
        {
            // Don't spawn particles if the hammer has no velocity since we can't determine which
            // direction to spawn particles in
            if (this.Entity.LinearVelocity == BEPUutilities.Vector3.Zero)
                return;

            // Calculate the normal, tangent, and bitangent for a plane that has its normal pointing
            // towards the hammer's velocity. This will be used to spawn particles in a plane
            // perpendicular to the hammer's movement.
            Vector3 normal = Vector3.Normalize(Entity.LinearVelocity.ToXNA());
            Vector3 tangent = Vector3.Cross(normal, new Vector3(-normal.Z, normal.X, normal.Y));
            Vector3 bitangent = Vector3.Cross(tangent, normal);

            for (int i = 0; i < numberParticles; i++)
            {
                float startRadius = 2f;
                float dispersion = 10f;

                // Determine a random angle along the circumference of a circle
                float randomAngle = Random.Shared.NextSingle() * MathHelper.TwoPi;
                (float y, float x) = MathF.SinCos(randomAngle);

                // Since the position property of the hammer is located at the bottom of the handle,
                // but we're moving the hammer head-first in the direction of travel, we should
                // offset the air particles by the whole height.
                //
                // We also offset the particles negatively by a random amount along the normal
                // vector between the current position and the position at the previous frame, so
                // that we don't get visual "circular bursts" of particles, but rather a continuous
                // particle stream.
                Vector3 offset = ((Entity as Box).Height - (Random.Shared.NextSingle() * (Position - OldPosition).Length())) * normal;

                // The position is the hammer's position plus the random unit vector multiplied by the
                // starting radius.
                Vector3 particlePosition = Entity.Position.ToXNA() + offset + (startRadius * ((y * tangent) + (x * bitangent)));

                // The velocity is the opposite of the hammer's velocity (in a action-reaction kind
                // of effect) plus horizontal dispersion along the perpendicular plane so it spreads
                // out as time passes.
                Vector3 particleInfluenceVelocity = -Entity.LinearVelocity.ToXNA() + dispersion * ((y * tangent) + (x * bitangent));
                windParticles.AddParticle(particlePosition, particleInfluenceVelocity);
            }
        }

        private bool StraightLinePath(out Vector3[] route)
        {

            // Note: The straight line path planning does not include any smoothing submethods.
            // The only thing that might affect the behaviour of the hammer and make it seems unnatural is the
            // distance threshold for moving towards the next entry of the queue of positions (look "Update").
            Vector3 lineSegment = this.player.Position - this.Position;
            double lineSegmentLength = lineSegment.Length();
            lineSegment.Normalize(); // Make the vector denote a direction.

            LinkedList<Vector3> path = new LinkedList<Vector3>();

            path.AddLast(this.Position); // So that the path always includes at least one vertex.
            for (int i = 1; i <= Math.Ceiling(lineSegmentLength / this.grid.sideLength); i++)
            {
                Vector3 samplePoint = this.Position + i * this.grid.sideLength * lineSegment;
                // If there is at least one cell which is not available in the straight line,
                // then inform that a more complex path planning method is required.
                if (!this.grid.GetCellMark(samplePoint)) { route = path.ToArray();  return false; }
                path.AddLast(samplePoint);
            }
            // Reaching this point in the code means that there is a straight path available from the hammer to the player.
            // Therefore, as a last step, we add the transposition required to get from the sampled line to the actual position of the player.
            path.AddLast(this.player.Position);

            route = path.ToArray();
            return true;

        }

        private async void ComputeShortestPath()
        {
            // Precautiously empty the previous route.
            // It should be empty by the time it finishes its previous route, but just in case.
            this.route.Clear();

            // First find the straight line path so as to get the hammer moving and, in the meantime, find best path with A*.

            // Scenario 1: A straight line is achievable.
            // "[In Euclidean space] The shortest distance between two points is a straight line"
            // ~ Archimedes of Syracuse (Αρχιμήδης ο Συρακούσιος)
            // Therefore, if the shortest path is unobstructed, there is no reason to follow a more complex path planning scheme.
            Vector3[] straightLineRoute; bool straightPathAchievable = this.StraightLinePath(out straightLineRoute);
            /// <remarks> According to a few benchmarks, this takes an insignificat amount of time (a couple dozen ms at most).
            /// Good to have it.</remarks>
            // Smoothen the path.
            // For a linear path, this is equivalent to reducing the number of positions tracked.
            // As an additional benefit, this greatly reduces the "wiggly" motion of the hammer in the naive turn smoothing.
            straightLineRoute = this.grid.RoughShortestPathSmoothing(straightLineRoute);
            // Casting the trajectory into the appropriate (physics engine) type.
            for (int i = 0; i < straightLineRoute.Length; i++) { this.route.Enqueue(straightLineRoute[i].ToBepu()); }
            // Initialize the first position in 3D space to visit.
            this.nextRoutePosition = this.route.Peek(); this.route.Dequeue();

            // Scenario 2: A straight line is not achievable.
            // A more complex path planning scheme must be used.
            // Currently, "raw" A* has been implemented.
            // Programming note: as of the time of writing (23/04/2023), everything inside the "Run" is thread-safe.
            // As such, no errors should arise.
            Task aStarPathComputationTask = Task.CompletedTask;
            if (!straightPathAchievable)
            {
                aStarPathComputationTask = Task.Run(() =>
                {
                    Vector3[] aStarRoute = this.grid.FindShortestPathAStar(straightLineRoute.Last(), this.player.Position);
                    for (int i = 0; i < aStarRoute.Length; i++) { this.route.Enqueue(aStarRoute[i].ToBepu()); }
                });
            }
            /// <remarks>
            /// INSPIRATION FOR AS TO WHY THE ABOVE IS EXECUTED ASYNCHRONOUSLY (using C# "Tasks").
            ///
            /// Observation 1
            /// =============
            /// The call
            /// <c>Vector3[] aStarRoute = this.grid.FindShortestPathAStar(straightLineRoute.Last(), this.player.Position);</c>
            /// is very expensive (takes a few seconds to execute).
            /// This is true even in cases where A* algorithm completes almost instantly(e.g. 60ms or less).
            /// This is because time (seconds)are required to iterate through the data.
            ///
            /// Observation 2
            /// =============
            /// If an "if-else" structure is adopted i.e.
            /// IF straightPathAchievable => FOLLOW STRAIGHT PATH
            /// ELSE => EXECUTE A*
            /// then instant response from the game is achieved.
            ///
            /// Combining observrations 1 and 2
            /// ===============================
            /// The hammer may start travelling towards the straight line as much as it can.
            /// WHILE it is travelling in a straight line,
            /// the software should compute the non-linear continuation of the path by executing the A*algorithm.
            /// In this case:
            /// 1) the game is responsive(the hammer has started some path instantly)
            /// 2) the full path is computed in the background, without altering the game experience.
            /// </remarks>

            await aStarPathComputationTask;
            // QUADRATIC BEZIER
            UpdateQuadraticBezierCurve();

        }

        // QUADRATIC BEZIER CURVE SECTION for turn smoothing!

        // Variables of the "Hammer" required exclusively for the Hermite spline turn smoothing section.
        float t = 0;
        private BEPUutilities.Vector3 p0, p1, p2, pc;
        private double CurveLength; // The Bezier curve length is approximated as the sum of the two linear segments it is comprised of.

        private void UpdateQuadraticBezierCurve()
        {
            t = 0.0f;
            p0 = this.Entity.Position;
            pc = this.nextRoutePosition;
            p2 = this.route.Peek();
            p1 = BezierQuadraticSpline.MiddleControlPointComputation(p0, pc, p2);
            CurveLength = (pc - p0).Length() + (p2 - pc).Length();
        }

        private void UpdateQuadraticBezierPosition()
        {
            t += 0.012f;
            var previousPosition = this.Entity.Position;
            this.Entity.Position = BezierQuadraticSpline.QuadraticBezierPosition(p0, p1, p2, t);
            var temp = this.Entity.Position - previousPosition; temp.Normalize(); temp *= currentHammerSpeed;
            this.Entity.LinearVelocity = this.Entity.Position - previousPosition; // Approximating velocity with (forward) discrete differences.
            //this.Entity.LinearVelocity = temp;
        }

        private Vector3 UpdateQuadraticBezierVelocity()
        {
            // The following scheme is WRONG.
            // Two more thought came to mind:
            // 1) Try to use inner product sign. Isn't generally applicable...
            // Something along the lines of: Δx = v * Δtime <=> t * Curve length = v * Δtime <=> t = (v * Δtime) / curve length
            // I couldn't make it work.
            var distanceOfCurrentFromEnd = (p2 - this.Entity.Position).Length();
            var distanceOfCenterFromEnd = (p2 - pc).Length();
            float tempT;
            if (distanceOfCurrentFromEnd > distanceOfCenterFromEnd) // The point has not reached the center point (t=0.5) yet.
            {
                tempT = (float)((1 - (pc - this.Entity.Position).Length() + distanceOfCenterFromEnd) / CurveLength);
            }
            else
            {
                tempT = (float)(0.5 + distanceOfCurrentFromEnd / CurveLength);
            }

            return BezierQuadraticSpline.QuadraticBezierVelocity(p0, p1, p2, tempT).ToXNA();
        }

        public override void Draw(GameTime gameTime, Matrix view, Matrix projection, Vector3 cameraPosition, SceneLightSetup lights)
        {
            base.Draw(gameTime, view, projection, cameraPosition, lights);

            windParticles.CopyShadowMapParametersFrom(Effect);
            windParticles.Draw(gameTime, view, projection, cameraPosition, lights);

            dustParticles.CopyShadowMapParametersFrom(Effect);
            dustParticles.Draw(gameTime, view, projection, cameraPosition, lights);
        }

        public new void UI()
        {
            base.UI();
            ImGui.Separator();
            System.Numerics.Vector4 temp = rotationWhenHeldByPlayer.ToVector4().ToNumerics();
            ImGui.DragFloat4("Rotation when held", ref temp);
            rotationWhenHeldByPlayer = new Quaternion(Vector4.Normalize(temp));
        }
    }
}

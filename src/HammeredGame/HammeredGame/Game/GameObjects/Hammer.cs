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
using BEPUutilities;
using Vector3 = Microsoft.Xna.Framework.Vector3; // How is it that this ambigouity results in an error after adding comments???
using Quaternion = Microsoft.Xna.Framework.Quaternion; // How is it that this ambigouity results in an error after adding comments???
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
        private float hammerSpeed = 40f;
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

            //// Adding the grid reference to the hammer.
            ///// <remarks>
            ///// TODO:   Storing the grid into the <c>services</c> object is NOT an information-secure solution!
            /////         Find a better way to insert a parameter which will be used to instantiate <c>grid</c>.
            /////         <seealso cref="SceneDescriptionIO.ParseFromXML(string, GameServices)"/>
            /////         IMPACT ASSESSMENT LEVEL: 2 (critical)
            ///// </remarks>
            //this.grid = services.GetService<UniformGrid>();
            //services.RemoveService<UniformGrid>(); // Removing the "UniformGrid" service so that as little as classes possible can access it.
            /// Update: The grid reference is passed to the hammer via the <see cref="Scene.OnSceneStart"/> ("protected" function)
            /// <seealso cref="Hammer.SetSceneUniformGrid(UniformGrid)"/>
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

        public void SetSceneUniformGrid(UniformGrid grid) { this.grid = grid; }

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
                    // this.Entity.LinearVelocity = hammerSpeed * (player.Entity.Position - Entity.Position);
                    BEPUutilities.Vector3 currentToNextPosition = this.nextRoutePosition - Entity.Position;
                    float distanceBetweenCurrentAndNext = currentToNextPosition.Length();
                    currentToNextPosition.Normalize();
                    // If the two points are too far apart
                    if (distanceBetweenCurrentAndNext > 1)
                    {
                        //// Follow the path in line segments. Stable, but no curvature.
                        //this.Entity.LinearVelocity = hammerSpeed * currentToNextPosition;

                        //// VERY unstable natural curves!
                        //// The problem is that if the player does not stand still to catch the hammer,
                        //// it will take a significant amount of time to converge towards the final point!!!
                        //var temp = this.Entity.LinearVelocity;
                        //temp.Normalize(); temp += 0.2f * currentToNextPosition; temp.Normalize();
                        //temp *= hammerSpeed;
                        //this.Entity.LinearVelocity = temp;
                        if (route.Count() > 0)
                            this.UpdateQuadraticBezierPosition();
                            //this.UpdateQuadraticBezierVelocity();
                        else
                            this.Entity.LinearVelocity = hammerSpeed * currentToNextPosition;

                        //// Hermite curves smoothing. Currently not working!
                        //this.UpdateHammerPositionHermite(gameTime);
                        //this.UpdateHammerVelocityHermite(gameTime);


                    }
                    // If the hammer hasn't reached its destination, travel towards the next position of the route.
                    else if (route.Count() > 0)
                    {
                        this.nextRoutePosition = route.Peek(); route.Dequeue();
                        if (route.Count() > 1)
                            //this.UpdateHermiteCoefficients();
                            this.UpdateQuadraticBezierCurve();
                    }

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

                // The hammer, when called back, will follow the shortest path from the point where it was dropped towards
                // the point the player called it FROM (it does not follow the player).
                ComputeShortestPath();
                //UpdateHermiteCoefficients();
               
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
            for (int i = 0; i < straightLineRoute.Length; i++) { this.route.Enqueue(MathConverter.Convert(straightLineRoute[i])); }
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
                    for (int i = 0; i < aStarRoute.Length; i++) { this.route.Enqueue(MathConverter.Convert(aStarRoute[i])); }

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


            // QUADRATIC BEZIER
            await aStarPathComputationTask;
            UpdateQuadraticBezierCurve();



            // HERMITEAN VELOCITY...INCOMPLETE!
            //// Initialize velocity of the hammer (required for Hermitean Velocity)
            //var temp = this.nextRoutePosition - this.Entity.Position;
            //if (temp.Length() > 0) { temp.Normalize(); temp *= hammerSpeed; } // If the hammer has started moving
            //else {
            //    if (this.route.Count() == 0)
            //        await aStarPathComputationTask;
            //    this.route.Dequeue(); // The first element of the queue points towards the current position.
            //    temp = this.route.Peek() - this.nextRoutePosition;
            //    temp.Normalize(); temp *= hammerSpeed;

            //}
            //this.Entity.LinearVelocity = temp;

        }


        // QUADRATIC BEZIER CURVE SECTION for turn smoothing!

        // Variables of the "Hammer" required exclusively for the Hermite spline turn smoothing section.
        private BEPUutilities.Vector3 p0, p1, p2, pc;
        float t = 0;
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
            t += 0.015f;
            var previousPosition = this.Entity.Position;
            this.Entity.Position = BezierQuadraticSpline.QuadraticBezierPosition(p0, p1, p2, t);
            var temp = this.Entity.Position - previousPosition; temp.Normalize(); temp *= hammerSpeed;
            this.Entity.LinearVelocity = this.Entity.Position - previousPosition; // Approximating velocity with (forward) discrete differences.
        }

        private void UpdateQuadraticBezierVelocity()
        {
            // The following scheme is WRONG.
            // Two more thought came to mind:
            // 1) Try to use inner product sign. Isn't generally applicable...
            // Something along the lines of: Δx = v * Δtime <=> t * Curve length = v * Δtime <=> t = (v * Δtime) / curve length
            // I couldn't make it work.
            var distanceOfCurrentFromEnd = (p2 - this.Entity.Position).Length();
            var distanceOfCenterFromEnd = (p2 - pc).Length();
            if (distanceOfCurrentFromEnd > distanceOfCenterFromEnd) // The point has not reached the center point (t=0.5) yet.
            {
                t = (float) (( 1 - (pc - this.Entity.Position).Length() + distanceOfCenterFromEnd ) / CurveLength);
            }
            else
            {
                t = (float)(0.5 + distanceOfCurrentFromEnd / CurveLength);
            }

            this.Entity.LinearVelocity = BezierQuadraticSpline.QuadraticBezierVelocity(p0, p1, p2, t);

        }







        // HERMITE CUBIC SPLINE SECTION
        // STATUS: NOT WORKING...
        // STATUS: QUADRATIC BEZIER CURVES ARE USED INSTEAD (see above code section)!!!
        // The reason for (attempting to) incorporate Hermitean splines is for smoothing any turns in the path.

        /* Hermite Cubic Spline := (2(p0 - p1) + p0'+ p1')t^3 + (- 3(p0 - p1) - 2p0' - p1')t^2 + p0't + p0
         * Derivate w.r.t. t := 3(2(p0 - p1) + p0'+ p1')t^2 + 2(- 3(p0 - p1) - 2p0' - p1')t + p0'
         * POINTER:
         * The good thing about the above is that the terms in the parentheses can be PRECOMPUTED and change only the t value.
         * 
         * Transitioning from the simple curve to the data of the problem, we would have:
         * p0 := a zero-order derivative initial condition, i.e. the starting point of the current line segment
         * p1 := a zero-order derivative (initial) condition, i.e. the destination point of the current line segment
         * p0' := a first-order derivative initial condition at the start of the line segment.
         *        This would translate to the velocity of the hammer when it reaches the point.
         * p1' := a first-order derivative (initial) condition at the destination point of the line segment.
         *        This right here would depend on design choice.
         *        Personally, I make the decision on the overall task we wish to accomplish:
         *        make the already-computed linear segment path smoother.
         *        As such, the hammer should reach the destination point of the current linear segment,
         *        --which will be "transformed" into the starting point of the next linear segment--
         *        in such a way that its velocity is pointing towards the next position/point in the full path. 
         *        
         * What is difficult to update in MonoGame is the "t" variable, which parameterizes the curve.
        */

        // Variables of the "Hammer" required exclusively for the Hermite spline turn smoothing section.
        private BEPUutilities.Vector3[] HermitePositionCoefficients = new BEPUutilities.Vector3[4];
        private BEPUutilities.Vector3[] HermiteVelocityCoefficients = new BEPUutilities.Vector3[3];
        //private float t = 0;
        //private BEPUutilities.Vector3 CurveStart;
        // double CurveLength; // The hermitean curve length is approximated as the linear segment connecting the two points.


        private void UpdateHermiteCoefficients()
        {
            BEPUutilities.Vector3 p0 = this.Entity.Position, p1 = this.nextRoutePosition, m0 = this.Entity.LinearVelocity; //m0.Normalize();

            /* [...] Personally, I make the decision on the overall task we wish to accomplish:
             * make the already-computed linear segment path smoother.
             * As such, the hammer should reach the destination point of the current linear segment,
             * --which will be "transformed" into the starting point of the next linear segment--
             * in such a way that its velocity is pointing towards the next position / point in the full path.
            */
            var m1 = this.route.Peek() - this.nextRoutePosition; m1.Normalize(); m1 *= hammerSpeed;

            this.HermitePositionCoefficients = HermiteCubicSpline.HermiteCubicSplinePositionCoefficients(p0, p1, m0, m1);
            this.HermiteVelocityCoefficients = HermiteCubicSpline.HermiteCubicSplineVelocityCoefficients(p0, p1, m0, m1);
            t = 0.0f; // Reset t = 0 so as to consider the current position be the start of the new Hermitean spline.
            CurveLength = (this.nextRoutePosition - this.Entity.Position).Length();
        }

        private void UpdateHammerPositionHermite(GameTime gameTime)
        {
            //// Simulate a time step using Forward Euler integration. As it is known, this is unstable if the time step is not small enough.
            //var stepPosition = this.Entity.Position + this.Entity.LinearVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            //// Find the (approximate) t of the curve the hammer ends up in.
            //t = (float)((this.nextRoutePosition - stepPosition).Length() / CurveLength);
            t += 0.01f;
            // Correct the time step so that the hammer ends up on the exact spot on the curve.
            this.Entity.Position = HermiteCubicSpline.HermiteCubicSplinePosition(
                this.HermitePositionCoefficients[0], this.HermitePositionCoefficients[1],
                this.HermitePositionCoefficients[2], this.HermitePositionCoefficients[3],
                t
            );
        }

        private void UpdateHammerVelocityHermite(GameTime gameTime)
        {
            // THIS COMPUTATION IS PROBLEMATIC!
            // There may be multiple points on the Hermite curve having the same distance from the endpoint!
            t = (float)(1 - (this.nextRoutePosition - this.Entity.Position).Length() / CurveLength);
            // t += (float)gameTime.ElapsedGameTime.TotalSeconds;
            var ComputedHermiteVelocity = HermiteCubicSpline.HermiteCubicSplineVelocity(
                this.HermiteVelocityCoefficients[0], this.HermiteVelocityCoefficients[1],
                this.HermiteVelocityCoefficients[2], BEPUutilities.Vector3.Zero,
                t
            );
            ComputedHermiteVelocity.Normalize(); ComputedHermiteVelocity *= hammerSpeed;
            this.Entity.LinearVelocity = ComputedHermiteVelocity;
        }

    }
}

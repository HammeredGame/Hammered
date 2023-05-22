using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Paths.PathFollowing;
using BEPUphysics.PositionUpdating;
﻿using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HammeredGame.Game.GameObjects.Player;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework.Content;
using HammeredGame.Core.Particles;
using HammeredGame.Graphics;
using Pleasing;
using System.Reflection.Metadata;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles
{
    /// <summary>
    /// The <c>Tree</c> class is a movable obstacle within the game world, contextually
    /// reacting to the hammer and player interactions.
    /// <para />
    /// Trees have a <code>treeFallen</code> property specific to it, which keeps track of the current
    /// state of the tree.
    /// <para />
    /// Specifically, if the tree has not fallen (<code>treeFallen == false</code>):
    ///     --- the player will be fully blocked by the tree
    ///     --- the hammer (in the <code>Enroute</code> state will:
    ///         >>> set the tree to a fallen state
    ///         >>> Rotate the tree to represent it having fallen in the direction of the hammer movement
    /// <para />
    /// If the tree as already fallen (<code>treeFallen == true</code>):
    ///     --- push the player vertically (set the player's Y component) up a little,
    ///         if the player collides with the tree
    ///     --- set the player back to ground level, if the player does not collide with the tree anymore
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> MovableObstacle
    /// <para />
    /// The current implementation of the tree's interaction with the player after falling is defined as
    /// setting the player's Y position to the max Y value of the tree's bounding box. This works alright
    /// for a flat level, but this will have undesired effects when the puzzles have any kind of
    /// elevation introduced.
    /// <para />
    /// TODO: Implement a better way to handle adjusting the player's position, when traversing the
    /// tree surface.
    /// </remarks>

    public class Tree : MovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool treeFallen = false;
        private bool playerOnTree;

        private Model fallenLog;
        private Texture2D logTexture;
        private bool isFalling = false;
        private BEPUutilities.Vector3 fallDirection;
        private float fallingAngle = 0;

        private readonly ParticleSystem fallDustParticles;

        public Tree(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            fallenLog = services.GetService<ContentManager>().Load<Model>("Meshes/Trees/trunk");
            logTexture = services.GetService<ContentManager>().Load<Texture2D>("Meshes/Trees/trunk_texture");
            this.AudioEmitter = new AudioEmitter();
            this.AudioEmitter.Position = this.Position;

            // We want a dust cloud when the tree falls. This is done through a particle system.
            fallDustParticles = new ParticleSystem(new ParticleSettings()
            {
                // Icospheres (spheres with less vertices) are cheap to render and fit the low-poly
                // vibe, so we use them for the particles
                Model = services.GetService<ContentManager>().Load<Model>("Meshes/Primitives/unit_icosphere"),

                // The texture can be anything, but white generally looks best for dust cloud. This
                // is also affected by the environment lighting since particles render using the
                // same shader as everything else.
                Texture = services.GetService<ContentManager>().Load<Texture2D>("Meshes/Primitives/1x1_white"),
                Duration = TimeSpan.FromSeconds(4),

                // The start size will be random from very small to a meter large
                MinStartSize = 0.1f,
                MaxStartSize = 10,

                // But they will shrink to nothing over the course of their lifetime
                MinEndSize = 0f,
                MaxEndSize = 0f,

                // Particles should spawn with some X/Z velocity to spread out the cloud and show impact
                MinHorizontalVelocity = -10f,
                MaxHorizontalVelocity = 10f,

                // It shouldn't have a lot of vertical velocity, but a nudge is nice to make it look
                // like it is rising because of the tree's impact. Gravity won't affect them so
                // it'll be a nice smoke-like look.
                MinVerticalVelocity = 0f,
                MaxVerticalVelocity = 3f
            }, services.GetService<GraphicsDevice>(), services.GetService<ContentManager>(), ActiveSpace);

            // Set up physics stuff
            if (this.Entity != null)
            {
                if (this.Entity is not Box)
                {
                    throw new Exception("Tree only supports Box due to how it falls over");
                }
                this.Entity.Tag = "MovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;
                this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, (this.Entity as Box).HalfHeight, 0);
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
                this.ActiveSpace.Add(this.Entity);

                this.Entity.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;
                this.Entity.CollisionInformation.Events.PairTouching += Events_PairTouching;
            }
        }

        private void Events_PairTouching(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            // Start tree fall (currently falls 90 degrees in the direction of hammer movement)
            if (other.Tag is Hammer && (!treeFallen && !isFalling))
            {
                var hammer = other.Tag as Hammer;

                if (hammer.IsEnroute())
                {
                    // TODO: de-duplicate the same exact code in Events_PairTouching and Events_InitialCollisionDetected
                    if (hammer.Entity.LinearVelocity.Length() > hammer.hammerSpeed - 1f &&
                            hammer.Entity.LinearVelocity.Length() < hammer.hammerSpeed + 1f)
                    {
                        fallDirection = hammer.Entity.LinearVelocity;
                        fallDirection.Normalize();

                        // Interpolate the rotation of the tree to fall over with an elastic tween
                        Tweening.Tween(angle =>
                        {
                            fallingAngle = angle;
                            Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.Cross(BEPUutilities.Vector3.Up, fallDirection), BEPUutilities.MathHelper.ToRadians(fallingAngle));
                        }, 0f, 90f, 600, Easing.Elastic.In, LerpFunctions.Float);

                        Entity.Position += 10f * fallDirection;
                        isFalling = true;
                        Services.GetService<AudioManager>().Play3DSound("Audio/tree_fall", false, this.AudioEmitter, 1);
                    }
                }

            }

            //if (other.Tag is Player && treeFallen)
            //{
            //    var player = other.Tag as Player;
            //    this.playerOnTree = true;
            //    player.Entity.Position = new BEPUutilities.Vector3(player.Entity.Position.X, player.Entity.Position.Y + 0.1f, player.Entity.Position.Z);
            //}
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender,
            BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            // Start tree fall (currently falls 90 degrees in the direction of hammer movement)
            if (other.Tag is Hammer && (!treeFallen && !isFalling))
            {
                var hammer = other.Tag as Hammer;

                if (hammer.IsEnroute())
                {
                    if (hammer.Entity.LinearVelocity.Length() > hammer.hammerSpeed - 1f &&
                            hammer.Entity.LinearVelocity.Length() < hammer.hammerSpeed + 1f)
                    {
                        // Determine the direction of the fall as the hammer's direction
                        fallDirection = hammer.Entity.LinearVelocity;
                        fallDirection.Normalize();

                        // Interpolate the rotation of the tree to fall over with an elastic tween
                        Tweening.Tween(angle =>
                        {
                            fallingAngle = angle;
                            Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.Cross(BEPUutilities.Vector3.Up, fallDirection), BEPUutilities.MathHelper.ToRadians(fallingAngle));
                        }, 0f, 90f, 600, Easing.Elastic.In, LerpFunctions.Float);

                        Entity.Position += 10f * fallDirection;
                        isFalling = true;
                        Services.GetService<AudioManager>().Play3DSound("Audio/tree_fall", false, this.AudioEmitter, 1);
                    }
                }

            }

            // If tree is fallen, player can walk on top of the tree
            // Currently designed as: player's Y = maxY + bbox width
            // maxY calculated as the max of either player's current Y or
            // the contact position's Y
            if (other.Tag is Player && treeFallen)
            {
                var player = other.Tag as Player;
                if (player.StandingOn != PlayerOnSurfaceState.OnTree)
                {
                    float maxY = player.Entity.Position.Y;
                    foreach (var contact in pair.Contacts)
                    {
                        BEPUutilities.Vector3 pointOfContact = contact.Contact.Position;
                        maxY = Math.Max(maxY, pointOfContact.Y);
                    }

                    player.StandingOn = PlayerOnSurfaceState.OnTree;
                    player.Entity.Position = new BEPUutilities.Vector3(player.Entity.Position.X, maxY + (this.Entity as Box).HalfWidth, player.Entity.Position.Z);
                }
            }

            // If tree is fallen, move block can slide on top of the tree
            // Currently designed as: move block's Y = maxY + bbox width
            // maxY calculated as the max of either move block's current Y or
            // the contact position's Y
            if (other.Tag is MoveBlock && treeFallen)
            {
                var moveblock = other.Tag as MoveBlock;
                if (moveblock.MgroundState != MoveBlock.MBGroundState.Tree)
                {
                    float maxY = moveblock.Entity.Position.Y;
                    foreach (var contact in pair.Contacts)
                    {
                        BEPUutilities.Vector3 pointOfContact = contact.Contact.Position;
                        maxY = Math.Max(maxY, pointOfContact.Y);
                    }

                    moveblock.MgroundState = MoveBlock.MBGroundState.Tree;
                    moveblock.Entity.Position = new BEPUutilities.Vector3(moveblock.Entity.Position.X, maxY + (this.Entity as Box).HalfWidth, moveblock.Entity.Position.Z);
                }
            }
        }

        public void SetTreeFallen(bool treeFallen)
        {
            this.treeFallen = treeFallen;
            if (this.Entity is Box box)
            {
                // Spawn particles to simulate some dust clouds so we can hide the tree model swapping
                for (int i = 0; i < 25; i++) {
                    // Spawn them with a random offset along the height and width of the tree trunk
                    float offsetAlongTree = MathHelper.Lerp(0, box.Height + 5, (float)Random.Shared.NextDouble());
                    float offsetPerpTree = MathHelper.Lerp(-box.HalfWidth, box.HalfWidth, (float)Random.Shared.NextDouble());

                    Vector3 alongTree = MathConverter.Convert(fallDirection) * offsetAlongTree;
                    Vector3 alongPerpendicular = Vector3.Cross(Vector3.Up, MathConverter.Convert(fallDirection)) * offsetPerpTree;

                    // Spawn a particle, passing in the tree's velocity as the parent influencing
                    // velocity. It should be zero so it doesn't really matter.
                    fallDustParticles.AddParticle(Position + alongTree + alongPerpendicular, MathConverter.Convert(Entity.LinearVelocity));
                }

                box.Width *= 1.2f;
                box.Length *= 1.2f;
                //this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
            }

            // Swap models
            this.Model = fallenLog;
            this.Texture = logTexture;
        }

        public bool IsTreeFallen()
        {
            return this.treeFallen;
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // When the tree falls, it starts a tweened animation to rotate 90 degrees. We check for
            // when it's done here and set the tree to fallen.
            if (isFalling && fallingAngle >= 90f)
            {
                SetTreeFallen(true);
                isFalling = false;
            }

            // Update the particles
            fallDustParticles.Update(gameTime);
        }

        public bool IsPlayerOn()
        {
            return this.playerOnTree;
        }

        /// <summary>
        /// Custom override of Draw to also draw dust particles after drawing the tree.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="lights"></param>
        public override void Draw(GameTime gameTime, Matrix view, Matrix projection, Vector3 cameraPosition, SceneLightSetup lights)
        {
            base.Draw(gameTime, view, projection, cameraPosition, lights);
            fallDustParticles.Draw(gameTime, view, projection, cameraPosition, lights);
        }
    }
}

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

        private List<SoundEffect> tree_sfx;

        private Model fallenLog;
        private Texture2D logTexture;
        private bool isFalling = false;
        private BEPUutilities.Vector3 fallDirection;
        private int fallingAngle = 0;

        public Tree(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {

                tree_sfx = Services.GetService<List<SoundEffect>>();

                fallenLog = services.GetService<ContentManager>().Load<Model>("Meshes/Trees/trunk"); 
                logTexture = services.GetService<ContentManager>().Load<Texture2D>("Meshes/Trees/trunk_texture");

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
                //this.Entity.CollisionInformation.Events.CollisionEnded += Events_CollisionEnded;
                //this.Entity.CollisionInformation.Events.RemovingPair += Events_RemovingPair;
                this.AudioEmitter = new AudioEmitter();
                this.AudioEmitter.Position = this.Position;
            }
        }

        //private void Events_RemovingPair(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.BroadPhaseEntry other)
        //{
        //    if (other.Tag is Player && treeFallen)
        //    {
        //        var player = other.Tag as Player;
        //        this.playerOnTree = false;
        //        player.Entity.Position = new BEPUutilities.Vector3(player.Entity.Position.X, 0.0f, player.Entity.Position.Z);
        //    }
        //}

        private void Events_PairTouching(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
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
                        fallDirection = hammer.Entity.LinearVelocity;
                        fallDirection.Normalize();
                        isFalling = true;
                        //tree_sfx[3].Play();
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
                        fallDirection = hammer.Entity.LinearVelocity;
                        fallDirection.Normalize();
                        isFalling = true;
                        //tree_sfx[3].Play();
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
        }

        public void SetTreeFallen(bool treeFallen)
        {
            this.treeFallen = treeFallen;
            if (this.Entity != null)
            {
                (this.Entity as Box).Width *= 1.2f;
                (this.Entity as Box).Length *= 1.2f;
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
            if(isFalling && fallingAngle < 90)
            {
                // Update falling position until complete 90 degrees rotation
                fallingAngle += (int)(0.3 * gameTime.ElapsedGameTime.Milliseconds);

                if (fallingAngle > 90) fallingAngle = 90;

                this.Entity.Orientation = BEPUutilities.Quaternion.Identity *
                    BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.Cross(BEPUutilities.Vector3.Up, fallDirection),
                    BEPUutilities.MathHelper.ToRadians(fallingAngle));

                if(fallingAngle >= 90)
                {
                    SetTreeFallen(true);
                    isFalling = false;
                }
            }
        }

        //public override void TouchingHammer(Hammer hammer)
        //{
        //    if (!treeFallen)
        //    {
        //        SetTreeFallen(true);
        //        Vector3 fallDirection = hammer.Position - hammer.OldPosition;
        //        fallDirection.Normalize();
        //        this.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, fallDirection), MathHelper.ToRadians(90));
        //        //this.position += new Vector3(0.0f, 20.0f, 0.0f);
        //        //System.Diagnostics.Debug.WriteLine(Vector3.UnitZ);
        //    }
        //}

        public bool IsPlayerOn()
        {
            return this.playerOnTree;
        }

        //public override void TouchingPlayer(Player player)
        //{
        //    if (!treeFallen)
        //    {
        //        base.TouchingPlayer(player);
        //    }
        //    else
        //    {
        //        //player.OnTree = true;
        //        this.playerOnTree = true;
        //        player.Position.Y = 3.0f; // this.BoundingBox.Max.Y; //- this.boundingBox.Min.Y;
        //    }
        //}

        //public override void NotTouchingPlayer(Player player)
        //{
        //    if (treeFallen)
        //    {
        //        //System.Diagnostics.Debug.WriteLine("OFF TREE");
        //        //player.OnTree = false;
        //        this.playerOnTree = false;

        //        if (!player.OnTree)
        //            player.Position.Y = 0.0f;
        //    }
        //}
    }
}

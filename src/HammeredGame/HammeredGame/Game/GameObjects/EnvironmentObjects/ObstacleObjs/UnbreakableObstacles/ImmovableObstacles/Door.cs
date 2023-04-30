using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
﻿using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using BEPUphysics.Entities;
using HammeredGame.Game.GameObjects.EmptyGameObjects;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    /// <summary>
    /// The <c>Door</c> class is an immovable obstacle within the game world, blocking the player's
    /// access to other parts of the map.
    /// <para />
    /// Doors have a <code>keyFound</code> property that indicates whether the player has found
    /// the associated key (that will open the door). If this property has been successfully set
    /// (player has key) and the player interacts with the door, the door will open up and cease
    /// blocking the player's path.
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    /// The current implementation of the door opening up just sets the door's visible status to false,
    /// resulting in it not being drawn on screen anymore and not considered for any further collisions.
    /// In the future, maybe we should be applying some form of animation, instead of it abruptly
    /// disappearing.
    /// <para />
    /// Temporarily, including a parameter <code>isGoal</code> (for the purposes of the demo
    /// submission). This indicates whether the door is the goal of the level - results in the
    /// "PUZZLE SOLVED" text to appear on the debug console (see Player.cs).
    /// <para />
    /// NOTE: THIS WILL NOT SCALE FOR FUTURE LEVELS!!!
    /// <para />
    /// TODO: remove the isGoal from this class. Potentially, make another class that exclusively
    /// handles the goal state - possibly an empty game object that does not get rendered, still
    /// checks for player collisions, and if the player makes it here with the necessary conditions
    /// satisfied, we trigger a cutscene/load next level/etc.
    /// </remarks>

    public class Door : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool keyFound = false;
        private bool isGoal = false; // TEMPORARY
        public Door(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                //this.Entity = new Box(MathConverter.Convert(this.Position), 5, 10, 3);
                this.Entity.Tag = "ImmovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
                this.ActiveSpace.Add(this.Entity);

                this.Entity.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;
                
                this.AudioEmitter = new AudioEmitter();
                this.AudioEmitter.Position = this.Position; 
            }
        }

        public void SetIsGoal(bool isGoal)
        {

            this.isGoal = isGoal;
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!

                // If hammer collides with door, set hammer to dropped state
                // This should only happen when hammer is called back
                if (other.Tag is Hammer)
                {
                    var hammer = other.Tag as Hammer;
                    hammer.DropHammer();
                }

                // If player collides with door and player has collected corresponding key
                // Door disappears (is opened) and the collision box associated with the
                // door is removed from the physics space
                if (other.Tag is Player && this.keyFound)
                {
                    var player = other.Tag as Player;
                    player.ReachedGoal = isGoal; // TEMPORARY
                    //this.Visible = false;
                    //this.ActiveSpace.Remove(sender.Entity);
                    this.OpenDoor();
                }

            }
        }

        public void SetKeyFound(bool keyFound)
        {
            this.keyFound = keyFound;
        }

        public void OpenDoor()
        {
            if (this.ActiveSpace.Entities.Contains(this.Entity) && this.Entity != null)
            {
                this.Visible = false;
                this.ActiveSpace.Remove(this.Entity);
                // Uncomment the following line if we do not wish the hammer to dodge the door, but instead collide with it.
                //this.CurrentScene.UpdateSceneGrid(this, true);
            }
        }

        public void CloseDoor()
        {
            if (!this.ActiveSpace.Entities.Contains(this.Entity) && this.Entity != null)
            {
                this.Visible = true;
                this.ActiveSpace.Add(this.Entity);
                // Uncomment the following line if we do not wish the hammer to dodge the door, but instead collide with it.
                //this.CurrentScene.UpdateSceneGrid(this, false);
            }
        }

        //public override void TouchingPlayer(Player player)
        //{
        //    if (keyFound)
        //    {
        //        player.ReachedGoal = isGoal; // TEMPORARY
        //        this.SetVisible(false);
        //    }
        //    base.TouchingPlayer(player);
        //}
    }
}

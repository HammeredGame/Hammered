using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using HammeredGame.Game.Screens;
using System;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables
{
    /// <summary>
    /// The <c>Ladder</c> class refers to an unmoving object that will be attached to a wall.
    /// When the player comes into contact with its bounding box, they will be prompted to hit an interact button.
    /// If the player hits the interact button, while still in contact with the ladder's bounding box,
    /// they will be teleported to the top, effectively having the player "climb" the ladder.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/> ---> <see cref="ImmovableInteractable "/>
    ///                         ---> <see cref="Ladder"/>
    /// </para>
    /// Currently, assuming that the Box entity attached the ladder will be constructed large enough, such that
    /// the player will be prompted to climb when they are a certain distance from the ladder.
    /// </remarks>
    public class Ladder : ImmovableInteractable
    {
        // Store the top and bottom position of ladder, represented by Empty GameObjects.
        // Used to teleport player
        EmptyGameObject topPosition, bottomPosition;

        public Ladder(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "ImmovableInteractableBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.ActiveSpace.Add(this.Entity);
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                this.Entity.CollisionInformation.Events.PairTouching += this.Events_PairTouching;
                this.Entity.CollisionInformation.Events.CollisionEnded += this.Events_CollisionEnded;
            }
        }

        private void Events_PairTouching(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!
                if (other.Tag is Player)
                {
                    // TODO: Set a variable in the scene to show prompt for "Interact"

                    // If the interact button has been pressed, while the player/ladder 
                    // contact pair is active - teleport player to top/bottom of ladder.
                    Input input = Services.GetService<Input>();
                    var player = other.Tag as Player;
                    if (player != null && UserAction.Interact.Pressed(input))
                    {
                        double distToTop = (player.Entity.Position - topPosition.Entity.Position).Length();
                        double distToBot = (player.Entity.Position - bottomPosition.Entity.Position).Length();

                        if (distToBot > distToTop) player.Entity.Position = bottomPosition.Entity.Position;
                        else player.Entity.Position = topPosition.Entity.Position;
                    }
                }
            }
        }

        private void Events_CollisionEnded(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!
                if (other.Tag is Player)
                {
                    // TODO: Set a variable in the scene to stop showing prompt for "Interact"
                }
            }

        }

        public void SetLadderEndpoints(EmptyGameObject top, EmptyGameObject bottom)
        {
            topPosition = top;
            bottomPosition = bottom;
        }

        //public override void Update(GameTime gameTime, bool screenHasFocus)
        //{            
        //}
    }
}

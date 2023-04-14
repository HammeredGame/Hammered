using BEPUphysics.Entities;
using BEPUphysics.PositionUpdating;
using BEPUphysics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects.EmptyGameObjects
{
    /// <summary>
    /// The <c>TriggerObject</c> class represents an empty game object that can be used to
    /// trigger custom events, as defined by the level.
    /// <para />
    /// </summary>
    /// <remarks>
    /// REMINDER (class tree): GameObject -> EmptyGameObject -> TriggerObject
    /// <para />
    /// TODO: Need to add the logic to handle custom event triggering.
    /// </remarks>
    public class TriggerObject : EmptyGameObject
    {

        public event EventHandler OnTrigger;

        public TriggerObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            this.Entity.Tag = "TriggerObjectBounds";
            this.Entity.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;
        }

        // Event Handler to initiate triggered in game event
        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            if (other.Tag is Player)
            {
                OnTrigger?.Invoke(this, null);
            }
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }

    }
}

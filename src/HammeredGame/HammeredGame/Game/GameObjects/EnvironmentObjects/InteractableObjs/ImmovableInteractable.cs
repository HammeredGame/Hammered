using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs
{
    /// <summary>
    /// <para>
    /// The <c>ImmovableInteractable</c> class refers to all <see cref="InteractableObject"/> instances
    /// which are not <see cref="CollectibleInteractable"/>.
    /// As such, it is expected that:
    /// 1) they are always present in the scene
    /// 2) they do not move
    /// 3) their change in the environment is in effect only while the <c>ImmovableInteractable</c> instance
    ///     is being interacted with
    /// </para>
    /// <para>
    /// The specific change in environment is to be defined inside classes which inherit from this class.
    /// </para>
    /// </summary>
    ///

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/> ---> <see cref="ImmovableInteractable"/>
    /// </para>
    /// </remarks>
    public class ImmovableInteractable : ObstacleObject
    {
        // Any Unbreakable Obstacle specific variables go here


        public ImmovableInteractable(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }
    }
}

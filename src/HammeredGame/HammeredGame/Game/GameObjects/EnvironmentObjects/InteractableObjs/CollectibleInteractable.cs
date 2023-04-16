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
    /// The <c>CollectibleInteractable</c> refers to items in the environment which are collected by the character once they
    /// interact with the former (usually by the character being in the same space as the items).
    /// </para>
    /// <para>
    /// By definition, it would be expected that once the <c>CollectibleInteractable</c> instance is collected by the
    /// character, the instance is not re-drawn in the map (possibly releasing the instance from memory) and a variable
    /// which indicates the collection of the item is altered.
    /// </para>
    /// <para>
    /// The specific change in environment and the ways for the <c>CollectibleInteractable</c> instance
    /// to be collected and triggered is to be defined inside classes which inherit from this class.
    /// </para>
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject"/> ---> <see cref="EnvironmentObject"/> --->
    ///                         <see cref="InteractableObject"/> ---> <see cref="CollectibleInteractable"/>
    /// </para>
    /// </remarks>
    public class CollectibleInteractable : ObstacleObject
    {
        // Any Unbreakable Obstacle specific variables go here

        public CollectibleInteractable(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}

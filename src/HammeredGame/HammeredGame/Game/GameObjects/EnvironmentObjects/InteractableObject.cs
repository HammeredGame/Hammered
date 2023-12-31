﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>InteractableObject</c> class refers to any element (possibly non-tangible even?) of the environment
    /// which triggers changes in a different <c>EnvironmentObject</c>.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/>
    /// </para>
    /// <para>
    /// TODO: Motivated by the definition provided above, it may be worth considering enriching the constructor of this class
    /// so as to always include another parameter:
    /// the EnvironmentObject which is triggered by the current <c>InteractableObject</c> instance.
    /// </para>
    /// </remarks>
    class InteractableObject : EnvironmentObject
    {
        // Any Interactable specific variables go here

        public InteractableObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }
    }
}

﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects
{
    public class EnvironmentObject : GameObject
    {
        /// <value>
        /// TODO: Consider renaming the field <code>IsGround</code> as "IsWalkable"?
        /// </value>
        public bool IsGround = false;
        public EnvironmentObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity)
            : base (services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }


        ///// <summary>
        ///// Called on game updates if the object is colliding with the global hammer object.
        ///// </summary>
        ///// <param name="hammer"></param>
        //public virtual void TouchingHammer(Hammer hammer) { }

        ///// <summary>
        ///// Called on game updates if the object is not colliding with the global hammer object.
        ///// </summary>
        ///// <param name="hammer"></param>
        //public virtual void NotTouchingHammer(Hammer hammer) { }

        ///// <summary>
        ///// Called on game updates if the object is colliding with the global player object.
        ///// </summary>
        ///// <param name="player"></param>
        //public virtual void TouchingPlayer(Player player) { }

        ///// <summary>
        ///// Called on game updates if the object is not colliding with the global player object.
        ///// </summary>
        ///// <param name="player"></param>
        //public virtual void NotTouchingPlayer(Player player) { }
    }
}

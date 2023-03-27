using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>FloorObject</c> class refers to any continuous surface the character (<see cref="Player"/>) may step on to
    /// traverse the level.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/> ---> <see cref="FloorObject "/> 
    /// </para>
    /// <para>
    /// TODO: Currently, the <see cref="Player"/> is responsible for handling their own movement
    /// according to the <c>FloorObject</c> they interact with <see cref="Player.Update(GameTime)"/>.
    /// This contradicts the method followed with other kinds of objects (e.g. <see cref="EnvironmentObject.TouchingPlayer(Player)"/>).
    /// An approach conforming to the previous example may be considered.
    /// NOTE: It is highly probable that an (external) physics library will be integrated into the project,
    /// which will possibly (among other things) handle cases such as these, or at least ease the implementation of it.
    /// </para>
    /// </remarks>

    class FloorObject : EnvironmentObject
    {
        // Any Interactable specific variables go here

        public FloorObject(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
            IsGround = true;
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}

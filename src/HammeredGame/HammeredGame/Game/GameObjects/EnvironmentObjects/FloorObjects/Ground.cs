using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Game.GameObjects.EnvironmentObjects;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using Hammered_Physics.Core;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects
{

    /// <summary>
    /// The <c>Ground</c> class refers to solid ground the character (<see cref="Player"/> may step on.
    /// Free movement of the character along the ground is permitted.
    /// </summary>

    /// <remarks>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="FloorObject "/> ---> <see cref="Ground"/>
    /// </remarks>
    class Ground : FloorObject
    {
        public Ground(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale) : base(services, model, t, pos, rotation, scale)
        {
            BEPUutilities.Vector3[] vertices;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(this.Model, out vertices, out indices);
            // Give the mesh information to a new StaticMesh.
            var mesh = new StaticMesh(vertices, indices, new BEPUutilities.AffineTransform(MathConverter.Convert(this.Position)));

            //Add it to the space!
            ActiveSpace.Add(mesh);
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}


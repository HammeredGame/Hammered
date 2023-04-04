using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;
using HammeredGame.Game.GameObjects.EnvironmentObjects;
using HammeredGame.Game;
using BEPUphysics.BroadPhaseEntries;
using BEPUutilities;
using Hammered_Physics.Core;
using BEPUphysics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects
{
    /// <summary>
    /// The <c>Ground</c> class refers to a water surface the character (<see cref="Player"/> may encounter.
    /// Movement towards or unto it is strictly prohibited.
    /// </summary>


    /// <remarks>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="FloorObject "/> ---> see<see cref="Water"/>
    /// </remarks>
    class Water : FloorObject
    {
        public Water(Model model, Microsoft.Xna.Framework.Vector3 pos, float scale, Texture2D t, Space space) : base(model, pos, scale, t, space)
        {
            BEPUutilities.Vector3[] vertices;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(this.Model, out vertices, out indices);
            //Give the mesh information to a new StaticMesh.  
            var mesh = new StaticMesh(vertices, indices, new AffineTransform(MathConverter.Convert(this.Position)));
            this.ActiveSpace.Add(mesh);
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HammeredGame.Game.GameObjects.Player;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles
{
    /// <summary>
    /// The <c>Tree</c> class is a movable obstacle within the game world, contextually
    /// reacting to the hammer and player interactions. 
    /// <para />
    /// Trees have a <code>treeFallen</code> property specific to it, which keeps track of the current
    /// state of the tree. 
    /// <para />
    /// Specifically, if the tree has not fallen (<code>treeFallen == false</code>):
    ///     --- the player will be fully blocked by the tree
    ///     --- the hammer (in the <code>Enroute</code> state will:
    ///         >>> set the tree to a fallen state
    ///         >>> Rotate the tree to represent it having fallen in the direction of the hammer movement
    /// <para />
    /// If the tree as already fallen (<code>treeFallen == true</code>):
    ///     --- push the player vertically (set the player's Y component) up a little, 
    ///         if the player collides with the tree
    ///     --- set the player back to ground level, if the player does not collide with the tree anymore
    /// </summary>
    /// 
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> MovableObstacle
    /// <para />
    /// The current implementation of the tree's interaction with the player after falling is defined as 
    /// setting the player's Y position to the max Y value of the tree's bounding box. This works alright
    /// for a flat level, but this will have undesired effects when the puzzles have any kind of 
    /// elevation introduced. 
    /// <para />
    /// TODO: Implement a better way to handle adjusting the player's position, when traversing the 
    /// tree surface.
    /// </remarks>

    public class Tree : MovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool treeFallen;

        public Tree(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
            treeFallen = false;
        }

        public void SetTreeFallen(bool treeFallen)
        {
            this.treeFallen = treeFallen;
        }

        public override void TouchingHammer(Hammer hammer)
        {
            if (!treeFallen)
            {
                SetTreeFallen(true);
                Vector3 fallDirection = hammer.Position - hammer.OldPosition;
                fallDirection.Normalize();
                this.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, fallDirection), MathHelper.ToRadians(90));
                //this.position += new Vector3(0.0f, 20.0f, 0.0f);
                //System.Diagnostics.Debug.WriteLine(Vector3.UnitZ);
            }
        }

        public override void TouchingPlayer(Player player)
        {
            if (!treeFallen)
            {
                base.TouchingPlayer(player);
            }
            else
            {
                player.OnTree = true;
                player.Position.Y = this.BoundingBox.Max.Y; //- this.boundingBox.Min.Y;
            }
        }

        public override void NotTouchingPlayer(Player player)
        {
            if (treeFallen)
            {
                //System.Diagnostics.Debug.WriteLine("OFF TREE");
                player.OnTree = false;
                player.Position.Y = 0.0f;
            }
        }
    }
}

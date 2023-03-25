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
    public class Tree : MovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool _treeFallen;

        public Tree(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
            _treeFallen = false;
        }

        public void setTreeFallen(bool treeFallen)
        {
            _treeFallen = treeFallen;
        }

        public override void TouchingHammer(Hammer hammer)
        {
            if (!_treeFallen)
            {
                setTreeFallen(true);
                Vector3 fallDirection = hammer.Position - hammer.OldPosition;
                fallDirection.Normalize();
                this.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, fallDirection), MathHelper.ToRadians(90));
                //this.position += new Vector3(0.0f, 20.0f, 0.0f);
                //System.Diagnostics.Debug.WriteLine(Vector3.UnitZ);

                //System.Diagnostics.Debug.WriteLine(Vector3.Cross(Vector3.Up, fallDirection));
                //this.additionalTransformation = Matrix.CreateTranslation(Vector3.Zero) * rotationMatrix; // * Matrix.CreateTranslation(this.position);
            }
        }

        public override void TouchingPlayer(Player player)
        {
            if (!_treeFallen) 
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
            if (_treeFallen)
            {
                //System.Diagnostics.Debug.WriteLine("OFF TREE");
                player.OnTree = false;
                player.Position.Y = 0.0f;
            }
        }
    }
}

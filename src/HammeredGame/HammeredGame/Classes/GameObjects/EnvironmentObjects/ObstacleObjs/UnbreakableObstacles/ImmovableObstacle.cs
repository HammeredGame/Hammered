using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Classes.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles
{
    public class ImmovableObstacle : UnbreakableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here

        public ImmovableObstacle(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
        }
    }
}



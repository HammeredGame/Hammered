using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Classes.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;

namespace HammeredGame.Classes.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables
{
    public class Key : CollectibleInteractable
    {
        /* Provisionally 
        */
        private Door _correspondingDoor;
        public Key(Model model, Vector3 pos, float scale, Camera cam, Texture2D t, Door correspondingDoor)
            : base(model, pos, scale, cam, t)
        {
            _correspondingDoor = correspondingDoor;
        }

        public override void hitByPlayer(Player player)
        {
            //this.activateTrigger();
            _correspondingDoor.setKeyFound(true);
            this.setVisible(false);
        }

    }
}

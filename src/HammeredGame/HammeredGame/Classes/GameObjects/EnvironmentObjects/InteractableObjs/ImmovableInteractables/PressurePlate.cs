using HammeredGame.Classes.GameObjects.EnvironmentObjects.ObstacleObjs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Classes.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables
{
    public class PressurePlate : ImmovableInteractable
    {
        private EnvironmentObject triggerObject;
        private bool playerOn, hammerOn;
        public PressurePlate(Model model, Vector3 pos, float scale, Camera cam, Texture2D t, EnvironmentObject triggerObject)
            : base(model, pos, scale, cam, t)
        {
            this.triggerObject = triggerObject;
            playerOn = false; hammerOn = false;
        }

        public override void Update(GameTime gameTime)
        {
            //triggerObject.setVisible(true);
            if (playerOn || hammerOn)
            {
                triggerObject.setVisible(false);
            }
            else
            {
                triggerObject.setVisible(true);
            }

        }

        private void activateTrigger()
        {
            triggerObject.setVisible(false);
        }

        public override void hitByPlayer(Player player)
        {
            //this.activateTrigger();
            playerOn = true;
        }

        public override void notHitByPlayer(Player player)
        {
            playerOn = false;
        }

        public override void hitByHammer(Hammer hammer)
        {
            //this.activateTrigger();
            hammerOn = true;
        }

        public override void notHitByHammer(Hammer hammer)
        {
            hammerOn = false;
        }
    }
}

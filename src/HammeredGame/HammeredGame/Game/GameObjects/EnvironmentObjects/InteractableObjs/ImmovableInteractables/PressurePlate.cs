using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables
{
    public class PressurePlate : ImmovableInteractable
    {
        private readonly EnvironmentObject triggerObject;
        private bool playerOn, hammerOn;

        public PressurePlate(Model model, Vector3 pos, float scale, Texture2D t, EnvironmentObject triggerObject) :
            base(model, pos, scale, t)
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

        private void ActivateTrigger()
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

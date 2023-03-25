using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables
{
    public class Key : CollectibleInteractable
    {
        /* Provisionally
        */
        private readonly Door correspondingDoor;
        private bool keyPickedUp = false;

        public Key(Model model, Vector3 pos, float scale, Texture2D t, Door correspondingDoor) :
            base(model, pos, scale, t)
        {
            this.correspondingDoor = correspondingDoor;
        }

        public bool IsPickedUp() => keyPickedUp;

        public override void hitByPlayer(Player player)
        {
            //this.activateTrigger();
            correspondingDoor.SetKeyFound(true);
            this.SetVisible(false);
            keyPickedUp = true;
        }
    }
}

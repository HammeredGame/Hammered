using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables
{
    /// <summary>
    /// The <c>PressurePlate</c> class refers to an unmoving object on the ground, which when pressed by some weight
    /// (currently the character or the hammer) triggers a change of behaviour of its corresponding <paramref>triggerObject</paramref>.
    /// Once the weight is lifted from the <c>PressurePlate</c> instance, the state of the corresponding <paramref>triggerObject</paramref>
    /// reverts to its state before being triggered by the <c>PressurePlate</c> instance.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/> ---> <see cref="ImmovableInteractable "/>
    ///                         ---> <see cref="PressurePlate"/>
    /// </para>
    /// </remarks>
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
                triggerObject.SetVisible(false);
            }
            else
            {
                triggerObject.SetVisible(true);
            }
        }

        private void ActivateTrigger()
        {
            triggerObject.SetVisible(false);
        }

        public override void TouchingPlayer(Player player)
        {
            //this.activateTrigger();
            playerOn = true;
        }

        public override void NotTouchingPlayer(Player player)
        {
            playerOn = false;
        }

        public override void TouchingHammer(Hammer hammer)
        {
            //this.activateTrigger();
            hammerOn = true;
        }

        public override void NotTouchingHammer(Hammer hammer)
        {
            hammerOn = false;
        }
    }
}

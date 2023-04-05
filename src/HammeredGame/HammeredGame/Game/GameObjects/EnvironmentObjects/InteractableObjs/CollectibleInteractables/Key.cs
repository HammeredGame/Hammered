using HammeredGame.Core;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables
{
    /// <summary>
    /// <para>
    /// The <c>Key</c> class refers to a collectible item in the scene which unlocks one particular <c>Door</c> instance
    /// in the entire scene.
    /// </para>
    /// <para>
    /// A <c>Key</c> instance is considered to be collected (<value>keyPickedUp</value> variable) when the character
    /// moves sufficiently close enough such that the two objects intersect
    /// (currently handled by <see cref="Player.Update(GameTime)"/> calling <see cref="Key.TouchingPlayer(Player)"/>).
    /// </para>
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="InteractableObject "/> ---> <see cref="CollectibleInteractable "/>
    ///                         ---> <see cref="Key"/>
    /// </para>
    /// <para>
    /// TODO: Instead of having a back-and-forth between the instance of <c>Key</c> and the instane of <c>Door</c>,
    /// maybe an alternative would for the <c>Key</c> instance to just change a state variable (example name "locked")
    /// and the <c>Key</c> instance to be rendered useless afterwards.
    /// The weakness to this approach is that the Player is immediately aware as to which <c>Door</c> instance corresponds
    /// to this key, which might be unwanted behaviour. This could be a point of discussion (probably tilting towards rejecting
    /// this proposition).
    /// </para>
    /// </remarks>
    public class Key : CollectibleInteractable
    {
        /* Provisionally
        */
        private readonly Door correspondingDoor;
        private bool keyPickedUp = false;

        public Key(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Door correspondingDoor) : base(services, model, t, pos, rotation, scale)
        {
            this.correspondingDoor = correspondingDoor;
        }

        public bool IsPickedUp() => keyPickedUp;

        public override void TouchingPlayer(Player player)
        {
            //this.activateTrigger();
            correspondingDoor.SetKeyFound(true);
            this.SetVisible(false);
            keyPickedUp = true;
        }
    }
}

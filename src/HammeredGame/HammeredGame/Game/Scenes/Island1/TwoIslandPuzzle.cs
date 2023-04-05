using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TwoIslandPuzzle : Scene
    {
        public TwoIslandPuzzle(GameServices services)
        {
            CreateFromXML(services, $"SceneDescriptions/Island1/TwoIslandPuzzle.xml");
            //Create<Player>("player", services, content.Load<Model>("character-colored"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
            //Create<Hammer>("hammer", services, content.Load<Model>("temp_hammer2"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
        }
    }
}

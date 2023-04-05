using HammeredGame.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammeredGame.Game
{

    /// <summary>
    /// A Scene Represents one 3D scene. It may or may not have a puzzle or a couple within it, but
    /// there are always game objects placed around it. A scene definition class will define how the
    /// scene looks, or how it plays out for the player, including any triggers, scripts, or dialogs.
    /// </summary>
    public abstract class Scene
    {

        /// <summary>
        /// The camera in the scene.
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// The objects loaded in the scene, keyed by unique identifier strings.
        /// </summary>
        public Dictionary<string, GameObject> GameObjects = new();

        public List<GameObject> GameObjectsList
        {
            get { return GameObjects.Values.ToList(); }
        }

        /// <summary>
        /// Create a new object in the scene
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The unique identifier for the object</param>
        /// <param name="arguments">The arguments to the constructor of the game object</param>
        /// <returns></returns>
        public T Create<T>(string name, params dynamic[] arguments) where T : GameObject
        {
            T i = (T)Activator.CreateInstance(typeof(T), args: arguments);
            GameObjects.Add(name, i);
            return i;
        }

        /// <summary>
        /// Find the object in the scene by a unique identifier. This approaches O(1) since it's a
        /// dictionary lookup.
        /// </summary>
        /// <typeparam name="T">The type of the game object to retrieve and cast</typeparam>
        /// <param name="name">The unique identifier to find</param>
        /// <returns>Null if not found, otherwise the game object</returns>
        public T Get<T>(string name) where T : GameObject
        {
            if (GameObjects.TryGetValue(name, out GameObject i))
            {
                return (T)i;
            }
            return null;
        }

        /// <summary>
        /// Remove an object in the scene by a unique identifier.
        /// </summary>
        /// <param name="name">The unique identifier to find</param>
        /// <returns>Whether the removal was successful or not</returns>
        public bool Remove(string name)
        {
            return GameObjects.Remove(name);
        }

        /// <summary>
        /// Removes all objects from the scene.
        /// </summary>
        public void Clear()
        {
            GameObjects.Clear();
        }

        public void CreateFromXML(GameServices services, string fileName)
        {
            XMLLevelLoader ll = new(fileName);
            Camera = ll.GetCamera(services.GetService<GraphicsDevice>(), services.GetService<Input>());
            int counter = 0;
            foreach (GameObject go in ll.GetGameObjects(services, Camera))
            {
                GameObjects.Add(counter.ToString(), go);
                counter++;
            }
        }
    }
}

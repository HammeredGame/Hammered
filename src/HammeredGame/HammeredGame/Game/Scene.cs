using HammeredGame.Core;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HammeredGame.Game
{

    /// <summary>
    /// A Scene Represents one 3D scene. It may or may not have a puzzle or a couple within it, but
    /// there are always game objects placed around it. A scene definition class will define how the
    /// scene looks, or how it plays out for the player, including any triggers, scripts, or dialogs.
    /// </summary>
    public abstract class Scene : IImGui
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

        /// <summary>
        /// Initialize the scene from the XML scene description. This will set up the Camera and the
        /// GameObjects. An XML scene description will not contain any game scripts or triggers.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="fileName"></param>
        public void CreateFromXML(GameServices services, string fileName)
        {
            XMLLevelLoader ll = new(fileName);
            Camera = ll.GetCamera(services.GetService<GraphicsDevice>(), services.GetService<Input>());
            GameObjects = ll.GetGameObjects(services, Camera);
        }

        // Store all the fully qualified names for available scene classes.
        private static IEnumerable<string> sceneFqns;

        /// <summary>
        /// Get all the fully qualified names for available scene classes in this assembly. Cached
        /// upon first computation. The results of these can be instantiated with Reflection using
        /// the function Activator.CreateInstance(Type.GetType(name)).
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<string> GetAllSceneFQNs()
        {
            // Calculate once and use a cache from then on, since the list of available scene types
            // don't change at runtime.
            if (sceneFqns == null)
            {
                // The prefix to search all types for
                const string sceneNamespacePrefix = "HammeredGame.Game.Scenes";
                // Predicate to check for compiler generated types, since using async/await
                // generates a couple of classes.
                static bool isCompilerGenerated(Type t)
                {
                    if (t == null)
                    {
                        return false;
                    }

                    return t.GetTypeInfo().GetCustomAttributes<CompilerGeneratedAttribute>().Any() || isCompilerGenerated(t.DeclaringType);
                }
                sceneFqns = Assembly
                    .GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsClass && !isCompilerGenerated(t) && t.Namespace?.StartsWith(sceneNamespacePrefix) == true)
                    .Select(t => t.FullName);
            }
            return sceneFqns;
        }

        public void UI()
        {
            // Show the camera UI
            Camera.UI();

            // Show an interactive list of game objects, each of which contain basic properties to edit
            if (ImGui.TreeNode($"Scene objects: {GameObjectsList.Count}"))
            {
                foreach ((string key, GameObject gameObject) in GameObjects) {

                    if (ImGui.TreeNode($"{key}: {gameObject.GetType().Name}"))
                    {
                        // ImGui accepts only system.numerics.vectorX and not MonoGame VectorX, so
                        // we need to temporarily convert.
                        System.Numerics.Vector3 pos = gameObject.Position.ToNumerics();
                        ImGui.DragFloat3("Position", ref pos);
                        gameObject.Position = pos;

                        System.Numerics.Vector4 rot = gameObject.Rotation.ToVector4().ToNumerics();
                        ImGui.DragFloat4("Rotation", ref rot, 0.01f, -1.0f, 1.0f);

                        gameObject.Rotation = Quaternion.Normalize(new Quaternion(rot));
                        ImGui.DragFloat("Scale", ref gameObject.Scale, 0.01f);

                        ImGui.Text($"Texture: {gameObject.Texture?.ToString() ?? "None"}");

                        // Draw any object specific UI
                        if (gameObject is IImGui objectWithGui)
                        {
                            objectWithGui.UI();
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
            if (ImGui.Button("Export Level"))
            {
                //new XMLLevelWriter(camera, gameObjects);
            }
        }
    }
}

using HammeredGame.Core;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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

        protected GameServices Services;

        public Scene(GameServices services)
        {
            this.Services = services;
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
                sceneFqns = GetAllFQNsWithPrefix(sceneNamespacePrefix);
            }
            return sceneFqns;
        }

        /// <summary>
        /// Get all the fully qualified names in the current assembly that begins with a prefix.
        /// This function is not cached, so repeated queries should be avoided in the game loop. The
        /// results of these can be instantiated with Reflection using the function Activator.CreateInstance(Type.GetType(name)).
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetAllFQNsWithPrefix(string prefix)
        {
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
            return Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !isCompilerGenerated(t) && t.Namespace?.StartsWith(prefix) == true)
                .Select(t => t.FullName);
        }

        // Some things for the object creation popup in the debug UI.
        // TODO: if this affects the memory footprint of the game in Release mode, hide with an DEBUG ifdef
        private string objectCreationSelectedFqn = "...";
        private string objectCreationSelectedModel = "...";
        private string objectCreationSelectedTexture = "...";
        private System.Numerics.Vector3 objectCreationPosition = System.Numerics.Vector3.Zero;
        private System.Numerics.Vector4 objectCreationRotation = Quaternion.Identity.ToVector4().ToNumerics();
        private float objectCreationScale = 1f;

        public void UI()
        {
            // Show the camera UI
            Camera.UI();

            if (ImGui.TreeNode($"Scene objects: {GameObjects.Count}"))
            {
                // Show an interactive list of game objects, each of which contain basic properties to edit
                foreach ((string key, GameObject gameObject) in GameObjects)
                {
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

                        // Draw any object specific UI defined within its UI() function
                        if (gameObject is IImGui objectWithGui)
                        {
                            objectWithGui.UI();
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            // A button to launch the popup for creating a new object
            if (ImGui.Button("Create New Object"))
            {
                ImGui.OpenPopup("create_new_object");
            }

            if (ImGui.BeginPopup("create_new_object"))
            {
                // Select a class from the dropdown of all available game object classes
                if (ImGui.BeginCombo("Class", objectCreationSelectedFqn))
                {
                    foreach (string fqn in GetAllFQNsWithPrefix("HammeredGame.Game.GameObjects"))
                    {
                        if (ImGui.Selectable(fqn, objectCreationSelectedFqn == fqn))
                        {
                            objectCreationSelectedFqn = fqn;
                        }
                    }
                    ImGui.EndCombo();
                }

                // Use Reflection to access a private variable within ContentManager that contains
                // the key/value of all loaded assets.
                Dictionary<string, object> loadedAssets = (Dictionary<string, object>)typeof(ContentManager).GetField("loadedAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Services.GetService<ContentManager>());

                // Filter just the models, and show that as a dropdown
                IEnumerable<string> models = loadedAssets.Where(asset => asset.Value.GetType() == typeof(Model)).Select(a => a.Key);
                if (ImGui.BeginCombo("Model", objectCreationSelectedModel))
                {
                    // Add an option to select null
                    if (ImGui.Selectable("<null>"))
                    {
                        objectCreationSelectedModel = "...";
                    };
                    foreach (string model in models)
                    {
                        if (ImGui.Selectable(model, objectCreationSelectedModel == model))
                        {
                            objectCreationSelectedModel = model;
                        }
                    }
                    ImGui.EndCombo();
                }

                // Filter just the textures and show those as the dropdown
                IEnumerable<string> textures = loadedAssets.Where(asset => asset.Value.GetType() == typeof(Texture2D)).Select(a => a.Key);
                if (ImGui.BeginCombo("Texture", objectCreationSelectedTexture))
                {
                    // Add an option to select null
                    if (ImGui.Selectable("<null>"))
                    {
                        objectCreationSelectedTexture = "...";
                    };
                    foreach (string texture in textures)
                    {
                        if (ImGui.Selectable(texture, objectCreationSelectedTexture == texture))
                        {
                            objectCreationSelectedTexture = texture;
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.InputFloat3("Position", ref objectCreationPosition);
                ImGui.InputFloat4("Rotation", ref objectCreationRotation);
                ImGui.InputFloat("Scale", ref objectCreationScale);

                if (ImGui.Button("Create"))
                {
                    // Generate a name for the object.
                    string nameCandidate = Type.GetType(objectCreationSelectedFqn).Name.ToLower();
                    for (int i = 1; GameObjects.ContainsKey(nameCandidate); i++)
                    {
                        nameCandidate = Type.GetType(objectCreationSelectedFqn).Name.ToLower() + i.ToString();
                    }

                    // Invoke this.Create with arguments for the game object type constructor. Since
                    // this is a generic method, we have to create a specific version for the type
                    // we are creating.
                    GetType().GetMethod(nameof(Create)).MakeGenericMethod(Type.GetType(objectCreationSelectedFqn)).Invoke(this, new object[] {
                        nameCandidate,
                        // Although the type signature of Create allows passing the name, followed
                        // by any number of parameters to pass to the game object constructor, C#
                        // treats this as syntax sugar for accepting an object[] as the second
                        // parameter. This is why we need to create an array here for the second
                        // parameter to pass.
                        new object[] {
                            Services,
                            objectCreationSelectedModel != "..." ? Services.GetService<ContentManager>().Load<Model>(objectCreationSelectedModel) : null,
                            objectCreationSelectedTexture != "..." ? Services.GetService<ContentManager>().Load<Texture2D>(objectCreationSelectedTexture) : null,
                            new Vector3(objectCreationPosition.X, objectCreationPosition.Y, objectCreationPosition.Z),
                            new Quaternion(objectCreationRotation),
                            objectCreationScale
                        }
                    });
                }
                ImGui.EndPopup();
            }

            if (ImGui.Button("Export Level"))
            {
                //new XMLLevelWriter(camera, gameObjects);
            }
        }
    }
}

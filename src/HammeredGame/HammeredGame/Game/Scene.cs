﻿using HammeredGame.Core;
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

        protected Scene(GameServices services)
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
        /// Generate a name for a game object that has numbers appended to the specified prefix
        /// until the point where no existing object in the scene has that name.
        /// </summary>
        /// <param name="prefix">The prefix to add numbers to</param>
        /// <returns>The unique name candidate</returns>
        private string GenerateUniqueNameWithPrefix(string prefix)
        {
            string nameCandidate = prefix;
            for (int i = 1; GameObjects.ContainsKey(nameCandidate); i++)
            {
                nameCandidate = prefix + i.ToString();
            }
            return nameCandidate;
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
        public void CreateFromXML(string fileName)
        {
            (Camera, GameObjects) = SceneDescriptionIO.ParseFromXML(fileName, Services);
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

        private string objectListCurrentSelection;

        public void UI()
        {
            // Show the camera UI
            Camera.UI();

            ImGui.Text($"{GameObjects.Count} objects in scene");
            ImGui.SameLine();
            // Button to load from XML. This will replace all the scene objects
            // TODO: update Space and bounding boxes?
            if (ImGui.Button("Load Scene XML"))
            {
                // open a cross platform file dialog
                NativeFileDialogSharp.DialogResult result = NativeFileDialogSharp.Dialog.FileOpen("xml");

                if (result.IsOk)
                {
                    // Clear the scene objects
                    Clear();
                    CreateFromXML(result.Path);
                }
            }

            ImGui.SameLine();
            // Button to export to XML
            if (ImGui.Button("Export Scene"))
            {
                SceneDescriptionIO.WriteToXML("defaultname.xml", Camera, GameObjects, Services);
            }

            // Show a dual pane layout, with the scene object list on the left and details on the
            // right. We begin with defining the left side.
            {
                ImGui.BeginGroup();
                const int sideBarWidth = 250;
                // Define the sidebar to take the set width and all height except one line at the bottom.
                if (ImGui.BeginChild("scene_object_list", new System.Numerics.Vector2(sideBarWidth, -ImGui.GetFrameHeightWithSpacing()), true))
                {
                    // See explanation later on why this boolean is needed
                    bool openDeletionConfirmation = false;

                    // Show each object key as a selectable item
                    foreach ((string key, GameObject gameObject) in new Dictionary<string, GameObject>(GameObjects))
                    {
                        if (ImGui.Selectable($"{key}: {gameObject.GetType().Name}", objectListCurrentSelection == key))
                        {
                            objectListCurrentSelection = key;
                        }
                        // Define the menu that pops up when right clicking an object in the tree
                        if (ImGui.BeginPopupContextItem())
                        {
                            // Object duplication (creates a new object with a new name but everything
                            // else the same)
                            if (ImGui.MenuItem("Duplicate Object"))
                            {
                                // Generate a new name for the object
                                string name = GenerateUniqueNameWithPrefix(gameObject.GetType().Name.ToLower());

                                // We want to call Create<T>() with T being the type of gameObject.
                                // However, we can't use variables for generic type parameters, so
                                // instead we will create a specific version of the method and invoke it
                                // manually. This causes some changes to how variadic "params dynamic[]"
                                // behaves, outlined below.
                                GetType().GetMethod(nameof(Create)).MakeGenericMethod(gameObject.GetType()).Invoke(this, new object[] {
                                    name,
                                    new object[] {
                                        Services,
                                        // We are passing references to the Model and Texture here,
                                        // assuming they won't change, and that loading them again from
                                        // the Content Manager would be a waste.
                                        gameObject.Model,
                                        gameObject.Texture,
                                        gameObject.Position,
                                        gameObject.Rotation,
                                        gameObject.Scale
                                    }
                                });

                                // Set sidebar focus to created object
                                objectListCurrentSelection = name;
                            }

                            // Object deletion
                            if (ImGui.MenuItem("Delete Object"))
                            {
                                // Explanation on this boolean: We want to call ImGui.OpenPopup()
                                // here to open the deletion confirmation popup. However, popups can
                                // only be called from the same ID space, so we need to have the
                                // popup defined in this block of code, but MenuItem closes on the
                                // next frame so the popup won't persist. The workaround is to set a flag.
                                openDeletionConfirmation = true;
                            }
                            ImGui.EndPopup();
                        }

                        // See above on why this is called here
                        if (openDeletionConfirmation)
                        {
                            ImGui.OpenPopup("object_deletion_confirmation_" + key);
                        }

                        // The confirmation popup to show. This has to be in the UI tree always
                        // regardless of the state of the menu that triggered it, otherwise it'll
                        // disappear instantly.
                        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new System.Numerics.Vector2(0.5f, 0.5f));
                        if (ImGui.BeginPopupModal("object_deletion_confirmation_" + key))
                        {
                            System.Diagnostics.Debug.WriteLine("b");
                            ImGui.Text($"Confirm delete object \"{key}\"?");
                            if (ImGui.Button("Cancel")) { ImGui.CloseCurrentPopup(); }
                            ImGui.SameLine();
                            if (ImGui.Button("Delete")) { Remove(key); ImGui.CloseCurrentPopup(); }
                            ImGui.EndPopup();
                        }
                    }
                    ImGui.EndChild();
                }
                // A button to launch the popup for creating a new object
                if (ImGui.Button("Create New Object", new System.Numerics.Vector2(sideBarWidth, 0f)))
                {
                    ImGui.OpenPopup("create_new_object");
                }

                ImGui.EndGroup();
            }
            ImGui.SameLine();

            // Define the right side of the two-pane layout, which contains the selected object details
            {
                ImGui.BeginGroup();
                if (ImGui.BeginChild("object_detail_view", new System.Numerics.Vector2(0, 0)))
                {
                    if (objectListCurrentSelection != null && GameObjects.ContainsKey(objectListCurrentSelection))
                    {
                        GameObject gameObject = GameObjects[objectListCurrentSelection];
                        // ImGui accepts only system.numerics.vectorX and not MonoGame VectorX, so
                        // we need to temporarily convert.
                        System.Numerics.Vector3 pos = gameObject.Position.ToNumerics();
                        ImGui.DragFloat3("Position", ref pos, 10f);
                        gameObject.Position = pos;

                        System.Numerics.Vector4 rot = gameObject.Rotation.ToVector4().ToNumerics();
                        ImGui.DragFloat4("Rotation", ref rot, 0.01f, -1.0f, 1.0f);

                        gameObject.Rotation = Quaternion.Normalize(new Quaternion(rot));
                        ImGui.DragFloat("Scale", ref gameObject.Scale, 0.01f);

                        ImGui.Text($"Texture: {gameObject.Texture?.ToString() ?? "None"}");

                        ImGui.Separator();

                        // Draw any object specific UI defined within its UI() function
                        if (gameObject is IImGui objectWithGui)
                        {
                            objectWithGui.UI();
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.EndGroup();
            }

            // The popup that upons when you click the button to create a new object
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
                    string name = GenerateUniqueNameWithPrefix(Type.GetType(objectCreationSelectedFqn).Name.ToLower());

                    // Invoke this.Create with arguments for the game object type constructor. Since
                    // this is a generic method, we have to create a specific version for the type
                    // we are creating.
                    GetType().GetMethod(nameof(Create)).MakeGenericMethod(Type.GetType(objectCreationSelectedFqn)).Invoke(this, new object[] {
                        name,
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

                    // Set sidebar focus to created object
                    objectListCurrentSelection = name;
                }
                ImGui.EndPopup();
            }
        }
    }
}

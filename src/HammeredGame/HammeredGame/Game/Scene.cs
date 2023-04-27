using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Settings;
using HammeredGame.Core;
using HammeredGame.Game.PathPlanning.Grid;
using HammeredGame.Game.Screens;
using HammeredGame.Graphics;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SoftCircuits.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        /// The uniform grid of the scene.
        /// </summary>
        public UniformGrid Grid { get; private set; }

        /// <summary>
        /// The objects loaded in the scene, keyed by unique identifier strings.
        /// </summary>
        public OrderedDictionary<string, GameObject> GameObjects = new();

        public List<GameObject> GameObjectsList
        {
            get { return GameObjects.Values.ToList(); }
        }

        public bool IsLoaded { get; protected set; } = false;

        public bool SceneStarted { get; private set; } = false;

        /// <summary>
        /// Any debug objects shown as representations of bounding boxes. This list is updated in this.UpdateDebugObjects().
        /// </summary>
        public List<EntityDebugDrawer> DebugObjects = new();

        public bool DrawDebugObjects = false;

        // Uniform Grid debugging variables
        public List<GridDebugDrawer> DebugGridCells = new();

        public bool DrawDebugGrid = false;

        /// <summary>
        /// The lighting for the scene. This will be loaded from the XML.
        /// </summary>
        public SceneLightSetup Lights;

        /// <summary>
        /// The physics space for the scene.
        /// </summary>
        public Space Space;

        /// <summary>
        /// The game services that all objects in the scene and any scripts can access.
        /// </summary>
        protected GameServices Services;

        /// <summary>
        /// The game screen that this scene belongs to.
        /// </summary>
        protected GameScreen ParentGameScreen;

        protected Scene(GameServices services, GameScreen screen)
        {
            this.Services = services;
            this.ParentGameScreen = screen;
        }

        public Task LoadContentAsync()
        {
            return Task.Run(() =>
            {
                LoadContent();
                IsLoaded = true;
            });
        }

        protected virtual void LoadContent()
        {
            InitNewSpace();
        }

        /// <summary>
        /// Script to run after loading the scene description. Called during the first Update()
        /// after the scene has fully finished loading. Any initialization before this should be
        /// done in LoadContent().
        /// </summary>
        protected abstract void OnSceneStart();

        /// <summary>
        /// Initialize a new physics space for the scene, with Earth-like gravity. Adds it to the
        /// GameService under the Space type.
        /// </summary>
        private void InitNewSpace()
        {
            // Construct a new space for the physics simulation to occur within.
            Space = new Space(HammeredGame.ParallelLooper);

            //Set the gravity of the simulation by accessing the simulation settings of the space.
            //It defaults to (0,0,0); this changes it to an 'earth like' gravity.
            //Try looking around in the space's simulationSettings to familiarize yourself with the various options.
            Space.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, -200f, 0);
            CollisionDetectionSettings.AllowedPenetration = 0.001f;

            // Add the physics space to be a globally accessible service
            Services.AddService<Space>(Space);
        }

        /// <summary>
        /// Create a new object in the scene. The object constructor handles the addition of itself
        /// to the physics Space if necessary.
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
        /// Remove an object in the scene by a unique identifier. Also removes the associated entity
        /// (if there is one) from the active physics space. Unfortunately this won't remove terrain
        /// StaticMeshes in the physics space since they're not entities nor are they tied to the
        /// game object, but hopefully it's rare that a terrain needs to be Remove()-ed.
        /// </summary>
        /// <param name="name">The unique identifier to find</param>
        /// <returns>Whether the removal was successful or not</returns>
        public bool Remove(string name)
        {
            if (GameObjects.ContainsKey(name))
            {
                Entity associatedEntity = Get<GameObject>(name)?.Entity;
                if (associatedEntity != null && Space.Entities.Contains(associatedEntity))
                {
                    Space.Remove(associatedEntity);
                }
                return GameObjects.Remove(name);
            }
            return false;
        }

        /// <summary>
        /// Removes all objects from the scene.
        /// </summary>
        public void Clear()
        {
            GameObjects.Clear();
            InitNewSpace();
        }

        /// <summary>
        /// Initialize the scene from the XML scene description. This will set up the Camera and the
        /// GameObjects. An XML scene description will not contain any game scripts or triggers.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="fileName"></param>
        public void CreateFromXML(string fileName)
        {
            (Camera, Lights, GameObjects, Grid) = SceneDescriptionIO.ParseFromXML(fileName, Services);
            // Set up the list of debug grid cells for debugging visualization
            // WARNING: Execute the following line of code if you wish to initialize the grid only once.
            // Suggested for when (constant) AVAILABLE grid cells are shown.
            //this.UpdateDebugGrid(); // Quick Patch...maybe it should be put at a better place in the code.
            foreach (GameObject obj in GameObjects.Values) { obj.SetCurrentScene(this); }
        }

        /// <summary>
        /// Update the scene at every tick
        /// </summary>
        public virtual void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            if (!IsLoaded) return;

            if (!SceneStarted)
            {
                SceneStarted = true;
                OnSceneStart();
            }

            // Update each game object
            foreach (GameObject gameObject in this.GameObjectsList)
            {
                // Game objects updating should depend on whether the screen has input focus, like
                // for player movement. When a dialogue is shown and screen loses focus, all
                // interactions with objects should be paused.
                gameObject.Update(gameTime, screenHasFocus);
            }

            // For the camera, we use the paused flag instead of whether the screen has focus, since
            // the camera only has a different behaviour when the game is paused. If there is
            // dialogue for example, the camera will be the same as regular gameplay.
            this.Camera.UpdateCamera(isPaused);

            //Steps the simulation forward one time step.
            // TODO: perhaps this shouldn't update if it's paused (i.e. check for HasFocus)?
            this.Space.Update();

            // Set up the list of debug entities for debugging visualization
            if (DrawDebugObjects) this.UpdateDebugObjects();

            // Set up the list of debug grid cells for debugging visualization
            // WARNING: Execute the following line of code if you wish to update the grid at each frame.
            // Suggested for when NON available grid cells are shown.
            if (DrawDebugGrid) this.UpdateDebugGrid();
        }

        /// <summary>
        /// Recreate the list of debug objects in this scene, representing bounding boxes. This
        /// should be called on every game loop Update().
        /// </summary>
        public void UpdateDebugObjects()
        {
            DebugObjects.Clear();
            var CubeModel = Services.GetService<ContentManager>().Load<Model>("cube");
            //Go through the list of entities in the space and create a graphical representation for them.
            foreach (Entity e in Space.Entities)
            {
                Box box = e as Box;
                if (box != null) //This won't create any graphics for an entity that isn't a box since the model being used is a box.
                {
                    BEPUutilities.Matrix scaling = BEPUutilities.Matrix.CreateScale(box.Width, box.Height, box.Length); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    EntityDebugDrawer model = new(e, CubeModel, scaling);
                    //Add the drawable game component for this entity to the game.
                    DebugObjects.Add(model);
                }
            }
        }

        /// <summary>
        /// Recreate the debug grid in this scene, representing bounding boxes. This should be
        /// called on every game loop Update().
        /// </summary>
        public void UpdateDebugGrid()
        {
            DebugGridCells.Clear();
            var CubeModel = Services.GetService<ContentManager>().Load<Model>("cube");
            //Go through the list of entities in the space and create a graphical representation for them.
            float sideLength = Grid.sideLength;
            Matrix scaling = Matrix.CreateScale(sideLength);

            int[] gridDimensions = Grid.GetDimensions();
            for (int i = 0; i < gridDimensions[0]; ++i)
            {
                for (int j = 0; j < gridDimensions[1]; ++j)
                {
                    for (int k = 0; k < gridDimensions[2]; ++k)
                    {
                        if (!Grid.mask[i, j, k])
                        {
                            Vector3 gridcell = Grid.grid[i, j, k];
                            GridDebugDrawer gdd = new GridDebugDrawer(CubeModel, gridcell, scaling);
                            DebugGridCells.Add(gdd);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate a name for a game object that has numbers appended to the specified prefix
        /// until the point where no existing object in the scene has that name.
        /// </summary>
        /// <param name="prefix">The prefix to add numbers to</param>
        /// <returns>The unique name candidate</returns>
        protected string GenerateUniqueNameWithPrefix(string prefix)
        {
            string nameCandidate = prefix;
            for (int i = 1; GameObjects.ContainsKey(nameCandidate); i++)
            {
                nameCandidate = prefix + i.ToString();
            }
            return nameCandidate;
        }

        public void UpdateSceneGrid(GameObject gameObject, bool availability, double tightness=1.0)
        {
            if (gameObject.Entity != null)
            {
                // Perhaps too many unneeded computations (p2 and p3, p3x, p3y and p3z do not need to be used).
                Box goBox = (gameObject.Entity as Box);
                /// <summary>
                /// 1) SUPPOSE that the bounding box is initially axis aligned (i.e. it "spreads out" parallelly to the x-axis, y-axis and z-axis)
                /// 2) Sample points using the "standard basis" --i.e. the basis of R^3: (1, 0, 0), (0, 1, 0), (0, 0, 1)-- to extract new points.
                /// 3) Rotate the points extracted in STEP 2) to get the correct 3D position of the sampled point
                ///     along the axes the bounding box "unfolds".
                /// </summary>

                Vector3 e1 = new Vector3(1, 0, 0), e2 = new Vector3(0, 1, 0), e3 = new Vector3(0, 0, 1);
                float sideLength = this.Grid.sideLength;
                // In case the developer wishes to create more "safety distance" between the hammer and the objects,
                // just adjust the scalar multiplier 1 to something greater than 1.
                // This could prove useful, because the size of the hammer is not currently taken into account.
                // Ideally, the "·Repetitions" variables would also be parameterized w.r.t the dimensions of the hammer.
                int xRepetitions = (int)(Math.Ceiling(goBox.HalfWidth / sideLength) * tightness); 
                int yRepetitions = (int)(Math.Ceiling(goBox.HalfHeight / sideLength) * tightness);
                int zRepetitions = (int)(Math.Ceiling(goBox.HalfLength / sideLength) * tightness); 
                for (int i = -xRepetitions; i <= xRepetitions; ++i)
                {
                    for (int j = -yRepetitions; j <= yRepetitions; ++j)
                    {
                        for (int k = -zRepetitions; k <= zRepetitions; ++k)
                        {
                            Vector3 localOrigin = MathConverter.Convert(goBox.Position);
                            Vector3 sampledPoint = localOrigin + sideLength * (i * e1 + j * e2 + k * e3);
                            //sampledPoint = Vector3.Transform(sampledPointInStandardBasis, MathConverter.Convert(goBox.OrientationMatrix)); // Bug! Ask Sid!
                            sampledPoint = Vector3.Transform(sampledPoint, Matrix.CreateTranslation(-localOrigin)); // Translate to origin.
                            sampledPoint = Vector3.Transform(sampledPoint, MathConverter.Convert(goBox.OrientationMatrix)); // Apply rotation
                            sampledPoint = Vector3.Transform(sampledPoint, Matrix.CreateTranslation(localOrigin)); // Return to global coordinates
                            this.Grid.MarkCellAs(sampledPoint, availability);
                        }
                    }
                }
            }
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

        /// <summary>
        /// Reorder a game object queried by the key to the destination index within the scene's
        /// object list. This removes the object at the original location, and inserts it again as
        /// the value at the destination index, shifting everything that already exists downwards.
        /// Reordering can be necessary in the level editor to control render order.
        /// </summary>
        /// <param name="sourceKey">The key/id for the game object that will move</param>
        /// <param name="destinationIndex">The index that the game object should move to</param>
        private void ReorderObject(string sourceKey, int destinationIndex)
        {
            // Grab the source object and its current index, then delete it
            GameObject sourceObject = GameObjects[sourceKey];
            int sourceIndex = GameObjects.IndexOf(sourceKey);
            GameObjects.Remove(sourceKey);

            // If the source was earlier in the list than the destination, the
            // insertion index would've shifted down by one because of the removal.
            if (sourceIndex < destinationIndex)
            {
                destinationIndex--;
            }

            // Insert the source object just before the destination index
            GameObjects.Insert(destinationIndex, sourceKey, sourceObject);
        }

        // Some things for the object creation popup in the debug UI, to make data persistent across frames.

        // The default placeholder to show in the object creation class name drop-down
        private string objectCreationSelectedFqn = "...";

        // The default placeholder to show in the object creation model name drop-down
        private string objectCreationSelectedModel = "...";

        // The default placeholder to show in the object creation texture name drop-down
        private string objectCreationSelectedTexture = "...";

        // The default values for position, rotation, scale, and physics entity when creating a new object
        private System.Numerics.Vector3 objectCreationPosition = System.Numerics.Vector3.Zero;
        private System.Numerics.Vector4 objectCreationRotation = Quaternion.Identity.ToVector4().ToNumerics();
        private float objectCreationScale = 1f;
        private Entity objectCreationEntity;

        // The key for the game object that's currently selected in the left pane
        private string objectListCurrentSelection;

        // The key for the game object that's currently being dragged
        private string currentlyDraggingKey;

        public void UI()
        {
            // Show the camera UI
            Camera.UI();

            // Show the scene editor

            ImGui.Text($"{GameObjects.Count} objects in scene, {Services.GetService<Space>().Entities.Count} entities in physics space");
            ImGui.SameLine();
            // Button to load from XML. This will replace all the scene objects
            if (ImGui.Button("Load Scene XML"))
            {
                // open a cross platform file dialog
                NativeFileDialogSharp.DialogResult result = NativeFileDialogSharp.Dialog.FileOpen("xml");

                if (result.IsOk)
                {
                    // Clear the scene objects and the physics space
                    Clear();
                    CreateFromXML(result.Path);
                    // Re-run the scene start script
                    OnSceneStart();
                }
            }

            ImGui.SameLine();
            // Button to export to XML
            if (ImGui.Button("Export Scene"))
            {
                SceneDescriptionIO.WriteToXML("defaultname.xml", Camera, Lights, GameObjects, Grid, Services);
            }

            ImGui.Checkbox("Draw Bounding Boxes", ref DrawDebugObjects);
            ImGui.Checkbox("Draw Grids", ref DrawDebugGrid);

            // Show UI for lights
            if (ImGui.CollapsingHeader("Sunlight"))
            {
                System.Numerics.Vector3 sunDir = Lights.Sun.Direction.ToNumerics();
                ImGui.DragFloat3("Sun Direction", ref sunDir, 0.01f);
                Lights.Sun.Direction = Vector3.Normalize(sunDir);
                ImGui.DragFloat("Sun Intensity", ref Lights.Sun.Intensity, 0.01f, 0f);
                System.Numerics.Vector3 sunColor = Lights.Sun.LightColor.ToVector3().ToNumerics();
                ImGui.PushItemWidth(200f);
                ImGui.ColorPicker3("Sun Color", ref sunColor);
                Lights.Sun.LightColor = new Color(sunColor);
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
                    bool openXMLCopyingPopup = false;

                    // Show each object key as a selectable item, with its own right click menus for
                    // duplication and deletion. Because we can create and remove items from the
                    // GameObject dictionary, we need to first create a copy that isn't affected by
                    // mutation. OrderedDictionary doesn't have a copy constructor. It does however,
                    // have an AddRange() method that takes another OrderedDictionary.
                    // Unfortunately, that is also flawed because it adds items to the new copy in
                    // the order that's in the internal key list, which will not be the desired
                    // order after any manual rearrangements (via drag/drop). We need a copy that
                    // has the same order as the actual item orderings, which is achieved by casting
                    // it to an IEnumerable and using another overload of AddRange() that does what
                    // we want.
                    OrderedDictionary<string, GameObject> workingCopy = new();
                    workingCopy.AddRange(GameObjects as IEnumerable<KeyValuePair<string, GameObject>>);

                    foreach ((string key, GameObject gameObject) in workingCopy)
                    {
                        // Show a Selectable item, that is highlighted if the current selection
                        // matches this one. This Selectable is also a draggable item.
                        if (ImGui.Selectable($"{key}: {gameObject.GetType().Name}", objectListCurrentSelection == key))
                        {
                            // When item in list selected, set the selection variable used in the
                            // details pane to show its details.
                            objectListCurrentSelection = key;

                            // Also highlight the item on screen by changing its texture to red for
                            // 500 milliseconds
                            var currentTexture = gameObject.Texture;
                            var redRectangle = new Texture2D(Services.GetService<GraphicsDevice>(), 1, 1);
                            redRectangle.SetData(new[] { Color.Red });
                            gameObject.Texture = redRectangle;

                            Services.GetService<ScriptUtils>()
                                .WaitMilliseconds(500)
                                .ContinueWith((_) => gameObject.Texture = currentTexture);
                        }

                        // Mark the previous item (the Selectable) as a draggable source. We want to
                        // reorder items in the editor because they affect the render order
                        // directly, and it's a hassle to have to manually rearrange the XML
                        // whenever we want to change render order.
                        if (ImGui.BeginDragDropSource())
                        {
                            // All draggable sources need to declare a payload. This would make
                            // sense in C++, but it doesn't in C# unless we want to play around with
                            // raw pointers, so we just set the payload as zero.
                            ImGui.SetDragDropPayload("GameObject", IntPtr.Zero, 0);
                            // The text displayed under the mouse while hovering
                            ImGui.Text($"Moving {key}");
                            // Set the item key for the item currently being hovered. This will be
                            // used upon release to determine what the drag source was, since we
                            // don't use payloads.
                            currentlyDraggingKey = key;
                            ImGui.EndDragDropSource();
                        }

                        // Mark the previous item (the Selectable) as a drop target, so items can be
                        // dropped onto other items. It's not possible with ImGui to drop items into
                        // the space between two items, so this is a compromise.
                        if (ImGui.BeginDragDropTarget())
                        {
                            // Technically, AcceptDragDropPayload() returns a pointer to the payload
                            // that was being dragged (or null if it wasn't dropped here). But we're
                            // not using payloads because we don't want to mess with raw pointers,
                            // so ignore its return value and manually check for mouse release.
                            ImGui.AcceptDragDropPayload("GameObject");
                            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                            {
                                // On drag release, we want to remove the source from where it was,
                                // and re-add it with the same key and value but at the location
                                // under the cursor. Since we use OrderedDictionary, we can access
                                // the corresponding numerical index of string keys and use those.
                                ReorderObject(currentlyDraggingKey, GameObjects.IndexOf(key));
                            }
                            ImGui.EndDragDropTarget();
                        }

                        // Define the menu that pops up when right clicking an object in the tree
                        if (ImGui.BeginPopupContextItem())
                        {
                            // Copying an individual object XML, this makes editing small values
                            // easy when you've already moved your player or tweaked other things,
                            // and don't necessarily want to save those.
                            if (ImGui.MenuItem("Show XML (for copying)"))
                            {
                                // We use a flag to show the popup later. For more info, see the
                                // comment under the "Delete Object" context menu item.
                                openXMLCopyingPopup = true;
                            }

                            // Object duplication (creates a new object with a new name but everything
                            // else the same)
                            if (ImGui.MenuItem("Duplicate Object"))
                            {
                                // Generate a new name for the object
                                string name = GenerateUniqueNameWithPrefix(gameObject.GetType().Name.ToLower());

                                // Copy the entity
                                Entity entity = null;
                                if (gameObject.Entity is Box box)
                                {
                                    entity = new Box(box.Position, box.Width, box.Height, box.Length, box.Mass);
                                }
                                else if (gameObject.Entity is Sphere sph)
                                {
                                    entity = new Sphere(sph.Position, sph.Radius, sph.Mass);
                                }

                                // We now want to call Create<T>() with T being the type of
                                // gameObject. However, we can't use dynamic variables as generic
                                // type parameters. Instead, we have to create a "specific" version
                                // of the method tailored for the item, so
                                // Create<typeof(gameObject)>, and call it with Invoke(). Because
                                // we're now no longer using the normal way of calling functions, we
                                // don't get the benefit of the syntactic sugar of "params" in the
                                // argument list of Create<T>(). Instead of variadic arguments, we
                                // now need to pass an array of objects, because this is how C#
                                // internally desugars "params" to.
                                GameObject newObj = (GameObject)GetType().GetMethod(nameof(Create)).MakeGenericMethod(gameObject.GetType()).Invoke(this, new object[] {
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
                                        gameObject.Scale,
                                        entity
                                    }
                                });
                                // Apply the same model offset as the original
                                newObj.EntityModelOffset = gameObject.EntityModelOffset;

                                // Move the newly duplicated object just under the source object
                                ReorderObject(name, GameObjects.IndexOf(key) + 1);

                                // Set sidebar focus to created object
                                objectListCurrentSelection = name;
                            }

                            // Object deletion
                            if (ImGui.MenuItem("Delete Object"))
                            {
                                // Explanation on this boolean: We ideally want to call
                                // ImGui.OpenPopup() here to open the deletion confirmation popup.
                                // However, popups can only be called from the same ID space as
                                // where it is defined, so we'd need to have the popup defined in
                                // this block of code for us to be able to call it here. But that's
                                // a problem, because MenuItem closes on the next frame so the popup
                                // won't persist. The workaround is to set a flag.
                                // This issue is documented on GitHub at ocornut/imgui#331
                                openDeletionConfirmation = true;
                            }
                            ImGui.EndPopup();
                        }

                        // See documentation inside the Delete Object menu item on why object
                        // deletion popup calls are performed outside of the right-click menu definition
                        if (openDeletionConfirmation)
                        {
                            ImGui.OpenPopup($"object_deletion_confirmation_{key}");
                            openDeletionConfirmation = false;
                        }

                        // The confirmation popup to show. This has to be in the UI tree always
                        // regardless of the state of the menu that triggered it.
                        ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos(), ImGuiCond.Appearing);
                        if (ImGui.BeginPopupModal($"object_deletion_confirmation_{key}"))
                        {
                            ImGui.Text($"Confirm delete object \"{key}\"?");
                            if (ImGui.Button("Cancel")) { ImGui.CloseCurrentPopup(); }
                            ImGui.SameLine();
                            if (ImGui.Button("Delete")) { Remove(key); ImGui.CloseCurrentPopup(); }
                            ImGui.EndPopup();
                        }

                        // Similar to a deletion popup, we have a popup for copying the XML source
                        // of a game object. It's shown using the same trick of a flag + popup.
                        if (openXMLCopyingPopup)
                        {
                            ImGui.OpenPopup($"object_copy_xml_{key}");
                            openXMLCopyingPopup = false;
                        }

                        // The actual popup for copying the XML source. We use BeginPopupModal to
                        // prevent clicks outside the popup, because we don't want the object to be
                        // deleted or anything.
                        ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos(), ImGuiCond.Appearing);
                        if (ImGui.BeginPopupModal($"object_copy_xml_{key}"))
                        {
                            // Generate the XML source for the game object
                            string element = SceneDescriptionIO.GameObjectToXML(
                                Services.GetService<ContentManager>(),
                                key,
                                gameObject).ToString();
                            ImGui.Text("XML Source: (ctrl/cmd + A to select all before copying)");
                            // Show a read-only input box where text can be copied
                            ImGui.InputTextMultiline("##xml" + key, ref element, 1000, new System.Numerics.Vector2(500, 200), ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

                            // We need some way of exiting, since a popup modal can't be exited in
                            // another way
                            if (ImGui.Button("Done")) { ImGui.CloseCurrentPopup(); }
                            ImGui.EndPopup();
                        }
                    }
                    ImGui.EndChild();
                }

                // A button to launch the popup for creating a new object. The actual popup is
                // defined later.
                if (ImGui.Button("Create New Object", new System.Numerics.Vector2(sideBarWidth, 0f)))
                {
                    objectCreationEntity = null;
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

                        ImGui.Text($"Object details: {objectListCurrentSelection}");

                        ImGui.Separator();

                        // Draw the game object UI, using the most specific implementation of UI()
                        (gameObject as IImGui)?.UI();
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

                if (ImGui.BeginCombo("Attached physics body", objectCreationEntity?.GetType()?.Name ?? "..."))
                {
                    if (ImGui.Selectable("<null>"))
                    {
                        objectCreationEntity = null;
                    }
                    if (ImGui.Selectable("Box"))
                    {
                        // Default to a unit cube at 0,0,0. This will be moved upon hitting the
                        // creation button.
                        objectCreationEntity = new Box(BEPUutilities.Vector3.Zero, 1f, 1f, 1f);
                    }
                    if (ImGui.Selectable("Sphere"))
                    {
                        objectCreationEntity = new Sphere(BEPUutilities.Vector3.Zero, 1f);
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button("Create"))
                {
                    // Generate a name for the object.
                    string name = GenerateUniqueNameWithPrefix(Type.GetType(objectCreationSelectedFqn).Name.ToLower());

                    // Set up the physics body if we said we desired one
                    if (objectCreationEntity != null)
                    {
                        objectCreationEntity.Position = MathConverter.Convert(objectCreationPosition);
                    }

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
                            objectCreationScale,
                            objectCreationEntity
                        }
                    });

                    // Move the newly created object just under the focused object
                    ReorderObject(name, GameObjects.IndexOf(objectListCurrentSelection) + 1);

                    // Set sidebar focus to created object
                    objectListCurrentSelection = name;
                }
                ImGui.EndPopup();
            }
        }
    }
}

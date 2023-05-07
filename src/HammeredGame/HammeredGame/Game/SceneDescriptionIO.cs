using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using HammeredGame.Game.PathPlanning.Grid;
using HammeredGame.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SoftCircuits.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HammeredGame.Game
{
    /// <summary>
    /// A class for loading and writing XML scene description files to and from GameObjects.
    /// </summary>
    internal static class SceneDescriptionIO
    {
        /// <summary>
        /// Generic function to parse basic data to classes.
        /// </summary>
        /// <typeparam name="T">Supports bool, float, int, XNA.Vector3, and XNA.Quaternion</typeparam>
        /// <param name="text">String to parse</param>
        /// <returns>null if the parsing failed, otherwise contains the parsed object</returns>
        internal static T Parse<T>(string text)
        {
            /// <value>
            /// The <code>string[] tokens</code> variable is used to split the singlue input string <code>text</code>
            /// of the XML file into multiple values.
            /// This is required in cases where classes are determined by more than one parameters.
            ///
            /// <example>
            /// Input: "position" == in XML == <position>X Y Z</position>
            /// 1) Split "X Y Z" into <code>float tokens[3] = {"X", "Y", "Z"} </code>
            /// 2) Parse each element of "tokens" independently as scalars.
            /// 3) Create the Vector3 object.
            /// </example>
            ///
            /// </value>
            string[] tokens; // Is used to split the single input string in the XML file to multiple values.


            /// <remarks>
            /// The <code>typeof(T).Name</code> function follows the name of the value types of .NET
            /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-types"/>
            ///
            /// For the built-in types being handled in the following switch case, read:
            ///
            /// "bool" -> Boolean
            /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool"/>
            ///
            /// "int" -> Int32
            /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types"/>
            ///
            /// "float" -> Single
            /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types"/>
            ///
            /// </remarks>
            switch (typeof(T).Name)
            {
                case "Boolean":
                    return (T)(object)bool.Parse(text);

                case "Int32":
                    return (T)(object)int.Parse(text);

                /// <remarks> Reminder: "float" is an alias for <c>System.Single</c> of .NET </remarks>
                case "Single":
                    // Make sure to parse floats with a culture-agnostic way that treats dots as decimal point.
                    // This is to successfully load data to systems with different locale s, by bypassing them.
                    return (T)(object)float.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

                case "Vector3":
                    tokens = text.Split(" ");
                    return (T)(object)new Vector3(Parse<float>(tokens[0]), Parse<float>(tokens[1]), Parse<float>(tokens[2]));

                case "Quaternion":
                    tokens = text.Split(" ");
                    return (T)(object)new Quaternion(Parse<float>(tokens[0]), Parse<float>(tokens[1]), Parse<float>(tokens[2]), Parse<float>(tokens[3]));

                default:
                    return (T)(object)null;
            }
        }

        /// <summary>
        /// Generic function to write basic classes to strings.
        /// </summary>
        /// <typeparam name="T">Supports bool, float, int, XNA.Vector3, and XNA.Quaternion</typeparam>
        /// <param name="obj">The object to show as string</param>
        /// <returns>
        /// Empty string if the conversion failed, otherwise the string representation of the object
        /// </returns>
        internal static string Show<T>(T obj)
        {
            switch (typeof(T).Name)
            {
                case "Boolean":
                    return obj.ToString();

                case "Int32":
                    return obj.ToString(); // Check if this is correct?

                case "Single":
                    // Show floats up to 3 decimal points, using the dot as the decimal point
                    return ((float)(object)obj).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

                case "Vector3":
                    Vector3 vec3 = (Vector3)(object)obj;
                    return string.Join(" ", Show<float>(vec3.X), Show<float>(vec3.Y), Show<float>(vec3.Z));

                case "Quaternion":
                    Quaternion quat = (Quaternion)(object)obj;
                    return string.Join(" ", Show<float>(quat.X), Show<float>(quat.Y), Show<float>(quat.Z), Show<float>(quat.W));

                default:
                    return "";
            }
        }

        /// <summary>
        /// Construct a XML level parser from a given file path relative to the binary directory. To
        /// add new level XML files that are recognized here, make sure to add the XML in the
        /// Content directory of the repository, followed by using the MGCB editor to add the file.
        /// Mark it as Copy (instead of Build). Then pass a path prepended with Content/ to this method.
        /// </summary>
        /// <param name="filePath">The file path to the XML file</param>
        /// <param name="services">
        /// Core game services, used to load content and to be passed to the game objects
        /// </param>
        /// <exception cref="Exception">Raised if the XML file could not be loaded</exception>
        /// <remarks>
        /// WARNING: A quick patch is implemented for parsing the grid of the scene, necessary for
        ///          the path planning. There could be better ways to implement it better. Adding
        /// more and more explicit outputs in the definition of the function is not maintenance-friendly.
        /// </remarks>
        public static async Task<(Camera, SceneLightSetup, OrderedDictionary<string, GameObject>, UniformGrid grid)> ParseFromXML(string filePath, GameServices services)
        {
            System.Diagnostics.Debug.WriteLine("Parsing XML from " + Thread.CurrentThread.ManagedThreadId);
            var loadedXML = File.ReadAllText(filePath, Encoding.UTF8);
            var xml = XDocument.Parse(loadedXML);

            //Sanity check
            if (xml == null)
            {
                throw new Exception("Could not load XML file: " + filePath);
            }

            Camera cam = GetCamera(xml, services);
            UniformGrid grid = await GetUniformGrid(xml);
            SceneLightSetup lights = GetLightSetup(xml);
            System.Diagnostics.Debug.WriteLine("load!");
            OrderedDictionary<string, GameObject> objs = await GetGameObjects(xml, services);
            System.Diagnostics.Debug.WriteLine("donesies!");
            return (cam, lights, objs, grid);
        }

        /// <summary>
        /// Find the single top-level camera tag and parse it, returning the instantiated Camera object.
        /// </summary>
        /// <returns>The instantiated Camera object</returns>
        private static Camera GetCamera(XDocument targetXML, GameServices services)
        {
            // Find the top level camera object (otherwise .Descendants("camera") returns nested child ones too)
            XElement cameraElement = targetXML.Root.Descendants("camera").Single(child => child.Parent == targetXML.Root);

            // Parse the target and positions, which are required for the constructor
            Vector3 focusPoint = Parse<Vector3>(cameraElement.Descendants("target").Single().Value);
            Vector3 upDirection = Parse<Vector3>(cameraElement.Descendants("up").Single().Value);

            // Instantiate the Camera object
            Camera cameraInstance = new Camera(services, focusPoint, upDirection);

            // Determine the camera mode
            switch (cameraElement.Attribute("mode")?.Value)
            {
                case "follow":
                    // Set the distance and angle for follow mode. The position is not necessary
                    // since it will be dynamically calculated at runtime from the distance, angle,
                    // and the 4 isometric view directions.
                    cameraInstance.Mode = Camera.CameraMode.Follow;
                    cameraInstance.FollowDistance = Parse<float>(cameraElement.Descendants("follow_distance").Single().Value);

                    #region TRANSITION_CODE_REMOVE_LATER
                    // if the old format with a single angle and dir_index is still used, load it but convert
                    if (cameraElement.Descendants("follow_angle").Any())
                    {
                        cameraInstance.FollowAngleVertical = Parse<float>(cameraElement.Descendants("follow_angle").Single().Value);
                        cameraInstance.FollowAngleHorizontal = (Parse<int>(cameraElement.Descendants("dir_index").SingleOrDefault()?.Value ?? "0") + 1) * MathHelper.PiOver2 + MathHelper.PiOver4;
                    }
                    else
                    {
                        cameraInstance.FollowAngleVertical = Parse<float>(cameraElement.Descendants("follow_angle_v").Single().Value);
                        cameraInstance.FollowAngleHorizontal = Parse<float>(cameraElement.Descendants("follow_angle_h").Single().Value);
                    }
                    #endregion

                    break;

                case "static":
                default:
                    // Set the four static positions.
                    cameraInstance.Mode = Camera.CameraMode.FourPointStatic;
                    cameraInstance.StaticPositions = cameraElement.Descendants("position").Select(v => Parse<Vector3>(v.Value)).ToArray();
                    cameraInstance.CurrentCameraDirIndex = Parse<int>(cameraElement.Descendants("dir_index").SingleOrDefault()?.Value ?? "0");
                    break;
            }

            // Set the field of view
            cameraInstance.FieldOfView = Parse<float>(cameraElement.Descendants("fov").Single().Value);

            return cameraInstance;
        }


        /// <summary>
        /// Identify the single top-level "uniform_grid" tag, parse it and return it as an instantiated <c>UniformGrid</c> object.
        /// <see cref="UniformGrid"/>
        /// </summary>
        /// <param name="targetXML">The XML document from which the required data is extracted. <see cref="ParseFromXML(string, GameServices)"/></param>
        ///
        /// <remarks>
        /// 1) The current implementation utilizes the constructor <see cref="UniformGrid.UniformGrid(Vector3, Vector3, float)"/>
        /// 2) TODO: Make it possible to create an object using different constructors. Risk severity assessment: Level 4 (Negligible)
        ///         This feature allows the user (XML writer) to be more versatile about how they go about writing the definition of
        ///         the grid, taking advantage of the different constructors of the class <see cref="UniformGrid"/>.
        /// </remarks>
        private static async Task<UniformGrid> GetUniformGrid(XDocument targetXML)
        {
            // It is expected that a scene consists of a single grid.
            // However, taking precautions for incorrect user (XML writer) input, the unique top-level grid is extracted.
            XElement uniformGridElement = targetXML.Root.Descendants("uniform_grid").Single(child => child.Parent == targetXML.Root);


            // Parse
            // i) the two (2) points, which define an orthogonal parallelepiped, and
            // ii)  the side length, which defines the partitioning of the aforementioned parallelepiped
            Vector3 bottomLeftClosePoint = Parse<Vector3>(uniformGridElement.Descendants("bottom_left_close").Single().Value); // i)
            Vector3 topRightAwayPoint = Parse<Vector3>(uniformGridElement.Descendants("top_right_away").Single().Value); // i)
            float sideLength = Parse<float>(uniformGridElement.Descendants("side_length").Single().Value); // ii)

            // Instantiate the <c>UniformGrid</c> object.

            return await Task.Run(() => new UniformGrid(bottomLeftClosePoint, topRightAwayPoint, sideLength));
        }

        /// <summary>
        /// Find the single top-level lights tag and parse it, returning the instantiated
        /// SceneLightSetup object.
        /// </summary>
        /// <param name="targetXML">The XML to parse</param>
        /// <returns>The instantiated SceneLightSetup object</returns>
        private static SceneLightSetup GetLightSetup(XDocument targetXML)
        {
            try
            {
                // Find the top level lights object (otherwise .Descendants("lights") returns nested child ones too)
                XElement lightsElement = targetXML.Root.Descendants("lights").Single(child => child.Parent == targetXML.Root);

                XElement sunElement = lightsElement.Descendants("sun").Single();
                Vector3 lightColor = Parse<Vector3>(sunElement.Descendants("color").Single().Value);
                Vector3 direction = Parse<Vector3>(sunElement.Descendants("direction").Single().Value);
                float intensity = Parse<float>(sunElement.Descendants("intensity").Single().Value);
                SunLight sun = new(new Color(lightColor), intensity, direction);

                List<InfiniteDirectionalLight> dirLights = new();

                foreach (XElement directionalElement in lightsElement.Descendants("directional"))
                {
                    lightColor = Parse<Vector3>(directionalElement.Descendants("color").Single().Value);
                    direction = Parse<Vector3>(directionalElement.Descendants("direction").Single().Value);
                    intensity = Parse<float>(directionalElement.Descendants("intensity").Single().Value);
                    dirLights.Add(new(new Color(lightColor), intensity, direction));
                }

                XElement ambientElement = lightsElement.Descendants("ambient").Single();
                lightColor = Parse<Vector3>(ambientElement.Descendants("color").Single().Value);
                intensity = Parse<float>(ambientElement.Descendants("intensity").Single().Value);
                AmbientLight ambient = new(new Color(lightColor), intensity);

                return new SceneLightSetup(sun, dirLights, ambient);
            }
            catch (Exception)
            {
                // Return a default light setup
                return new(
                   new SunLight(Color.LightYellow, 1f, new Vector3(0, 0.97f, 0.20f)),
                   new List<InfiniteDirectionalLight> {
                        new InfiniteDirectionalLight(Color.White, 0.05f, new Vector3(0.2f, 0.97f, 0f))
                   },
                   new AmbientLight(Color.White, 0.01f)
               );
            }
        }

        /// <summary>
        /// Find the top-level object tags and parse them as GameObjects. Some objects require
        /// access to Input or Camera, so this method should be called once you have parsed those.
        /// This method returns a dictionary mapping unique identifier strings to objects. These
        /// identifiers are either taken from the XML if specified via the "id" attribute, or
        /// otherwise generated.
        /// </summary>
        /// <param name="targetXML">The root XML document containing the whole scene description</param>
        /// <param name="services">
        /// Core game services used to load assets and to pass to objects' constructors
        /// </param>
        /// <returns>A dictionary of unique IDs and parsed GameObjects</returns>
        /// <exception cref="Exception">When some trouble arises trying to create the object</exception>
        private static async Task<OrderedDictionary<string, GameObject>> GetGameObjects(XDocument targetXML, GameServices services)
        {
            // Maintain a map of named objects (those with attribute "id") so that we can reference
            // them and use them as arguments to constructors if needed in the future. This is used
            // for example for keys and doors, where the door needs to be present first in the XML
            // with an id, and then when parsing the key we'd have access to that.
            var namedObjects = new OrderedDictionary<string, GameObject>();

            foreach (var obj in targetXML.Root.Descendants("object"))
            {
                string fullyQualifiedClassName = obj.Attribute("type").Value;
                Type t = Type.GetType(fullyQualifiedClassName);
                if (t == null)
                {
                    throw new Exception("Type not found: " + fullyQualifiedClassName);
                }
                if (!t.IsSubclassOf(typeof(GameObject)))
                {
                    throw new Exception("Cannot create a class that's not a GameObject" + fullyQualifiedClassName);
                }

                // List of arguments to pass to the constructor. These have to match the types and
                // orders in the constructor exactly.
                List<object> arguments = new()
                {
                    services
                };

                // Model is an optional tag (and even if it exists, could have no value) for things
                // like trigger objects. So check if it's specified, and load it only if it is.
                // (FirstOrDefault() returns null if it doesn't find a matching entry)
                string modelName = obj.Descendants("model").FirstOrDefault()?.Value;
                Model model = null;
                if (!string.IsNullOrEmpty(modelName))
                {
                    model = await services.GetService<ContentManager>().LoadAsync<Model>(services.GetService<ScriptUtils>(), modelName);
                }
                arguments.Add(model);

                // Texture is an optional tag (and even if it exists, could have no value), so check
                // if we have it first. If we don't, then pass null as the Texture.
                // (FirstOrDefault() returns null if it doesn't find a matching entry)
                string textureName = obj.Descendants("texture").FirstOrDefault()?.Value;
                Texture2D texture = null;
                if (!string.IsNullOrEmpty(textureName))
                {
                    texture = await services.GetService<ContentManager>().LoadAsync<Texture2D>(services.GetService<ScriptUtils>(), textureName);
                }
                arguments.Add(texture);

                arguments.Add(Parse<Vector3>(obj.Descendants("position").Single().Value));
                arguments.Add(Parse<Quaternion>(obj.Descendants("rotation").Single().Value));
                arguments.Add(Parse<float>(obj.Descendants("scale").Single().Value));

                // Parse the physics body attached to this
                (Entity body, Vector3 bodyOffset) = GetBody(Parse<Vector3>(obj.Descendants("position").Single().Value), obj.Descendants("body").FirstOrDefault());
                arguments.Add(body);

                // Instantiate the GameObject. This can throw an exception if the arguments don't
                // match the constructors.
                GameObject instance = (GameObject)Activator.CreateInstance(t, args: arguments.ToArray());

                // Visibility is also not part of the constructor
                instance.Visible = Parse<bool>(obj.Descendants("visibility").SingleOrDefault()?.Value ?? "true");

                instance.EntityModelOffset = bodyOffset;

                // Insert into the dictionary to return together with either the ID attribute value or a generated id
                if (obj.Attribute("id") != null)
                {
                    // If the object was named, then store its ID
                    namedObjects.Add(obj.Attribute("id").Value, instance);
                }
                else
                {
                    // Generate a unique identifier by using the lowercase class name and appending numbers
                    string nameCandidate = t.Name.ToLower();
                    // Check until the candidate ID doesn't exist yet
                    for (int counter = 1; namedObjects.ContainsKey(nameCandidate); counter++)
                    {
                        nameCandidate = t.Name.ToLower() + counter.ToString();
                    }
                    namedObjects.Add(nameCandidate, instance);
                }
            }
            return namedObjects;
        }

        /// <summary>
        /// Generate a physics body Entity using the body tag specified in the XML. Supports box and sphere types.
        /// </summary>
        /// <param name="modelPosition">The position of the model</param>
        /// <param name="bodyElement">The XML element for the body tag</param>
        /// <returns>The BEPUphysics entity, and the visual offset</returns>
        private static (Entity, Vector3) GetBody(Vector3 modelPosition, XElement bodyElement)
        {
            if (bodyElement == null)
            {
                return (null, Vector3.Zero);
            }

            // All types of collision bodies can have an optional mass which makes it a dynamic body
            // (responsive to movement upon collision).
            XElement massElem = bodyElement.Descendants("mass").FirstOrDefault();
            Entity body;

            switch (bodyElement.Attribute("type").Value)
            {
                case "box":
                    // A Box needs to receive width, height, length and optionally mass in its constructor.
                    float width = Parse<float>(bodyElement.Descendants("width").First().Value);
                    float height = Parse<float>(bodyElement.Descendants("height").First().Value);
                    float length = Parse<float>(bodyElement.Descendants("length").First().Value);
                    if (massElem != null)
                    {
                        float mass = Parse<float>(massElem.Value);
                        body = new Box(MathConverter.Convert(modelPosition), width, height, length, mass);
                    }
                    else
                    {
                        body = new Box(MathConverter.Convert(modelPosition), width, height, length);
                    }
                    break;

                case "sphere":
                    // A sphere needs to receive radius and optionally mass.
                    float radius = Parse<float>(bodyElement.Descendants("radius").First().Value);
                    if (massElem != null)
                    {
                        float mass = Parse<float>(massElem.Value);
                        body = new Sphere(MathConverter.Convert(modelPosition), radius, mass);
                    }
                    else
                    {
                        body = new Sphere(MathConverter.Convert(modelPosition), radius);
                    }
                    break;

                default:
                    return (null, Vector3.Zero);
            }

            // The model origin may be different to the center of mass where the physics
            // engine treats as the origin. The shift_graphic value allows specifying how
            // much to shift the rendered model by compared to the collision body.
            return (body, bodyElement
                .Descendants("shift_graphic")
                .Select(a => Parse<Vector3>(a.Value))
                .FirstOrDefault(Vector3.Zero));
        }

        /// <summary>
        /// Write an XML file to the specified file path (with a prompt, overwriting any existing
        /// file contents). The content will be a valid scene description constructed from the
        /// various components of a scene passed in as arguments.
        /// </summary>
        /// <param name="filePath">The filepath to write the XML to.</param>
        /// <param name="camera">The camera instance to write to XML</param>
        /// <param name="namedObjects">The game objects in the scene and their unique ids</param>
        /// <param name="services">Core game services. This method needs the Content Manager.</param>
        /// <returns>Whether the write was successful or not</returns>
        public static bool WriteToXML(string filePath, Camera camera, SceneLightSetup lights, OrderedDictionary<string, GameObject> namedObjects, UniformGrid grid, GameServices services)
        {
            // Create the root <scene> tag and add the camera, grid, and lights
            XElement rootElement = new XElement("scene",
                CameraToXML(camera), UniformGridToXML(grid), LightsToXML(lights));

            // Then add all the game objects, including player and hammer
            foreach ((string id, GameObject gameObject) in namedObjects)
            {
                rootElement.Add(GameObjectToXML(services.GetService<ContentManager>(), id, gameObject));
            }

            // Show a file dialogue to prompt saving it
            NativeFileDialogSharp.DialogResult result = NativeFileDialogSharp.Dialog.FileSave("xml", filePath);
            try
            {
                if (result.IsOk)
                {
                    File.WriteAllText(result.Path, rootElement.ToString());
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Create a "camera" tag for the XML from a <see cref="Camera"/> instance.
        /// </summary>
        /// <param name="camera">The camera instance to write to XML</param>
        /// <returns>The camera XML element</returns>
        public static XElement CameraToXML(Camera camera)
        {
            // First create the global camera element
            XElement cameraElement = new XElement("camera");
            if (camera.Mode == Camera.CameraMode.FourPointStatic)
            {
                cameraElement.Add(
                    new XAttribute("mode", "static"),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[0])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[1])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[2])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[3])),
                    new XElement("dir_index",
                        Show<int>(camera.CurrentCameraDirIndex)));
            }
            else
            {
                cameraElement.Add(
                    new XAttribute("mode", "follow"),
                    new XElement("follow_distance",
                        Show<float>(camera.FollowDistance)),
                    new XElement("follow_angle_v",
                        Show<float>(camera.FollowAngleVertical)),
                    new XElement("follow_angle_h",
                        Show<float>(camera.FollowAngleHorizontal)));
            }
            cameraElement.Add(
                    new XElement("target",
                        Show<Vector3>(camera.Target)),
                    new XElement("up",
                        Show<Vector3>(camera.Up)),
                    new XElement("fov",
                        Show<float>(camera.FieldOfView)));
            return cameraElement;
        }

        /// <summary>
        /// Create a "uniform_grid" tag for the XML from a <see cref="UniformGrid"/> instance.
        /// </summary>
        /// <param name="grid">The uniform grid instance to write to XML</param>
        /// <returns>The grid XML element</returns>
        public static XElement UniformGridToXML(UniformGrid grid)
        {
            // Create the scene (uniform) grid XML element.
            return new XElement("uniform_grid",
                                            new XElement("bottom_left_close",
                                                Show<Vector3>(grid.originPoint)),
                                            new XElement("top_right_away",
                                                Show<Vector3>(grid.endPoint)),
                                            new XElement("side_length",
                                                Show<float>(grid.sideLength))
                                            );
        }

        /// <summary>
        /// Create a "lights" tag for the XML from a <see cref="SceneLightSetup"/> instance.
        /// </summary>
        /// <param name="lights">The scene light setup instance to write to XML</param>
        /// <returns>The lights XML element</returns>
        public static XElement LightsToXML(SceneLightSetup lights)
        {
            // Then create a <lights> tag for the scene light setup
            XElement lightsElement = new XElement("lights");
            lightsElement.Add(
                new XElement(
                    "sun",
                    new XElement("color", Show<Vector3>(lights.Sun.LightColor.ToVector3())),
                    new XElement("direction", Show<Vector3>(lights.Sun.Direction)),
                    new XElement("intensity", Show<float>(lights.Sun.Intensity))
                    )
                );
            foreach (InfiniteDirectionalLight dirLight in lights.Directionals)
            {
                lightsElement.Add(
                    new XElement(
                        "directional",
                        new XElement("color", Show<Vector3>(dirLight.LightColor.ToVector3())),
                        new XElement("direction", Show<Vector3>(dirLight.Direction)),
                        new XElement("intensity", Show<float>(dirLight.Intensity))
                        )
                    );
            }
            lightsElement.Add(
                new XElement(
                    "ambient",
                    new XElement("color", Show<Vector3>(lights.Ambient.LightColor.ToVector3())),
                    new XElement("intensity", Show<float>(lights.Ambient.Intensity))
                    )
                );
            return lightsElement;
        }

        /// <summary>
        /// Create an "object" tag for the XML from a <see cref="GameObject"/> instance.
        /// </summary>
        /// <param name="cm">The content manager, required to get the loaded model name</param>
        /// <param name="id">The string name/id of the object in the scene</param>
        /// <param name="gameObject">The game object instance to write to the XML</param>
        /// <returns>The camera XML element</returns>
        public static XElement GameObjectToXML(ContentManager cm, string id, GameObject gameObject)
        {
            // Basic attributes
            XElement objElement = new("object",
                new XAttribute("type", gameObject.GetType().FullName),
                new XAttribute("id", id));

            // For the model, gameObject.Model does not contain the name of the file used to
            // load it, so we will look into the Content Manager's internal cache to find the
            // key matching the same object reference.
            Dictionary<string, object> loadedAssets = (Dictionary<string, object>)typeof(ContentManager).GetField("loadedAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cm);

            string modelName = loadedAssets.Where(asset => asset.Value == gameObject.Model).Select(a => a.Key).FirstOrDefault();
            objElement.Add(new XElement("model", modelName));

            // Texture can be null, but if it exists, we already have the name available to us
            if (gameObject.Texture != null)
            {
                objElement.Add(new XElement("texture", gameObject.Texture.Name));
            }

            objElement.Add(new XElement("position", Show<Vector3>(gameObject.Position)));
            objElement.Add(new XElement("rotation", Show<Quaternion>(gameObject.Rotation)));
            objElement.Add(new XElement("scale", Show<float>(gameObject.Scale)));

            // Add visibility tag only if it's not visible
            if (!gameObject.Visible)
            {
                objElement.Add(new XElement("visibility", Show<bool>(gameObject.Visible)));
            }

            // Add the physics body entity (if one exists)
            if (gameObject.Entity != null)
            {
                if (gameObject.Entity is Box box)
                {
                    objElement.Add(new XElement("body",
                        new XAttribute("type", "box"),
                        new XElement("width", Show<float>(box.Width)),
                        new XElement("height", Show<float>(box.Height)),
                        new XElement("length", Show<float>(box.Length)),
                        box.IsDynamic ? new XElement("mass", Show<float>(box.Mass)) : null,
                        new XElement("shift_graphic", Show<Vector3>(gameObject.EntityModelOffset))));
                }
                else if (gameObject.Entity is Sphere sph)
                {
                    objElement.Add(new XElement("body",
                        new XAttribute("type", "sphere"),
                        new XElement("radius", Show<float>(sph.Radius)),
                        sph.IsDynamic ? new XElement("mass", Show<float>(sph.Mass)) : null,
                        new XElement("shift_graphic", Show<Vector3>(gameObject.EntityModelOffset))));
                }
            }
            return objElement;
        }
    }
}

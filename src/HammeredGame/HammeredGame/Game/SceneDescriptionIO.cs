using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using Hammered_Physics.Core;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <typeparam name="T">Supports bool, float, XNA.Vector3, and XNA.Quaternion</typeparam>
        /// <param name="text">String to parse</param>
        /// <returns>null if the parsing failed, otherwise contains the parsed object</returns>
        internal static T Parse<T>(string text)
        {
            string[] tokens;
            switch (typeof(T).Name)
            {
                case "Boolean":
                    return (T)(object)bool.Parse(text);

                case "Single":
                    // Make sure to parse floats with a culture-agnostic way that treats dots as
                    // decimal point
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
        /// <typeparam name="T">Supports bool, float, XNA.Vector3, and XNA.Quaternion</typeparam>
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
        /// <exception cref="Exception">Raised if the XML file could not be loaded</exception>
        public static (Camera, Dictionary<string, GameObject>) ParseFromXML(string filePath, GameServices services)
        {
            var loadedXML = File.ReadAllText(filePath, Encoding.UTF8);
            var xml = XDocument.Parse(loadedXML);

            //Sanity check
            if (xml == null)
            {
                throw new Exception("Could not load XML file: " + filePath);
            }

            Camera cam = GetCamera(xml, services);
            Dictionary<string, GameObject> objs = GetGameObjects(xml, services, cam);

            return (cam, objs);
        }

        /// <summary>
        /// Find the single top-level camera tag and parse it, returning the instantiated Camera object.
        /// </summary>
        /// <param name="gpu">The gpu parameter to pass to the Camera constructor</param>
        /// <param name="input">The input parameter to pass to the Camera constructor</param>
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

            // Set the four static positions. (We /could/ modify the Camera constructor to pass
            // these too, but it gets a little long. Also, on the off chance we need a camera that
            // follows the player closely for one level, the current approach leaves more freedom.
            cameraInstance.StaticPositions = cameraElement.Descendants("position").Select(v => Parse<Vector3>(v.Value)).ToArray();

            return cameraInstance;
        }

        /// <summary>
        /// Find the top-level object tags and parse them as GameObjects. Some objects require
        /// access to Input or Camera, so this method should be called once you have parsed those.
        /// This method returns a dictionary mapping unique identifier strings to objects. These
        /// identifiers are either taken from the XML if specified via the "id" attribute, or
        /// otherwise generated.
        /// </summary>
        /// <param name="cm">ContentManager used for loading Models and Textures</param>
        /// <param name="input">Input parameter to pass to the constructors if necessary</param>
        /// <param name="cam">Camera parameter to pass to the constructors if necessary</param>
        /// <returns>A dictionary of unique IDs and parsed GameObjects</returns>
        /// <exception cref="Exception">When some trouble arises trying to create the object</exception>
        private static Dictionary<string, GameObject> GetGameObjects(XDocument targetXML, GameServices services, Camera cam)
        {
            var gameObjects = new List<GameObject>();

            // Maintain a map of named objects (those with attribute "id") so that we can reference
            // them and use them as arguments to constructors if needed in the future. This is used
            // for example for keys and doors, where the door needs to be present first in the XML
            // with an id, and then when parsing the key we'd have access to that.
            var namedObjects = new Dictionary<string, GameObject>();

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

                Model model = services.GetService<ContentManager>().Load<Model>((obj.Descendants("model").Single()).Value);
                arguments.Add(model);

                // Texture is an optional tag, so check if we have it first. If we don't, then pass
                // null as the Texture. (FirstOrDefault() returns null if it doesn't find a matching entry)
                XElement textureElement = obj.Descendants("texture").FirstOrDefault();
                Texture2D texture = null;
                if (textureElement != null)
                {
                    texture = services.GetService<ContentManager>().Load<Texture2D>(textureElement.Value);
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
                    for (int counter = 1; namedObjects.GetValueOrDefault(nameCandidate) != null; counter++)
                    {
                        nameCandidate = t.Name.ToLower() + counter.ToString();
                    }
                    namedObjects.Add(nameCandidate, instance);
                }
            }
            return namedObjects;
        }

        /// <summary>
        /// Generate a physics body Entity using the body tag specified in the XML. Supports box types.
        /// </summary>
        /// <param name="modelPosition">The position of the model</param>
        /// <param name="bodyElement">The XML element for the body tag</param>
        /// <returns></returns>
        private static (Entity, Vector3) GetBody(Vector3 modelPosition, XElement bodyElement)
        {
            if (bodyElement == null)
            {
                return (null, Vector3.Zero);
            }

            switch (bodyElement.Attribute("type").Value)
            {
                case "box":
                    // A Box needs to receive width, height, length and optionally mass in its constructor.
                    // Specifying a mass makes it a dynamic body (responsive to movement upon collision).
                    float width = Parse<float>(bodyElement.Descendants("width").First().Value);
                    float height = Parse<float>(bodyElement.Descendants("height").First().Value);
                    float length = Parse<float>(bodyElement.Descendants("length").First().Value);
                    XElement massElem = bodyElement.Descendants("mass").FirstOrDefault();
                    Entity body;
                    if (massElem != null)
                    {
                        float mass = Parse<float>(massElem.Value);
                        body = new Box(MathConverter.Convert(modelPosition), width, height, length, mass);
                    }
                    else
                    {
                        body = new Box(MathConverter.Convert(modelPosition), width, height, length);
                    }
                    // The model origin may be different to the center of mass where the physics
                    // engine treats as the origin. The local position attribute allows shifting the
                    // center of mass.
                    return (body, bodyElement
                        .Descendants("shift_graphic")
                        .Select(a => Parse<Vector3>(a.Value))
                        .FirstOrDefault(Vector3.Zero));
                default:
                    return (null, Vector3.Zero);
            }
        }

        /// <summary>
        /// Write an XML file to the specified file path (overwriting any existing file contents).
        /// The content will be a valid scene description constructed from the Camera instance
        /// passed to this method, and the dictionary of objects and their unique identifiers.
        /// </summary>
        /// <param name="filePath">The filepath to write the XML to. </param>
        /// <param name="camera">The camera instance to write to XML</param>
        /// <param name="namedObjects">The game objects in the scene and their unique ids</param>
        /// <param name="services">Core game services. This method needs the Content Manager.</param>
        /// <returns>Whether the write was successful or not</returns>
        public static bool WriteToXML(string filePath, Camera camera, Dictionary<string, GameObject> namedObjects, GameServices services)
        {
            // First create the global camera element
            XElement cameraElement =
                new XElement("camera",
                    new XAttribute("type", "static"),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[0])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[1])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[2])),
                    new XElement("position",
                        Show<Vector3>(camera.StaticPositions[3])),
                    new XElement("target",
                        Show<Vector3>(camera.Target)),
                    new XElement("up",
                        Show<Vector3>(camera.Up)));

            // Create the root <level> tag and add the camera
            XElement rootElement = new XElement("scene",
                cameraElement);

            // Then add all the game objects, including player and hammer
            foreach ((string id, GameObject gameObject) in namedObjects)
            {
                // Basic attributes
                XElement objElement = new("object",
                    new XAttribute("type", gameObject.GetType().FullName),
                    new XAttribute("id", id));

                // For the model, gameObject.Model does not contain the name of the file used to
                // load it, so we will look into the Content Manager's internal cache to find the
                // key matching the same object reference.
                Dictionary<string, object> loadedAssets = (Dictionary<string, object>)typeof(ContentManager).GetField("loadedAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(services.GetService<ContentManager>());
                string modelName = loadedAssets.Where(asset => asset.Value == gameObject.Model).Select(a => a.Key).FirstOrDefault();
                objElement.Add(new XElement("model", modelName));

                // Texture can be null
                if (gameObject.Texture != null)
                {
                    objElement.Add(new XElement("texture",
                        new XAttribute("type", "texture2d"),
                        gameObject.Texture.Name));
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
                }

                rootElement.Add(objElement);
            }

            NativeFileDialogSharp.DialogResult result = NativeFileDialogSharp.Dialog.FileSave("xml", filePath);
            try
            {
                if (result.IsOk)
                {
                    File.WriteAllText(result.Path, rootElement.ToString());
                    return true;
                }
                return false;
            } catch (Exception e)
            {
                return false;
            }
        }
    }
}

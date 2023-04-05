using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace HammeredGame.Game
{
    /// <summary>
    /// A class for loading XML files into GameObjects.
    /// </summary>
    internal class XMLLevelLoader
    {
        /// <summary>
        /// The XML document once loaded through the constructor.
        /// </summary>
        private XDocument targetXML;

        internal static T Parse<T>(string text)
        {
            string[] tokens;
            string test = typeof(T).Name;
            switch (typeof(T).Name)
            {
                case "Boolean":
                    return (T)(object)bool.Parse(text);
                case "Single":
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
        /// Construct a XML level parser from a given file path under the Content directory. To add
        /// new level XML files that are recognized here, make sure to add the XML in the Content
        /// directory of the repository, followed by using the MGCB editor to add the file. Mark it
        /// as Copy (instead of Build).
        /// </summary>
        /// <param name="filePath">The file path to the XML file, under the Content directory</param>
        /// <exception cref="Exception">Raised if the XML file could not be loaded</exception>
        public XMLLevelLoader(string filePath)
        {
            var loadedXML = File.ReadAllText("Content/" + filePath, Encoding.UTF8);
            var xml = XDocument.Parse(loadedXML);

            //Sanity check
            if (xml == null)
            {
                throw new Exception("Could not load XML file: " + filePath);
            }

            targetXML = xml;
        }

        /// <summary>
        /// Find the single top-level camera tag and parse it, returning the instantiated Camera object.
        /// </summary>
        /// <param name="gpu">The gpu parameter to pass to the Camera constructor</param>
        /// <param name="input">The input parameter to pass to the Camera constructor</param>
        /// <returns>The instantiated Camera object</returns>
        public Camera GetCamera(GraphicsDevice gpu, Input input)
        {
            // FInd the top level camera object (otherwise .Descendants("camera") returns nested child ones too)
            XElement cameraElement = targetXML.Root.Descendants("camera").Single(child => child.Parent == targetXML.Root);

            // Parse the target and positions, which are required for the constructor
            Vector3 focusPoint = Parse<Vector3>(cameraElement.Descendants("target").Single().Value);
            Vector3 upDirection = Parse<Vector3>(cameraElement.Descendants("up").Single().Value);

            // Instantiate the Camera object
            Camera cameraInstance = new Camera(gpu, focusPoint, upDirection, input);

            // Set the four static positions. (We /could/ modify the Camera constructor to pass
            // these too, but it gets a little long. Also, on the off chance we need a camera that
            // follows the player closely for one level, the current approach leaves more freedom.
            cameraInstance.StaticPositions = (from vecs in cameraElement.Descendants("position")
                                   where vecs.Attribute("type").Value == "vec3"
                                   select vecs).Select(v => Parse<Vector3>(v.Value)).ToArray();

            return cameraInstance;
        }

        /// <summary>
        /// Find the top-level object tags and parse them as GameObjects. Some objects require
        /// access to Input or Camera, so this method should be called once you have parsed those.
        ///
        /// </summary>
        /// <param name="cm">ContentManager used for loading Models and Textures</param>
        /// <param name="input">Input parameter to pass to the constructors if necessary</param>
        /// <param name="cam">Camera parameter to pass to the constructors if necessary</param>
        /// <returns>List of parsed GameObjects</returns>
        /// <exception cref="Exception">When some trouble arises trying to create the object</exception>
        public List<GameObject> GetGameObjects(GameServices services, Camera cam)
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
                XElement textureElement = (from tex in obj.Descendants("texture")
                                           where tex.Attribute("type").Value == "texture2d"
                                           select tex).FirstOrDefault();
                Texture2D texture = null;
                if (textureElement != null)
                {
                    texture = services.GetService<ContentManager>().Load<Texture2D>(textureElement.Value);
                }
                arguments.Add(texture);

                arguments.Add(Parse<Vector3>(obj.Descendants("position").Single().Value));
                arguments.Add(Parse<Quaternion>(obj.Descendants("rotation").Single().Value));
                arguments.Add(Parse<float>(obj.Descendants("scale").Single().Value));

                // If any other object-specific arguments are required by the constructor, they are
                // specified using other_constructor_args, which contains values in order. We don't
                // care about the tag name but we will use the object_ref_id attribute if it refers
                // to a previously declared ID, or use the type="" attribute for simple values.
                if (obj.Descendants("other_constructor_args").Any())
                {
                    foreach (XElement args in obj.Descendants("other_constructor_args").First().Descendants())
                    {
                        // Check if this argument is a reference to a previously defined object
                        if (args.Attribute("object_ref_id") != null)
                        {
                            switch (args.Attribute("object_ref_id").Value)
                            {
                                // The Camera and Input parameters aren't GameObjects, so hard-code
                                // their available IDs
                                case "main_camera":
                                    arguments.Add(cam);
                                    break;
                                default:
                                    // Attempt a look up, throw an Exception if it isn't declared previously
                                    arguments.Add(namedObjects[args.Attribute("object_ref_id").Value]);
                                    break;
                            }
                            continue;
                        }

                        // For simple values, parse using the static parsing map
                        switch (args.Attribute("type").Value)
                        {
                            case "vec3":
                                arguments.Add(Parse<Vector3>(args.Value));
                                break;
                            case "quaternion":
                                arguments.Add(Parse<Quaternion>(args.Value));
                                break;
                            case "float":
                                arguments.Add(Parse<float>(args.Value));
                                break;
                            case "boolean":
                                arguments.Add(Parse<bool>(args.Value));
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Instantiate the GameObject. This can throw an exception if the arguments don't
                // match the constructors.
                GameObject instance = (GameObject)Activator.CreateInstance(t, args:arguments.ToArray());

                // Visibility is also not part of the constructor
                instance.SetVisible(Parse<bool>(obj.Descendants("visibility").SingleOrDefault()?.Value ?? "true"));

                // If the object was named, then store its ID for future reference
                if (obj.Attribute("id") != null)
                {
                    namedObjects.Add(obj.Attribute("id").Value, instance);
                }

                gameObjects.Add(instance);
            }
            return gameObjects;
        }
    }
}

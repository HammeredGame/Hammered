using HammeredGame.Core;
using Microsoft.Xna.Framework;
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
    /// A class for loading XML files into GameObjects.
    /// </summary>
    internal class XMLLevelLoader
    {
        /// <summary>
        /// The XML document once loaded through the constructor.
        /// </summary>
        private XDocument targetXML;

        /// <summary>
        /// A static mapping of simple value types in XML to their parsing functions.
        /// </summary>
        private static Dictionary<string, Func<string, dynamic>> parserFor = new Dictionary<string, Func<string, dynamic>>()
        {
            { "vec3", (input) => ParseVector3(input) },
            { "quaternion", (input) => ParseQuaternion(input) },
            { "float", (input) => float.Parse(input, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) },
            { "boolean", (input) => bool.Parse(input) }
        };

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
        /// Parse three numbers (or floating point numbers, basically anything accepted by
        /// float.Parse()) separated by spaces, into a Vector3.
        /// </summary>
        /// <param name="spaceSeparatedNumbers">Space-separated numbers</param>
        /// <returns>Parsed Vector3</returns>
        private static Vector3 ParseVector3(string spaceSeparatedNumbers)
        {
            string[] tokens = spaceSeparatedNumbers.Split(" ");
            return new Vector3(parserFor["float"](tokens[0]), parserFor["float"](tokens[1]), parserFor["float"](tokens[2]));
        }

        /// <summary>
        /// Parse four numbers (or floating point numbers, basically anything accepted by
        /// float.Parse()) separated by spaces, into a Quaternion.
        /// </summary>
        /// <param name="spaceSeparatedNumbers">Space-separated numbers</param>
        /// <returns>Parsed Quaternion</returns>
        private static Quaternion ParseQuaternion(string spaceSeparatedNumbers)
        {
            string[] tokens = spaceSeparatedNumbers.Split(" ");
            return new Quaternion(parserFor["float"](tokens[0]), parserFor["float"](tokens[1]), parserFor["float"](tokens[2]), float.Parse(tokens[3]));
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
            Vector3 focusPoint = (Vector3)parserFor["vec3"]((cameraElement.Descendants("target").Single().Value));
            Vector3 upDirection = (Vector3)parserFor["vec3"]((cameraElement.Descendants("up").Single().Value));

            // Instantiate the Camera object
            Camera cameraInstance = new Camera(gpu, focusPoint, upDirection, input);

            // Set the four static positions. (We /could/ modify the Camera constructor to pass
            // these too, but it gets a little long. Also, on the off chance we need a camera that
            // follows the player closely for one level, the current approach leaves more freedom.
            cameraInstance.StaticPositions = (from vecs in cameraElement.Descendants("position")
                                   where vecs.Attribute("type").Value == "vec3"
                                   select vecs).Select(v => (Vector3)parserFor["vec3"](v.Value)).ToArray();

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
        public List<GameObject> GetGameObjects(Microsoft.Xna.Framework.Content.ContentManager cm, Input input, Camera cam)
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
                List<object> arguments = new List<object>();

                Model model = cm.Load<Model>((obj.Descendants("model").Single()).Value);
                arguments.Add(model);

                XElement posElement = obj.Descendants("position").Single();
                var pos = parserFor[posElement.Attribute("type").Value](posElement.Value);
                arguments.Add(pos);

                XElement scaElement = obj.Descendants("scale").Single();
                var scale = parserFor[scaElement.Attribute("type").Value](scaElement.Value);
                arguments.Add(scale);

                // Texture is an optional tag, so check if we have it first. If we don't, then pass
                // null as the Texture. (FirstOrDefault() returns null if it doesn't find a matching entry)
                XElement textureElement = (from tex in obj.Descendants("texture")
                                      where tex.Attribute("type").Value == "texture2d"
                                      select tex).FirstOrDefault();
                Texture2D texture = null;
                if (textureElement != null)
                {
                    texture = cm.Load<Texture2D>(textureElement.Value);
                }
                arguments.Add(texture);

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
                                case "input":
                                    arguments.Add(input);
                                    break;
                                default:
                                    // Attempt a look up, throw an Exception if it isn't declared previously
                                    arguments.Add(namedObjects[args.Attribute("object_ref_id").Value]);
                                    break;
                            }
                            continue;
                        }

                        // For simple values, parse using the static parsing map
                        arguments.Add(parserFor[args.Attribute("type").Value](args.Value));
                    }
                }

                // Instantiate the GameObject. This can throw an exception if the arguments don't
                // match the constructors.
                GameObject instance = (GameObject)Activator.CreateInstance(t, args:arguments.ToArray());

                // Rotation is not part of the constructor but is a public field, so we set it after creation
                instance.Rotation = (Quaternion)parserFor["quaternion"](obj.Descendants("rotation").Single().Value);

                // Visibility is also not part of the constructor
                instance.SetVisible((bool)parserFor["boolean"](obj.Descendants("visibility").SingleOrDefault()?.Value ?? "true"));

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

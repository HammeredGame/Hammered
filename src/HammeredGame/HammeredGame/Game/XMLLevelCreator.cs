using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Xml.Linq;

namespace HammeredGame.Game
{
    internal class XMLLevelCreator
    {
        private readonly Camera camera;
        private readonly List<GameObject> gameObjects;

        public XMLLevelCreator(Camera camera, List<GameObject> gameObjects)
        {
            this.camera = camera;
            this.gameObjects = gameObjects;
        }

        /// <summary>
        /// Return a string representation of a Vector3, with three floating point numbers separated
        /// by spaces.
        /// </summary>
        /// <param name="vec3">Vector3 to convert to string</param>
        /// <returns>Space-separated numbers</returns>
        private static string WriteVector3(Vector3 vec3)
        {
            return string.Join(" ", vec3.X.ToString(), vec3.Y.ToString(), vec3.Z.ToString());
        }

        /// <summary>
        /// Return a string representation of a Quaternion, with four floating point numbers
        /// separated by spaces.
        /// </summary>
        /// <param name="quat">Quaternion to convert to string</param>
        /// <returns>Space-separated numbers</returns>
        private static string WriteQuaternion(Quaternion quat)
        {
            return string.Join(" ", quat.X.ToString(), quat.Y.ToString(), quat.Z.ToString(), quat.W.ToString());
        }

        /// <summary>
        /// non functional stuff
        /// </summary>
        /// <param name="filename"></param>
        public void WriteXML(string filename)
        {
            // First create the global camera element
            XElement cameraElement =
                new XElement("camera",
                    new XAttribute("type", "static"),
                    new XElement("position",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.StaticPositions[0])),
                    new XElement("position",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.StaticPositions[1])),
                    new XElement("position",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.StaticPositions[2])),
                    new XElement("position",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.StaticPositions[3])),
                    new XElement("target",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.Target),
                    new XElement("up",
                        new XAttribute("type", "vec3"),
                        WriteVector3(camera.Up))));

            // Create the root <level> tag and add the camera
            XElement rootElement = new XElement("level",
                cameraElement);

            // Then add all the game objects, including player and hammer
            foreach (GameObject gameObject in gameObjects)
            {
                // Basic attributes
                XElement objElement = new XElement("object",
                    new XAttribute("type", gameObject.GetType().FullName),
                    new XElement("model", gameObject.Model.ToString()),
                    new XElement("position",
                        new XAttribute("type", "vec3"),
                        WriteVector3(gameObject.Position)),
                    new XElement("rotation",
                        new XAttribute("type", "quaternion"),
                        WriteQuaternion(gameObject.Rotation)),
                    new XElement("scale",
                        new XAttribute("type", "float"),
                        gameObject.Scale.ToString())
                );

                // Texture is optional
                if (gameObject.Texture != null)
                {
                    objElement.Add(new XElement("texture",
                        new XAttribute("type", "texture2d"),
                        gameObject.Texture.Name));
                }

                // Add visibility tag only if it's not visible
                if (!gameObject.IsVisible())
                {
                    objElement.Add(new XElement("visibility",
                        new XAttribute("type", "boolean"),
                        gameObject.IsVisible().ToString()));
                }

                // Get other constructor arguments......
                // TODO: figure out how to get the required things??? how do we
                // change from an object reference to a generated ID, how do we
                // find out if e.g. the Input argument passed in is really for the
                // global Input or if it's something else that needs treatment
                // ideally we want to find out what the array of arguments that
                // the instance was constructed with and modify only the pos/rot/scale
                // values... how?
                var ctor = gameObject.GetType().GetConstructors()[0];
                ctor.GetParameters();

                rootElement.Add(objElement);
            }

            // write out rootElement to somewhere

        }
    }
}

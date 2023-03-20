using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame
{
    public abstract class GameObject
    {

        // Common variables for any object in the game (will be modified as we develop further)
        public Model model;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public Texture2D tex;

        // Change/remove once we modify how collisions / obstacles work
        public bool destroyed = false;

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(Matrix view, Matrix projection);

        // Common method to draw 3D models
        public void DrawModel(Model model, Matrix world, Matrix view, Matrix projection, Texture2D? tex)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                // Set the effect class for each mesh part in the model
                // This is most likely where we attach shaders to the model/mesh
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    //effect.LightingEnabled = Keyboard.GetState().IsKeyUp(Keys.L);
                    effect.LightingEnabled = true;
                    effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                    effect.DirectionalLight0.Direction = new Vector3(1, 1, 0);
                    effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

                    if (tex != null)
                    {
                        effect.TextureEnabled = true;
                        effect.Texture = tex;
                    }
                }

                mesh.Draw();
            }
        }

        // Method to get bounding box for the mesh - for basic collision detection
        // Probably not going to be necessary for later iterations
        // (once we bring in an external library to handle collisions)
        public BoundingBox GetBounds()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Get bounding box min/max from each mesh part's vertices
            foreach (ModelMesh mesh in this.model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                    int vertexBufferSize = meshPart.NumVertices * vertexStride;

                    int vertexDataSize = vertexBufferSize / sizeof(float);
                    float[] vertexData = new float[vertexDataSize];
                    meshPart.VertexBuffer.GetData<float>(vertexData);

                    for (int i = 0; i < vertexDataSize; i += vertexStride / sizeof(float))
                    {
                        Vector3 vertex = this.position + new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
                        min = Vector3.Min(min, vertex);
                        max = Vector3.Max(max, vertex);
                    }
                }
            }

            return new BoundingBox(min, max);
        }

        // Getter function for game object position
        public Vector3 GetPosition()
        {
            return this.position;
        }

        // Getter function for game object rotation
        public Quaternion GetRotation()
        {
            return this.rotation;
        }
    }
}

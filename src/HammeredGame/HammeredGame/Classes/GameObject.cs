using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Classes
{
    public abstract class GameObject
    {
        // Common variables for any object in the game (will be modified as we develop further)
        public Model model;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public Texture2D tex;

        //public Matrix additionalTransformation = Matrix.Identity;

        public Camera activeCamera;

        // Change/remove once we modify how collisions / obstacles work
        public bool destroyed = false;
        protected bool visible = true;

        public GameObject(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
        {
            this.model = model;
            this.position = pos;
            this.rotation = Quaternion.Identity;
            this.scale = scale;
            this.activeCamera = cam;
            this.tex = t;
        }
        public bool isVisible()
        {
            return this.visible;
        }

        public void setVisible(bool vis)
        {
            this.visible = vis;
        }

        public abstract void Update(GameTime gameTime);

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        public virtual void Draw(Matrix view, Matrix projection)
        {
            DrawModel(model, view, projection, tex);
        }

        // Common method to draw 3D models
        public void DrawModel(Model model, Matrix view, Matrix projection, Texture2D tex)
        {
            Matrix world = getWorldMatrix();

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                // Set the effect class for each mesh part in the model
                // This is most likely where we attach shaders to the model/mesh
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = meshTransforms[mesh.ParentBone.Index] * world;
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
            Matrix world = getWorldMatrix();
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Get bounding box min/max from each mesh part's vertices
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    // Get mesh transform with respect to world
                    Matrix meshTransform = boneTransforms[mesh.ParentBone.Index] * world;

                    int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                    int vertexBufferSize = meshPart.NumVertices * vertexStride;

                    int vertexDataSize = vertexBufferSize / sizeof(float);
                    float[] vertexData = new float[vertexDataSize];
                    meshPart.VertexBuffer.GetData(vertexData);

                    for (int i = 0; i < vertexDataSize; i += vertexStride / sizeof(float))
                    {
                        //Vector3 vertex = new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
                        Vector3 vertex = Vector3.Transform(new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]), meshTransform);
                        min = Vector3.Min(min, vertex);
                        max = Vector3.Max(max, vertex);
                    }
                }
            }

            return new BoundingBox(min, max);
        }

        public Matrix getWorldMatrix()
        {
            Vector3 pos = GetPosition();
            Quaternion rot = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            Matrix scaleMatrix = Matrix.CreateScale(scale);

            // Construct world matrix
            Matrix world = scaleMatrix * rotationMatrix * translationMatrix;
            return world;
        }

        // Getter function for game object position
        public Vector3 GetPosition()
        {
            return position;
        }

        // Getter function for game object rotation
        public Quaternion GetRotation()
        {
            return rotation;
        }
    }
}

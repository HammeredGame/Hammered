using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game
{
    /// <summary>
    /// The <c>GameObject</c> class encompasses all the visual components the player is going to encounter during their playthrough.
    /// The specific behaviours (either individual or inter-object) are to be defined in the respective subclasses
    /// (see the "class_skeleton_class_diagram.jpg" for a thorough class tree on the subclasses)
    /// 
    /// Any 3D object/shape in a geometric space can be uniquely defined by its:
    /// - (base) geometry -> <code>Model model</code> variable
    /// - the affine transformation applied to it:
    ///     -- translation -> <code>Vector3 position</code> variable
    ///     -- rotation -> <code>Quaternion rotation</code> variable
    ///     -- scaling -> <code>float scale</code> 
    ///     
    /// In addition, a shape (as defined above) may be cosmetically enhanced with a (.png) texture -> <code>Texture2D tex</code> variable.
    /// 
    /// For collision detection, each <c>GameObject</c> has bounding box attached -> <code>BoundingBox boundingBox</code> variable.
    /// </summary>
    public abstract class GameObject
    {
        // Common variables for any object in the game (will be modified as we develop further)
        public Model model;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public Texture2D tex;
        public BoundingBox boundingBox { get; private set; }

        // Change/remove once we modify how collisions / obstacles work (?)
        /// <summary>
        ///  The "flag" variable <code>bool visible</code> is used to indicate the state in which the <c>GameObject</c>
        ///  instance is in.
        ///  If its value is true, then the instance will be drawn on the screen (utilizing the <code>DrawModel()</code> function)
        /// </summary>
        protected bool visible = true;

        public GameObject(Model model, Vector3 pos, float scale, Texture2D t)
        {
            this.model = model;
            this.position = pos;
            this.rotation = Quaternion.Identity;
            this.scale = scale;
            this.tex = t;

            this.computeBounds();
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
            if (this.isVisible())
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
        public void computeBounds()
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

            this.boundingBox = new BoundingBox(min, max);
        }


        // Get the world matrix for the object's current position in the world
        // Mainly used for drawing
        public Matrix getWorldMatrix()
        {
            Vector3 pos = GetPosition();
            Quaternion rot = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            Matrix scaleMatrix = Matrix.CreateScale(scale);

            // Construct world matrix
            // Be careful! Order matters!
            // The transformations in this framework are applied FROM LEFT TO RIGHT
            // (in contrast with how it is done in mathematical notation).
            ///<example>
            /// Provided the transformation standard affine transformation matrices: Translation (T), Rotation (R) and Scaling (S)
            /// if we wish to apply the transformation: R -> T-> S on a vector
            /// we would express it in mathematical notation as STR,
            /// but as R * T * S in MonoGame (and OpenGL).
            ///</example>

            // World matrix = S -> R -> T
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

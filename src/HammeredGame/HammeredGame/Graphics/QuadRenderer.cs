
#region Using Statements

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#endregion

namespace HammeredGame.Graphics
{
    /// MIT License
    ///
    /// Copyright(c) 2023 Thomas Lüttich
    ///
    /// Permission is hereby granted, free of charge, to any person obtaining a copy
    /// of this software and associated documentation files (the "Software"), to deal
    /// in the Software without restriction, including without limitation the rights
    /// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    /// copies of the Software, and to permit persons to whom the Software is
    /// furnished to do so, subject to the following conditions:
    ///
    /// The above copyright notice and this permission notice shall be included in all
    /// copies or substantial portions of the Software.
    ///
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    /// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    /// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    /// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    /// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    /// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    /// SOFTWARE.
    ///
    /// <summary>
    /// Renders a simple quad to the screen. Uncomment the Vertex / Index buffers to make it a static fullscreen quad.
    /// The performance effect is barely measurable though and you need to dispose of the buffers when finished!
    /// </summary>
    public class QuadRenderer
    {
        //buffers for rendering the quad
        private readonly VertexPositionTexture[] vertexBuffer;
        private readonly short[] indexBuffer;

        //private VertexBuffer _vBuffer;
        //private IndexBuffer _iBuffer;

        public QuadRenderer(GraphicsDevice graphicsDevice)
        {
            vertexBuffer = new VertexPositionTexture[4];
            vertexBuffer[0] = new VertexPositionTexture(new Vector3(-1, 1, 1), new Vector2(0, 0));
            vertexBuffer[1] = new VertexPositionTexture(new Vector3(1, 1, 1), new Vector2(1, 0));
            vertexBuffer[2] = new VertexPositionTexture(new Vector3(-1, -1, 1), new Vector2(0, 1));
            vertexBuffer[3] = new VertexPositionTexture(new Vector3(1, -1, 1), new Vector2(1, 1));

            indexBuffer = new short[] { 0, 3, 2, 0, 1, 3 };

            //_vBuffer = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            //_iBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);

            //_vBuffer.SetData(_vertexBuffer);
            //_iBuffer.SetData(_indexBuffer);

        }

        public void RenderQuad(GraphicsDevice graphicsDevice, Vector2 v1, Vector2 v2)
        {
            vertexBuffer[0].Position.X = v1.X;
            vertexBuffer[0].Position.Y = v2.Y;

            vertexBuffer[1].Position.X = v2.X;
            vertexBuffer[1].Position.Y = v2.Y;

            vertexBuffer[2].Position.X = v1.X;
            vertexBuffer[2].Position.Y = v1.Y;

            vertexBuffer[3].Position.X = v2.X;
            vertexBuffer[3].Position.Y = v1.Y;

            graphicsDevice.DrawUserIndexedPrimitives
                (PrimitiveType.TriangleList, vertexBuffer, 0, 4, indexBuffer, 0, 2);

            //graphicsDevice.SetVertexBuffer(_vBuffer);
            //graphicsDevice.Indices = (_iBuffer);

            //graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
            //    0, 2);
        }
    }
}

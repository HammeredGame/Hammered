using HammeredGame.Game;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HammeredGame.Graphics
{
    internal class GameRenderer
    {

        private RenderTarget2D diffuseTarget;
        private RenderTarget2D depthTarget;

        private RenderTarget2D lightDepthTarget;

        private RenderTarget2D finalTarget;

        private GraphicsDevice gpu;
        private SpriteBatch spriteBatch;

        private Effect colorCorrectionEffect;
        private float exposure = 1.0f;
        private float shadowMapDepthBias = 1.0f / 2048.0f * 2f;
        private float shadowMapNormalOffset = 2f;

        private bool showDebugTargets;

        public GameRenderer(GraphicsDevice gpu, ContentManager content) {
            this.gpu = gpu;
            this.spriteBatch = new SpriteBatch(gpu);

            this.colorCorrectionEffect = content.Load<Effect>("Effects/PostProcess/ColorCorrection");

            diffuseTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.HdrBlendable, DepthFormat.Depth24);
            depthTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Single, DepthFormat.Depth24);

            // todo: the size of this target doesn't have to be equal to the back buffer size; experiment
            // todo: find out why this works with halfvector4 but not Single despite storing only one fp number
            lightDepthTarget = new RenderTarget2D(gpu, 2048, 2048, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24);

            finalTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        /// <summary>
        /// Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        /// <para/>
        /// To set state variables within graphics device back to default (in case they are changed
        /// at any point) to ensure we are correctly drawing in 3D space
        /// </summary>
        private void Set3DStates()
        {
            gpu.BlendState = BlendState.AlphaBlend; // Potentially needs to be modified depending on our textures
            gpu.DepthStencilState = DepthStencilState.Default; // Ensure we are using depth buffer (Z-buffer) for 3D
            if (gpu.RasterizerState.CullMode == CullMode.None)
            {
                // Cull back facing polygons
                RasterizerState rs = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
                gpu.RasterizerState = rs;
            }
        }

        public void DrawScene(Scene scene)
        {
            if (scene == null)
            {
                return;
            }

            // Perform a pass to generate depth values from the scene's sunlight
            gpu.SetRenderTargets(lightDepthTarget);
            gpu.Clear(Color.White);
            Set3DStates();

            Vector3 sunPos = scene.Lights.Sun.Direction * 500f;
            Matrix sunView = Matrix.CreateLookAt(sunPos, Vector3.Zero, Vector3.Up);
            Matrix sunProj = Matrix.CreateOrthographic(1000, 1000, 0.01f, 2000f);
            foreach (GameObject gameObject in scene.GameObjectsList)
            {
                gameObject.Effect.CurrentTechnique = gameObject.Effect.Techniques["RenderLightDepthMap"];
                gameObject.Draw(sunView, sunProj, sunPos, scene.Lights);
                gameObject.Effect.CurrentTechnique = gameObject.Effect.Techniques["MainShading"];
            }

            // Perform a main forward render pass but also store depth information
            gpu.SetRenderTargets(diffuseTarget, depthTarget);
            Set3DStates();

            // Render all the scene objects
            foreach (GameObject gameObject in scene.GameObjectsList)
            {
                gameObject.Effect.Parameters["SunDepthTexture"]?.SetValue(lightDepthTarget);
                gameObject.Effect.Parameters["SunView"]?.SetValue(sunView);
                gameObject.Effect.Parameters["SunProj"]?.SetValue(sunProj);
                gameObject.Effect.Parameters["ShadowMapDepthBias"]?.SetValue(shadowMapDepthBias);
                gameObject.Effect.Parameters["ShadowMapNormalOffset"]?.SetValue(shadowMapNormalOffset);
                gameObject.Draw(scene.Camera.ViewMatrix, scene.Camera.ProjMatrix, scene.Camera.Position, scene.Lights);
            }
        }

        public void PostProcess()
        {
            gpu.SetRenderTarget(finalTarget);

            colorCorrectionEffect.Parameters["Exposure"].SetValue(exposure);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, colorCorrectionEffect, null);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();
        }

        private void DisplayIntermediateTargets()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, 320, 180), Color.SkyBlue);
            spriteBatch.Draw(depthTarget, new Rectangle(320, 0, 320, 180), Color.SkyBlue);
            spriteBatch.Draw(lightDepthTarget, new Rectangle(640, 0, 180, 180), Color.SkyBlue);
            spriteBatch.End();
        }

        public void CopyOutputTo(RenderTarget2D target)
        {
            gpu.SetRenderTarget(target);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(finalTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();

            if (showDebugTargets)
            {
                DisplayIntermediateTargets();
            }
        }

        public void UI()
        {
            ImGui.Checkbox("Show intermediate targets", ref showDebugTargets);
            ImGui.Text("Exposure:");
            ImGui.SameLine();
            ImGui.DragFloat("##exposure", ref exposure, 0.1f, 0f, 5f);
            ImGui.Text("Shadow Map Depth Bias:");
            ImGui.SameLine();
            ImGui.DragFloat("##bias", ref shadowMapDepthBias, 0.001f, 0f, 1f);
            ImGui.Text("Shadow Map Normal Offset:");
            ImGui.SameLine();
            ImGui.DragFloat("##offset", ref shadowMapNormalOffset, 0.1f, 0f, 10f);
        }
    }
}

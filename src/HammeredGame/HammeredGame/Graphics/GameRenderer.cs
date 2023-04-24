using HammeredGame.Game;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HammeredGame.Graphics
{
    internal class GameRenderer
    {

        private RenderTarget2D diffuseTarget;
        private RenderTarget2D normalsTarget;
        private RenderTarget2D depthTarget;

        private RenderTarget2D lightsTarget;

        private RenderTarget2D finalTarget;

        private GraphicsDevice gpu;
        private SpriteBatch spriteBatch;

        private Effect lightingPassEffect;
        private Effect combinePassEffect;

        private bool showDebugTargets;

        public GameRenderer(GraphicsDevice gpu, ContentManager content) {
            this.gpu = gpu;
            this.spriteBatch = new SpriteBatch(gpu);

            diffuseTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            normalsTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            depthTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Single, DepthFormat.Depth24);

            lightsTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);

            finalTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);

            lightingPassEffect = content.Load<Effect>("Effects/deferredlighting");
            combinePassEffect = content.Load<Effect>("Effects/finalcombine");
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

        public void SetupDrawTargets() {
            // set up geometry (g) buffer
            gpu.SetRenderTargets(diffuseTarget, normalsTarget, depthTarget);
            Set3DStates();
        }

        public void ApplyDeferredLighting(List<GameObject> objects) {
            // lighting pass for directional light & point lights & emissive materials
            gpu.SetRenderTarget(lightsTarget);
            Vector2 halfPixel = new(
                0.5f / (float)gpu.PresentationParameters.BackBufferWidth,
                0.5f / (float)gpu.PresentationParameters.BackBufferHeight
            );

            lightingPassEffect.Parameters["lightDirection"]?.SetValue(new Vector3(0, -1, 0));
            lightingPassEffect.Parameters["lightColor"]?.SetValue(Color.White.ToVector3());
            lightingPassEffect.Parameters["lightIntensity"]?.SetValue(0.5f);
            lightingPassEffect.Parameters["diffuseMap"]?.SetValue(diffuseTarget);
            lightingPassEffect.Parameters["normalMap"]?.SetValue(normalsTarget);
            lightingPassEffect.Parameters["depthMap"]?.SetValue(depthTarget);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, lightingPassEffect, null);
            spriteBatch.Draw(normalsTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();

            lightingPassEffect.Parameters["lightDirection"]?.SetValue(new Vector3(0,-1, 1));
            lightingPassEffect.Parameters["lightColor"]?.SetValue(Color.White.ToVector3());
            lightingPassEffect.Parameters["lightIntensity"]?.SetValue(0.5f);
            lightingPassEffect.Parameters["diffuseMap"]?.SetValue(diffuseTarget);
            lightingPassEffect.Parameters["normalMap"]?.SetValue(normalsTarget);
            lightingPassEffect.Parameters["depthMap"]?.SetValue(depthTarget);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, lightingPassEffect, null);
            spriteBatch.Draw(normalsTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();

            gpu.SetRenderTarget(finalTarget);
            combinePassEffect.Parameters["diffuseMap"]?.SetValue(diffuseTarget);
            combinePassEffect.Parameters["lightMap"]?.SetValue(lightsTarget);
            combinePassEffect.Parameters["AmbientColor"]?.SetValue(Color.White.ToVector4());
            combinePassEffect.Parameters["AmbientIntensity"]?.SetValue(0.1f);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, combinePassEffect, null);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.SkyBlue);
            spriteBatch.End();
        }

        public void PostProcess() {
            if (showDebugTargets)
            {
                RenderDebugTargets();
            }
        }

        private void RenderDebugTargets()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, 160, 90), Color.SkyBlue);
            spriteBatch.Draw(normalsTarget, new Rectangle(160, 0, 160, 90), Color.SkyBlue);
            spriteBatch.Draw(depthTarget, new Rectangle(320, 0, 160, 90), Color.SkyBlue);
            spriteBatch.Draw(lightsTarget, new Rectangle(480, 0, 160, 90), Color.SkyBlue);
            spriteBatch.End();
        }

        public void CopyOutputTo(RenderTarget2D target)
        {
            gpu.SetRenderTarget(target);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(finalTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();
        }

        public void UI()
        {
            ImGui.Checkbox("Show intermediate targets", ref showDebugTargets);
        }
    }
}

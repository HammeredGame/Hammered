using BEPUphysics.OtherSpaceStages;
using HammeredGame.Game;
using HammeredGame.Game.GameObjects.EnvironmentObjects;
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
        // Targets that the light's shadow depth generation pass will write to
        private readonly RenderTarget2D lightDepthTarget;

        // Targets that the main shading pass will write to and any of its
        // customizable parameters
        private readonly RenderTarget2D diffuseTarget;
        private readonly RenderTarget2D depthTarget;
        private float shadowMapDepthBias = 1.0f / 2048.0f * 2f;
        private float shadowMapNormalOffset = 2f;

        // Final target that contains post-processed color data
        private readonly RenderTarget2D finalTarget;

        // References to the global stuff
        private readonly GraphicsDevice gpu;
        private readonly SpriteBatch spriteBatch;

        // The color correction post-processing effect and its parameters
        private readonly Effect colorCorrectionEffect;
        private readonly RenderTarget2D postprocessTarget;
        private float exposure = 1.0f;

        private BloomFilter _bloomFilter;

        private bool showDebugTargets;


        public GameRenderer(GraphicsDevice gpu, ContentManager content) {
            this.gpu = gpu;
            this.spriteBatch = new SpriteBatch(gpu);

            this.colorCorrectionEffect = content.Load<Effect>("Effects/PostProcess/ColorCorrection");

            // Set up the target where the sunlight's depth generation pass will write to. This
            // doesn't need to equal the back buffer size, and in fact, we use a square to get
            // uniform UV-mapping.
            lightDepthTarget = new RenderTarget2D(gpu, 2048, 2048, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24);

            // Set up the main shading pass targets. The diffuse target will be HDR with each color
            // component being [0, Inf], so set it up right.
            diffuseTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.HdrBlendable, DepthFormat.Depth24);
            depthTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Single, DepthFormat.Depth24);

            // The target for the final tone-mapped and post-processed image. Format is Color, i.e.
            // 8 bit RGBA.
            postprocessTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            finalTarget = new RenderTarget2D(gpu, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);

            _bloomFilter = new BloomFilter();
            _bloomFilter.Load(gpu, content, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight);
            _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;
        }

        /// <summary>
        /// Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        /// <para/>
        /// To set state variables within graphics device back to default (in case they are changed
        /// at any point) to ensure we are correctly drawing in 3D space
        /// </summary>
        private void Set3DStates()
        {
            gpu.BlendState = BlendState.AlphaBlend;
            gpu.DepthStencilState = DepthStencilState.Default; // Ensure we are using depth buffer (Z-buffer) for 3D
            if (gpu.RasterizerState.CullMode == CullMode.None)
            {
                // Cull back facing polygons
                gpu.RasterizerState = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
            }
        }

        /// <summary>
        /// Draw a scene, with shadows and lighting. this.PostProcess() should be called after this
        /// is called, to copy the post-processed and tone-mapped images into the final render target.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="scene"></param>
        public void DrawScene(GameTime gameTime, Scene scene)
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
                gameObject.Draw(gameTime, sunView, sunProj, sunPos, scene.Lights);
                gameObject.Effect.CurrentTechnique = gameObject.Effect.Techniques["MainShading"];
            }

            // Perform a main forward render pass but also store depth information
            gpu.SetRenderTargets(diffuseTarget, depthTarget);
            Set3DStates();

            // Render all the scene objects except for Skybox, which is to be rendered last with a
            // different cull mode
            SkyboxObject deferredSkybox = null;
            foreach (GameObject gameObject in scene.GameObjectsList)
            {
                // The skybox needs to be rendered last (otherwise it'll draw so many useless pixels
                // across the screen, that would be overwritten by other closer objects). It also
                // needs a specific cull-mode since we will be rendering the back face and not the
                // front face, so we'll defer it to later.
                if (gameObject is SkyboxObject sky)
                {
                    deferredSkybox = sky;
                    continue;
                }

                // TODO: move these parameter-setting into GameObject's Draw() so everything is in one place
                gameObject.Effect.Parameters["SunDepthTexture"]?.SetValue(lightDepthTarget);
                gameObject.Effect.Parameters["SunView"]?.SetValue(sunView);
                gameObject.Effect.Parameters["SunProj"]?.SetValue(sunProj);
                gameObject.Effect.Parameters["ShadowMapDepthBias"]?.SetValue(shadowMapDepthBias);
                gameObject.Effect.Parameters["ShadowMapNormalOffset"]?.SetValue(shadowMapNormalOffset);
                gameObject.Draw(gameTime, scene.Camera.ViewMatrix, scene.Camera.ProjMatrix, scene.Camera.Position, scene.Lights);
            }

            // Render the deferred skybox if there is one
            if (deferredSkybox != null)
            {
                // The skybox is essentially a gigantic box that we're viewing from inside. Because
                // of this, we need to make sure that we're culling not the back faces, but the
                // front faces.
                gpu.RasterizerState = new RasterizerState { CullMode = CullMode.CullClockwiseFace };
                deferredSkybox.Draw(gameTime, scene.Camera.ViewMatrix, scene.Camera.ProjMatrix, scene.Camera.Position, scene.Lights);
            }

            if (scene.DrawDebugObjects)
            {
                // Temporarily render in WireFrame fill mode to visualise debug entities
                RasterizerState currentState = gpu.RasterizerState;
                gpu.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame };
                foreach (EntityDebugDrawer item in scene.DebugObjects) {
                    item.Draw(gameTime, scene.Camera.ViewMatrix, scene.Camera.ProjMatrix);
                }
                gpu.RasterizerState = currentState;
            }

            if (scene.DrawDebugGrid)
            {
                // Temporarily render in WireFrame fill mode to visualise debug entities
                RasterizerState currentRS = gpu.RasterizerState;
                gpu.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame };
                foreach (GridDebugDrawer gdd in scene.DebugGridCells)
                {
                    gdd.Draw(gameTime, scene.Camera.ViewMatrix, scene.Camera.ProjMatrix);
                }
                gpu.RasterizerState = currentRS;
            }
        }

        /// <summary>
        /// Post-process the scene rendered with this.DrawScene(). You should call
        /// this.CopyOutputTo() after this function to copy the final render output.
        /// </summary>
        public void PostProcess()
        {
            gpu.SetRenderTarget(postprocessTarget);

            colorCorrectionEffect.Parameters["Exposure"].SetValue(exposure);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, colorCorrectionEffect, null);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();

            Texture2D bloom = _bloomFilter.Draw(postprocessTarget, gpu.PresentationParameters.BackBufferWidth / 2, gpu.PresentationParameters.BackBufferHeight / 2);

            gpu.SetRenderTarget(finalTarget);
            gpu.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            spriteBatch.Draw(postprocessTarget, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.Draw(bloom, new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Draw any intermediate render targets in the top left corner for debugging purposes.
        /// </summary>
        private void DisplayIntermediateTargets()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(diffuseTarget, new Rectangle(0, 0, 320, 180), Color.SkyBlue);
            spriteBatch.Draw(depthTarget, new Rectangle(320, 0, 320, 180), Color.SkyBlue);
            spriteBatch.Draw(lightDepthTarget, new Rectangle(640, 0, 180, 180), Color.SkyBlue);
            spriteBatch.Draw(postprocessTarget, new Rectangle(820, 0, 320, 180), Color.SkyBlue);
            spriteBatch.End();
        }

        /// <summary>
        /// Copy the output of the scene rendering to the specified target. This function must be
        /// called after this.DrawScene() and this.PostProcess() in order for it to contain
        /// meaningful information.
        /// </summary>
        /// <param name="target"></param>
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
            if (ImGui.BeginCombo("Bloom Preset", "Wide")) {
                if (ImGui.Selectable("Wide"))
                {
                    _bloomFilter.BloomPreset = BloomFilter.BloomPresets.Wide;
                } else if (ImGui.Selectable("SuperWide"))
                {
                    _bloomFilter.BloomPreset = BloomFilter.BloomPresets.SuperWide;

                }
                else if (ImGui.Selectable("Focussed"))
                {
                    _bloomFilter.BloomPreset = BloomFilter.BloomPresets.Focussed;

                }
                else if (ImGui.Selectable("Small"))
                {
                    _bloomFilter.BloomPreset = BloomFilter.BloomPresets.Small;

                }
                else if (ImGui.Selectable("Cheap"))
                {
                    _bloomFilter.BloomPreset = BloomFilter.BloomPresets.Cheap;

                }

                ImGui.EndCombo();
            }
            float length = _bloomFilter.BloomStreakLength;
            ImGui.DragFloat("Bloom Filter Streak Length", ref length, 0.01f, 0f, 10f);
            _bloomFilter.BloomStreakLength = length;
            float threshold = _bloomFilter.BloomThreshold;
            ImGui.DragFloat("Bloom Filter Threshold", ref threshold, 0.01f, 0f, 1f);
            _bloomFilter.BloomThreshold = threshold;
        }
    }
}

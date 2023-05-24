using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Graphics.ForwardRendering
{
    /// <summary>
    /// This class is a base class for all forward rendering effects. It defines parameters for
    /// world, view and projection matrices, and parameters necessary for passing shadow maps. It
    /// assumes that all forward rendering effects define a RenderLightDepthMap technique, a
    /// MainShading technique and a MainShadingInstanced technique.
    /// </summary>
    public abstract class AbstractForwardRenderingEffect
    {
        private EffectParameter worldParam;

        /// <summary>
        /// The world matrix.
        /// </summary>
        public Matrix World
        {
            get { return worldParam.GetValueMatrix(); }
            set { worldParam.SetValue(value); }
        }

        private EffectParameter viewParam;

        /// <summary>
        /// The view matrix.
        /// </summary>
        public Matrix View
        {
            get { return viewParam.GetValueMatrix(); }
            set { viewParam.SetValue(value); }
        }

        private EffectParameter projectionParam;

        /// <summary>
        /// The projection matrix.
        /// </summary>
        public Matrix Projection
        {
            get { return projectionParam.GetValueMatrix(); }
            set { projectionParam.SetValue(value); }
        }

        private EffectParameter worldInverseTransposeParam;

        public Matrix WorldInverseTranspose
        {
            get { return worldInverseTransposeParam.GetValueMatrix(); }
            set { worldInverseTransposeParam.SetValue(value); }
        }


        private EffectParameter sunDepthTextureParam;

        /// <summary>
        /// The depth/shadow map generated from the sun's point of view.
        /// </summary>
        public Texture2D SunDepthTexture
        {
            get { return sunDepthTextureParam.GetValueTexture2D(); }
            set { sunDepthTextureParam.SetValue(value); }
        }

        private EffectParameter shadowMapDepthBiasParam;

        /// <summary>
        /// The depth bias used when sampling the shadow map, to allow floating point errors. Values
        /// that are too high may cause peter panning.
        /// </summary>
        public float ShadowMapDepthBias
        {
            get { return shadowMapDepthBiasParam.GetValueSingle(); }
            set { shadowMapDepthBiasParam.SetValue(value); }
        }

        private EffectParameter shadowMapNormalOffsetParam;

        /// <summary>
        /// The offset to move the normal when sampling the shadow map, to account for the case when
        /// a face's normal is almost perpendicular to the sun's direction, causing banding artifacts.
        /// </summary>
        public float ShadowMapNormalOffset
        {
            get { return shadowMapNormalOffsetParam.GetValueSingle(); }
            set { shadowMapNormalOffsetParam.SetValue(value); }
        }

        private EffectParameter sunViewParam;

        /// <summary>
        /// The view matrix from the sun.
        /// </summary>
        public Matrix SunView
        {
            get { return sunViewParam.GetValueMatrix(); }
            set { sunViewParam.SetValue(value); }
        }

        /// <summary>
        /// The projection matrix from the sun.
        /// </summary>
        private EffectParameter sunProjectionParam;

        public Matrix SunProjection
        {
            get { return sunProjectionParam.GetValueMatrix(); }
            set { sunProjectionParam.SetValue(value); }
        }

        protected readonly Effect ShadingEffect;

        protected readonly EffectTechnique ShadowMapGenerationTechnique;
        protected readonly EffectTechnique MainShadingTechnique;
        protected readonly EffectTechnique MainShadingInstancedTechnique;

        protected abstract string EffectName { get; }

        protected AbstractForwardRenderingEffect(ContentManager content)
        {
            // Make sure we clone the effect so we don't reuse the same instance, causing properties
            // set on a previous object to be retained. We want to have separate instances for each
            // object so that we can set some material properties once and keep them throughout all
            // of its Draw calls.
            ShadingEffect = content.Load<Effect>(EffectName).Clone();

            CacheEffectParameters();
            ShadowMapGenerationTechnique = ShadingEffect.Techniques["RenderLightDepthMap"];
            MainShadingTechnique = ShadingEffect.Techniques["MainShading"];
            MainShadingInstancedTechnique = ShadingEffect.Techniques["MainShadingInstanced"];
        }

        protected AbstractForwardRenderingEffect(AbstractForwardRenderingEffect clone)
        {
            ShadingEffect = clone.ShadingEffect.Clone();

            CacheEffectParameters();
            ShadowMapGenerationTechnique = ShadingEffect.Techniques["RenderLightDepthMap"];
            MainShadingTechnique = ShadingEffect.Techniques["MainShading"];
            MainShadingInstancedTechnique = ShadingEffect.Techniques["MainShadingInstanced"];
        }

        public abstract AbstractForwardRenderingEffect Clone();

        /// <summary>
        /// Looking up effect parameters by name is slightly slow when done often, so we look them
        /// up once and cache the parameters.
        /// </summary>
        protected virtual void CacheEffectParameters()
        {
            worldParam = ShadingEffect.Parameters["World"];
            viewParam = ShadingEffect.Parameters["View"];
            projectionParam = ShadingEffect.Parameters["Projection"];
            worldInverseTransposeParam = ShadingEffect.Parameters["WorldInverseTranspose"];

            sunDepthTextureParam = ShadingEffect.Parameters["SunDepthTexture"];
            shadowMapDepthBiasParam = ShadingEffect.Parameters["ShadowMapDepthBias"];
            shadowMapNormalOffsetParam = ShadingEffect.Parameters["ShadowMapNormalOffset"];
            sunViewParam = ShadingEffect.Parameters["SunView"];
            sunProjectionParam = ShadingEffect.Parameters["SunProj"];
        }

        /// <summary>
        /// The possible passes (synonymous with shader techniques here, since all forward rendering
        /// techniques only have a single pass each).
        /// </summary>
        public enum Pass
        {
            ShadowMapGeneration,
            MainShading,
            MainShadingInstanced,
        }

        public Pass CurrentPass
        {
            set
            {
                switch (value)
                {
                    case Pass.ShadowMapGeneration:
                        ShadingEffect.CurrentTechnique = ShadowMapGenerationTechnique;
                        break;
                    case Pass.MainShading:
                        ShadingEffect.CurrentTechnique = MainShadingTechnique;
                        break;
                    case Pass.MainShadingInstanced:
                        ShadingEffect.CurrentTechnique = MainShadingInstancedTechnique;
                        break;
                }
            }
        }

        /// <summary>
        /// Apply the current active pass.
        /// </summary>
        public void Apply()
        {
            ShadingEffect.CurrentTechnique.Passes[0].Apply();
        }

        /// <summary>
        /// Get the <see cref="Effect"/> that you can set on a <see cref="ModelMeshPart"/> when
        /// using <see cref="ModelMesh.Draw()"/> as a shortcut instead of manually drawing primitives.
        /// </summary>
        public Effect GetEffect()
        {
            return ShadingEffect;
        }
    }
}

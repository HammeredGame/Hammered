using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Graphics.ForwardRendering
{
    public class SkyboxEffect : AbstractForwardRenderingEffect
    {
        private EffectParameter sunLightColorParam;

        /// <summary>
        /// The color of the sun light to add to the skybox.
        /// </summary>
        public Vector4 SunLightColor
        {
            get { return sunLightColorParam.GetValueVector4(); }
            set { sunLightColorParam.SetValue(value); }
        }

        private EffectParameter sunLightIntensityParam;

        /// <summary>
        /// The intensity of the sun light to add to the skybox.
        /// </summary>
        public float SunLightIntensity
        {
            get { return sunLightIntensityParam.GetValueSingle(); }
            set { sunLightIntensityParam.SetValue(value); }
        }

        private EffectParameter sunLightDirectionParam;

        /// <summary>
        /// The direction towards the sun that gets added to the skybox.
        /// </summary>
        public Vector3 SunLightDirection
        {
            get { return sunLightDirectionParam.GetValueVector3(); }
            set { sunLightDirectionParam.SetValue(value); }
        }

        private EffectParameter skyboxTextureParam;

        /// <summary>
        /// The cube texture for the skybox.
        /// </summary>
        public TextureCube SkyboxTexture
        {
            get { return skyboxTextureParam.GetValueTextureCube(); }
            set { skyboxTextureParam.SetValue(value); }
        }

        private EffectParameter skyboxTextureGammaCorrectionParam;

        /// <summary>
        /// Whether the skybox texture needs to be gamma corrected to linear from sRGB.
        /// </summary>
        public bool SkyboxTextureGammaCorrection
        {
            get { return skyboxTextureGammaCorrectionParam.GetValueBoolean(); }
            set { skyboxTextureGammaCorrectionParam.SetValue(value); }
        }

        protected override string EffectName => "Effects/ForwardRendering/Skybox";

        public SkyboxEffect(ContentManager content) : base(content) { }

        public SkyboxEffect(SkyboxEffect clone) : base(clone) { }

        public override SkyboxEffect Clone()
        {
            return new SkyboxEffect(this);
        }

        protected override void CacheEffectParameters()
        {
            base.CacheEffectParameters();

            sunLightColorParam = ShadingEffect.Parameters["SunLightColor"];
            sunLightIntensityParam = ShadingEffect.Parameters["SunLightIntensity"];
            sunLightDirectionParam = ShadingEffect.Parameters["SunLightDirection"];
            skyboxTextureParam = ShadingEffect.Parameters["SkyboxTexture"];
            skyboxTextureGammaCorrectionParam = ShadingEffect.Parameters["SkyboxTextureGammaCorrection"];
        }
    }
}

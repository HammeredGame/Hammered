using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Graphics.ForwardRendering
{
    public class WaterEffect : AbstractForwardRenderingEffect
    {
        private EffectParameter litParam;

        /// <summary>
        /// Whether this object is affected by lighting or not.
        /// </summary>
        public bool Lit
        {
            get { return litParam.GetValueBoolean(); }
            set { litParam.SetValue(value); }
        }

        private EffectParameter directionalLightColorsParam;

        /// <summary>
        /// Colors of the directional lights.
        /// </summary>
        public Vector4[] DirectionalLightColors
        {
            get { return directionalLightColorsParam.GetValueVector4Array(); }
            set { directionalLightColorsParam.SetValue(value); }
        }

        private EffectParameter directionalLightIntensitiesParam;

        /// <summary>
        /// Intensities of the directional lights.
        /// </summary>
        public float[] DirectionalLightIntensities
        {
            get { return directionalLightIntensitiesParam.GetValueSingleArray(); }
            set { directionalLightIntensitiesParam.SetValue(value); }
        }

        private EffectParameter directionalLightDirectionsParam;

        /// <summary>
        /// Directions towards the directional lights.
        /// </summary>
        public Vector3[] DirectionalLightDirections
        {
            get { return directionalLightDirectionsParam.GetValueVector3Array(); }
            set { directionalLightDirectionsParam.SetValue(value); }
        }

        private EffectParameter sunLightIndexParam;

        /// <summary>
        /// Which of the directional lights is the sun and should be used for shadow mapping.
        /// </summary>
        public int SunLightIndex
        {
            get { return sunLightIndexParam.GetValueInt32(); }
            set { sunLightIndexParam.SetValue(value); }
        }

        private EffectParameter ambientLightColorParam;

        /// <summary>
        /// Color of the ambient light.
        /// </summary>
        public Vector4 AmbientLightColor
        {
            get { return ambientLightColorParam.GetValueVector4(); }
            set { ambientLightColorParam.SetValue(value); }
        }

        private EffectParameter ambientLightIntensityParam;

        /// <summary>
        /// Intensity of the ambient light.
        /// </summary>
        public float AmbientLightIntensity
        {
            get { return ambientLightIntensityParam.GetValueSingle(); }
            set { ambientLightIntensityParam.SetValue(value); }
        }

        private EffectParameter materialDiffuseColorParam;

        /// <summary>
        /// Diffuse color of the material.
        /// </summary>
        public Vector4 MaterialDiffuseColor
        {
            get { return materialDiffuseColorParam.GetValueVector4(); }
            set { materialDiffuseColorParam.SetValue(value); }
        }

        private EffectParameter materialAmbientColorParam;

        /// <summary>
        /// Ambient color of the material.
        /// </summary>
        public Vector4 MaterialAmbientColor
        {
            get { return materialAmbientColorParam.GetValueVector4(); }
            set { materialAmbientColorParam.SetValue(value); }
        }

        private EffectParameter materialHasSpecularParam;

        /// <summary>
        /// Whether the material has a specular component.
        /// </summary>
        public bool MaterialHasSpecular
        {
            get { return materialHasSpecularParam.GetValueBoolean(); }
            set { materialHasSpecularParam.SetValue(value); }
        }

        private EffectParameter materialSpecularColorParam;

        /// <summary>
        /// Specular color of the material.
        /// </summary>
        public Vector4 MaterialSpecularColor
        {
            get { return materialSpecularColorParam.GetValueVector4(); }
            set { materialSpecularColorParam.SetValue(value); }
        }

        private EffectParameter materialShininessParam;

        /// <summary>
        /// How shiny the material is.
        /// </summary>
        public float MaterialShininess
        {
            get { return materialShininessParam.GetValueSingle(); }
            set { materialShininessParam.SetValue(value); }
        }

        private EffectParameter cameraPositionParam;

        /// <summary>
        /// World-space position of the camera for shadow mapping based on the view frustum.
        /// </summary>
        public Vector3 CameraPosition
        {
            get { return cameraPositionParam.GetValueVector3(); }
            set { cameraPositionParam.SetValue(value); }
        }

        private EffectParameter modelTextureParam;

        /// <summary>
        /// The texture of the water surface.
        /// </summary>
        public Texture2D ModelTexture
        {
            get { return modelTextureParam.GetValueTexture2D(); }
            set { modelTextureParam.SetValue(value); }
        }

        private EffectParameter modelTextureGammaCorrectionParam;

        /// <summary>
        /// Whether the texture needs to be gamma corrected to linear from sRGB.
        /// </summary>
        public bool ModelTextureGammaCorrection
        {
            get { return modelTextureGammaCorrectionParam.GetValueBoolean(); }
            set { modelTextureGammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter gameTimeSecondsParam;

        /// <summary>
        /// The current total game time in seconds for animating the water.
        /// </summary>
        public float GameTimeSeconds
        {
            get { return gameTimeSecondsParam.GetValueSingle(); }
            set { gameTimeSecondsParam.SetValue(value); }
        }

        private EffectParameter waterNormal0Param;

        /// <summary>
        /// One of the normal maps for the water surface.
        /// </summary>
        public Texture2D WaterNormal0
        {
            get { return waterNormal0Param.GetValueTexture2D(); }
            set { waterNormal0Param.SetValue(value); }
        }

        private EffectParameter waterNormal0GammaCorrectionParam;

        /// <summary>
        /// Whether the texture needs to be gamma corrected to linear from sRGB.
        /// </summary>
        public bool WaterNormal0GammaCorrection
        {
            get { return waterNormal0GammaCorrectionParam.GetValueBoolean(); }
            set { waterNormal0GammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter waterNormal1Param;

        /// <summary>
        /// One of the normal maps for the water surface.
        /// </summary>
        public Texture2D WaterNormal1
        {
            get { return waterNormal1Param.GetValueTexture2D(); }
            set { waterNormal1Param.SetValue(value); }
        }

        private EffectParameter waterNormal1GammaCorrectionParam;

        /// <summary>
        /// Whether the texture needs to be gamma corrected to linear from sRGB.
        /// </summary>
        public bool WaterNormal1GammaCorrection
        {
            get { return waterNormal1GammaCorrectionParam.GetValueBoolean(); }
            set { waterNormal1GammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter useBumpMapParam;

        /// <summary>
        /// Whether to use bump making for the water surface. If true, <see cref="WaterNormal0"/>
        /// and <see cref="WaterNormal1"/> need to be set, otherwise those are ignored.
        /// </summary>
        public bool UseBumpMap
        {
            get { return useBumpMapParam.GetValueBoolean(); }
            set { useBumpMapParam.SetValue(value); }
        }

        private EffectParameter waterOpacityParam;

        /// <summary>
        /// The opacity at which to render the water at.
        /// </summary>
        public float WaterOpacity
        {
            get { return waterOpacityParam.GetValueSingle(); }
            set { waterOpacityParam.SetValue(value); }
        }

        protected override string EffectName => "Effects/ForwardRendering/Water";

        public WaterEffect(ContentManager content) : base(content) { }

        public WaterEffect(WaterEffect clone) : base(clone) { }

        public override WaterEffect Clone()
        {
            return new WaterEffect(this);
        }

        protected override void CacheEffectParameters()
        {
            base.CacheEffectParameters();

            litParam = ShadingEffect.Parameters["Lit"];
            directionalLightColorsParam = ShadingEffect.Parameters["DirectionalLightColors"];
            directionalLightIntensitiesParam = ShadingEffect.Parameters["DirectionalLightIntensities"];
            directionalLightDirectionsParam = ShadingEffect.Parameters["DirectionalLightDirections"];
            sunLightIndexParam = ShadingEffect.Parameters["SunLightIndex"];
            ambientLightColorParam = ShadingEffect.Parameters["AmbientLightColor"];
            ambientLightIntensityParam = ShadingEffect.Parameters["AmbientLightIntensity"];
            materialDiffuseColorParam = ShadingEffect.Parameters["MaterialDiffuseColor"];
            materialAmbientColorParam = ShadingEffect.Parameters["MaterialAmbientColor"];
            materialHasSpecularParam = ShadingEffect.Parameters["MaterialHasSpecular"];
            materialSpecularColorParam = ShadingEffect.Parameters["MaterialSpecularColor"];
            materialShininessParam = ShadingEffect.Parameters["MaterialShininess"];
            cameraPositionParam = ShadingEffect.Parameters["CameraPosition"];
            modelTextureParam = ShadingEffect.Parameters["ModelTexture"];
            modelTextureGammaCorrectionParam = ShadingEffect.Parameters["ModelTextureGammaCorrection"];
            gameTimeSecondsParam = ShadingEffect.Parameters["GameTimeSeconds"];
            waterNormal0Param = ShadingEffect.Parameters["WaterNormal0"];
            waterNormal0GammaCorrectionParam = ShadingEffect.Parameters["WaterNormal0GammaCorrection"];
            waterNormal1Param = ShadingEffect.Parameters["WaterNormal1"];
            waterNormal1GammaCorrectionParam = ShadingEffect.Parameters["WaterNormal1GammaCorrection"];
            useBumpMapParam = ShadingEffect.Parameters["UseBumpMap"];
            waterOpacityParam = ShadingEffect.Parameters["WaterOpacity"];
        }
    }
}

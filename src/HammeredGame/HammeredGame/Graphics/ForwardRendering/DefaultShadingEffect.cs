using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Graphics.ForwardRendering
{
    public class DefaultShadingEffect : AbstractForwardRenderingEffect
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
        /// The texture of the model.
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

        protected override string EffectName => "Effects/ForwardRendering/MainShading";

        public DefaultShadingEffect(ContentManager content) : base(content)
        {
        }

        public DefaultShadingEffect(DefaultShadingEffect clone) : base(clone)
        {
        }

        public override DefaultShadingEffect Clone()
        {
            return new DefaultShadingEffect(this);
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
        }
    }
}

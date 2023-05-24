using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Graphics.ForwardRendering
{
    public class LaserEffect : AbstractForwardRenderingEffect
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

        private EffectParameter laserMaterialParam;

        /// <summary>
        /// The color material for the laser to control how it looks. A gradient texture works well.
        /// </summary>
        public Texture2D LaserMaterial
        {
            get { return laserMaterialParam.GetValueTexture2D(); }
            set { laserMaterialParam.SetValue(value); }
        }

        private EffectParameter laserMaterialGammaCorrectionParam;

        /// <summary>
        /// Whether the texture should be gamma corrected to linear from sRGB.
        /// </summary>
        public bool LaserMaterialGammaCorrection
        {
            get { return laserMaterialGammaCorrectionParam.GetValueBoolean(); }
            set { laserMaterialGammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter laserTextureParam;

        /// <summary>
        /// The repeating shape texture to use for the laser. This will animate over time based on
        /// the <see cref="LaserSpeed"/>.
        /// </summary>
        public Texture2D LaserTexture
        {
            get { return laserTextureParam.GetValueTexture2D(); }
            set { laserTextureParam.SetValue(value); }
        }

        private EffectParameter laserTextureGammaCorrectionParam;

        /// <summary>
        /// Whether the texture should be gamma corrected to linear from sRGB.
        /// </summary>
        public bool LaserTextureGammaCorrection
        {
            get { return laserTextureGammaCorrectionParam.GetValueBoolean(); }
            set { laserTextureGammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter laserMaskParam;

        /// <summary>
        /// The alpha mask to apply on the entire laser, which can be used to make the ends of the
        /// laser fade so the billboard effect is less obvious.
        /// </summary>
        public Texture2D LaserMask
        {
            get { return laserMaskParam.GetValueTexture2D(); }
            set { laserMaskParam.SetValue(value); }
        }

        private EffectParameter laserMaskGammaCorrectionParam;

        /// <summary>
        /// Whether the texture should be gamma corrected to linear from sRGB.
        /// </summary>
        public bool LaserMaskGammaCorrection
        {
            get { return laserMaskGammaCorrectionParam.GetValueBoolean(); }
            set { laserMaskGammaCorrectionParam.SetValue(value); }
        }

        private EffectParameter gameTimeSecondsParam;

        /// <summary>
        /// The current total game time in seconds for animating the laser.
        /// </summary>
        public float GameTimeSeconds
        {
            get { return gameTimeSecondsParam.GetValueSingle(); }
            set { gameTimeSecondsParam.SetValue(value); }
        }

        private EffectParameter laserIntensityParam;

        /// <summary>
        /// The intensity of the laser, which the Bloom post processing filter can use to make the
        /// laser glow.
        /// </summary>
        public float LaserIntensity
        {
            get { return laserIntensityParam.GetValueSingle(); }
            set { laserIntensityParam.SetValue(value); }
        }

        private EffectParameter laserSpeedParam;

        /// <summary>
        /// The speed at which to animate the laser.
        /// </summary>
        public Vector2 LaserSpeed
        {
            get { return laserSpeedParam.GetValueVector2(); }
            set { laserSpeedParam.SetValue(value); }
        }

        protected override string EffectName => "Effects/ForwardRendering/Laser";

        public LaserEffect(ContentManager content) : base(content) { }

        public LaserEffect(LaserEffect clone) : base(clone) { }

        public override LaserEffect Clone()
        {
            return new LaserEffect(this);
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
            laserMaterialParam = ShadingEffect.Parameters["LaserMaterial"];
            laserMaterialGammaCorrectionParam = ShadingEffect.Parameters["LaserMaterialGammaCorrection"];
            laserTextureParam = ShadingEffect.Parameters["LaserTexture"];
            laserTextureGammaCorrectionParam = ShadingEffect.Parameters["LaserTextureGammaCorrection"];
            laserMaskParam = ShadingEffect.Parameters["LaserMask"];
            laserMaskGammaCorrectionParam = ShadingEffect.Parameters["LaserMaskGammaCorrection"];
            gameTimeSecondsParam = ShadingEffect.Parameters["GameTimeSeconds"];
            laserIntensityParam = ShadingEffect.Parameters["LaserIntensity"];
            laserSpeedParam = ShadingEffect.Parameters["LaserSpeed"];
        }
    }
}

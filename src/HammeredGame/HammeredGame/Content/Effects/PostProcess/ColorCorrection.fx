
#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This shader defines post-process effects that can be done in a single pass.
// Specifically, the single technique in this shader performs:
//   - HDR Tonemapping
//   - Gamma Correction from physically-based linear space to sRGB space

// Reference: https://learnopengl.com/Advanced-Lighting/HDR

#define gamma 2.2

float Exposure;

texture HDRTexture;
sampler2D hdrBufferSampler = sampler_state
{
    Texture = (HDRTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Retrieve the HDR RGBA color values in [0, inf] range
    float3 hdrColor = tex2D(hdrBufferSampler, input.TexCoord).rgb;

    // ToneMap it (implementation from LearnOpenGL.com)
    float3 mapped = float3(1.0, 1.0, 1.0) - exp(-hdrColor * Exposure);

    // Also gamma correct to sRGB. Because of this, make sure that all loaded
    // material textures are inverse-gamma-corrected from sRGB to linear,
    // otherwise they'll be corrected twice and will appear very washed out.
    mapped = pow(mapped, float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));

    // Alpha of color should be set to 1
    return float4(mapped, 1.0);
};

technique TonemapGammaCorrection
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
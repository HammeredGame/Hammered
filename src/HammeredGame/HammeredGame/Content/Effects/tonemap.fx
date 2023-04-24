
#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define gamma 2.2

// Reference: https://learnopengl.com/Advanced-Lighting/HDR

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
    float3 hdrColor = tex2D(hdrBufferSampler, input.TexCoord).rgb;
    float3 mapped = float3(1.0, 1.0, 1.0) - exp(-hdrColor * Exposure);

    // Also gamma correct to sRGB. When using this, make sure that all loaded material textures are
    // inverse-gamma-corrected from sRGB to linear, otherwise they'll be corrected twice and will
    // appear very washed out.
    mapped = pow(mapped, float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));

    return float4(mapped, 1.0);
};

technique Basic
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 halfPixel;

texture diffuseMap;
texture lightMap;

float4 AmbientColor;
float AmbientIntensity;

sampler2D diffuseSampler = sampler_state
{
    Texture = (diffuseMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    MipFilter = LINEAR;
};

sampler2D lightSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    MipFilter = LINEAR;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 diffuseColor = tex2D(diffuseSampler, input.TexCoord).rgb;
    float4 light = tex2D(lightSampler, input.TexCoord);
    float3 diffuseLight = light.rgb;
    return float4(diffuseColor * diffuseLight, 1) + AmbientColor * AmbientIntensity;
}

technique Basic
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}

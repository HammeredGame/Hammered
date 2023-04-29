#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This shader defines techniques for the main rendering pass.
// Specifically, there is one technique for rendering the shadow map from a
// light's perspective, and a second technique for fully rendering the pixel
// using the shadow map, the textures, lights, and a Phong model.

// Reference: https://learnopengl.com/Lighting/Materials
// Reference: https://learnopengl.com/Lighting/Basic-Lighting
// Reference: https://learnopengl.com/Advanced-Lighting/Advanced-Lighting

#include "Common.fxh"

// ============================================================================
//
// SUN SHADOW MAP GENERATION
//
// ============================================================================

struct DepthMapVSInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
};

struct DepthMapVSOutput
{
    float4 Position : POSITION0;
};

DepthMapVSOutput DepthMapVS(DepthMapVSInput input)
{
    DepthMapVSOutput output;

    // Transform vertex coordinates into light projection-space [-1, 1]
    output.Position = ObjectToProjection(input.Position);

    return output;
}

float4 DepthMapPS() : COLOR0
{
    return float4(0, 0, 0, 0);
}

technique RenderLightDepthMap
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL DepthMapVS();
        PixelShader = compile PS_SHADERMODEL DepthMapPS();
    }
}

// ============================================================================
//
// MAIN SHADING
//
// ============================================================================

// The skybox texture
texture SkyboxTexture;
samplerCUBE skyboxTextureSampler = sampler_state
{
    Texture = (SkyboxTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Mirror;
    AddressV = Mirror;
};

bool SkyboxTextureGammaCorrection;

float4 SunLightColor;
float SunLightIntensity;
float3 SunLightDirection;

struct MainShadingVSInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct MainShadingVSOutput
{
    float4 Position : POSITION0;
    // We declare the following as TEXCOORD to get interpolation across pixels
    float3 TextureCoordinate : TEXCOORD0;
};

MainShadingVSOutput MainShadingVS(MainShadingVSInput input)
{
    MainShadingVSOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection).xyww;

    output.TextureCoordinate = input.Position;
    return output;
}

// The pixel shader of the main shading pass outputs both color and depth.
// TODO: find out if float4 for depth is fine given it's a single float
struct PixelShaderOutput
{
    float4 Color : COLOR0;
    float4 Depth : COLOR1;
};

PixelShaderOutput MainShadingPS(MainShadingVSOutput input)
{
    PixelShaderOutput output;

    // Sample from the cube map
    float4 textureColor = (SkyboxTextureGammaCorrection == true ? pow(texCUBE(skyboxTextureSampler, input.TextureCoordinate), 2.2) : texCUBE(skyboxTextureSampler, input.TextureCoordinate));
    output.Color = textureColor;

    // Add sunlight if the angle is near the sunlight angle
    float angleToSun = acos(dot(normalize(input.TextureCoordinate), normalize(SunLightDirection)));
    output.Color += SunLightColor * SunLightIntensity * (0.02 / angleToSun);

    // Depth is just really far away (max value)
    output.Depth = float4(1.0, 1.0, 1.0, 1.0);
    return output;
}

technique MainShading
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL MainShadingVS();
        PixelShader = compile PS_SHADERMODEL MainShadingPS();
    }
}
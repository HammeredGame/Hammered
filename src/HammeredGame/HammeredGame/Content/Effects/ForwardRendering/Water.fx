#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This shader defines techniques for rendering water. It is similar to the
// main rendering pass, but has two scrolling normal maps (in X and Y), an
// oscillating y-position in the vertex shader, and low transparency.

#include "Common.fxh"
#include "Macros.fxh"

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
    // Don't cast shadows
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

#include "MaterialsAndLighting.fxh"

// The world-space camera position is used to calculate the view vector from
// the pixel to the camera, which is used by the Phong specular component.
float4 CameraPosition;

// The material texture
DECLARE_TEXTURE(ModelTexture, textureSampler, Wrap, Wrap)

// Amount of total elapsed game time in floating point seconds
float GameTimeSeconds;

DECLARE_TEXTURE(WaterNormal0, normalXSampler, Wrap, Wrap)
DECLARE_TEXTURE(WaterNormal1, normalYSampler, Wrap, Wrap)

bool UseBumpMap;

float WaterOpacity;

struct MainShadingVSInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
    float3 Binormal : BINORMAL0;
    float3 Tangent : TANGENT0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct MainShadingVSOutput
{
    float4 Position : POSITION0;
    // We declare the following as TEXCOORD to get interpolation across pixels
    float3 Normal : TEXCOORD0;
    float3 Tangent : TEXCOORD1;
    float3 Binormal : TEXCOORD2;
    float2 TextureCoordinate : TEXCOORD3;
    float2 Depth : TEXCOORD4;
    float4 WorldSpacePosition : TEXCOORD5;
    float4 SunSpacePosition : TEXCOORD6;
};

MainShadingVSOutput MainShadingVS(MainShadingVSInput input)
{
    MainShadingVSOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    worldPosition.y += sin(GameTimeSeconds) * 0.5;
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Shift the position by y for a wave-like effect

    // We need the world-space position value to calculate the view vector for
    // Phong specular, using also the world-space camera position and world-
    // space light directions.
    output.WorldSpacePosition = worldPosition;

    // Compute world-space normal. Multiplying by the world matrix's inverse
    // transform accounts for non-uniform scaling that wouldn't be accounted
    // by a simple multiplication by the world matrix. As long as the scaling
    // is uniform, the operations are identical.
    output.Normal = normalize(mul(input.Normal.xyz, WorldInverseTranspose));

    // Also do the same for the tangent and binormal, which we need for the
    // bump map effect
    output.Tangent = normalize(mul(input.Tangent, WorldInverseTranspose));
    output.Binormal = normalize(mul(input.Binormal, WorldInverseTranspose));

    // Push [0,1] UV texture coordinates to fragment shader
    output.TextureCoordinate = input.TextureCoordinate;

    // Write the z depth (world-space in relation to the camera) and the
    // homogenous w for scaling it. This will be interpolated as it goes into
    // the pixel shader (since it has the TEXCOORD semantic). In the pixel
    // shader, we'll use the value of z / w as the pixel depth value.
    //
    // This is the same approach as done for the light depth map generation,
    // but for the camera depth so that we can do interesting screen-space
    // post-processing effects with it.
    output.Depth.xy = output.Position.zw;

    // Move the world position by an offset received as parameter, in the
    // direction of the normal. This effectively moves the vertex into the
    // light if the face was perpendicular to the light direction, fixing
    // shadow acne.
    float4 offsetPosition = worldPosition;
    offsetPosition.xyz += ShadowMapNormalOffset * output.Normal;

    // Also push the sun screen-space position [-1, 1] so it gets interpolated
    // for the pixels and we can use it in the pixel shader to query the
    // shadow map.
    float4 sunViewPosition = mul(offsetPosition, SunView);
    output.SunSpacePosition = mul(sunViewPosition, SunProj);
    return output;
}

// The pixel shader of the main shading pass outputs both color and depth.
// TODO: find out if float4 for depth is fine given it's a single float
struct PixelShaderOutput
{
    float4 Color : COLOR0;
    float4 Depth : COLOR1;
};

float4 SampleWaterTexture(float2 uv)
{
    // TODO: set this as a parameter
    uv *= 3.0f;
    // Oscillate vertically a little bit
    uv.y += sin(GameTimeSeconds) / 256.0;
    return SAMPLE_TEXTURE(textureSampler, uv, ModelTextureGammaCorrection);
}

float3 SampleWaterNormal(float3 normal, float3 tangent, float3 binormal, float2 uv)
{
    uv *= 3.0f;
    float4 normal0 = SAMPLE_TEXTURE(normalXSampler, uv + float2(0, GameTimeSeconds / 50.0), WaterNormal0GammaCorrection);
    float4 normal1 = SAMPLE_TEXTURE(normalYSampler, uv + float2(GameTimeSeconds / 50.0, 0), WaterNormal1GammaCorrection);

    normal0 = 2.0f * normal0 - 1.0f;
    normal1 = 2.0f * normal1 - 1.0f;
    float3 bump = normal0 + normal1;
    return normalize(normal + bump.x * tangent + bump.y * binormal);
}

PixelShaderOutput MainShadingPS(MainShadingVSOutput input)
{
    PixelShaderOutput output;

    // Sample material texture based on vertex UV passed from the vertex shader
    float4 textureColor = SampleWaterTexture(input.TextureCoordinate);

    // Use two bump maps if we have told it so
    float3 normal = UseBumpMap ? SampleWaterNormal(input.Normal, input.Tangent, input.Binormal, input.TextureCoordinate) : input.Normal;

    // The specular and diffuse components are added for every directional light
    // We multiply by two (arbitrary) to add a kind of shininess and transparency
    // of water that can't just be expressed by the specular attribute (idk, it
    // looked nice)
    output.Color = 2 * CalculateLightingContributions(normal, input.SunSpacePosition, input.WorldSpacePosition, CameraPosition);

    // Multiply all the lighting so far by the texture color. If there is no
    // texture, this will result in a multiplication by zero, thus black.
    output.Color *= textureColor;

    // Keep alpha at a constant since the value would otherwise be a mess from
    // the various lighting components that didn't care about alpha.
    output.Color.a = WaterOpacity;

    // Interpolate to white near the end of the camera far plane (assumed to be 1000)
    output.Color = AddFog(output.Color, 800.0, 1000.0, length(input.WorldSpacePosition - CameraPosition));

    // Write to a depth buffer too, for use in post-processing shaders
    output.Depth = input.Depth.x / input.Depth.y;

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
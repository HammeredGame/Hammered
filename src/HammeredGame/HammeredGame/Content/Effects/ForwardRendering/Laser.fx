#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This shader defines techniques for the Laser. Much of it is very similar
// to the main shader (e.g. it's affected by lighting, has a shadow-map
// generation pass) but there's laser-specific code at the bottom which is
// based on the following Unity laser beam tutorial, translated manually to
// MonoGame HLSL: https://www.youtube.com/watch?v=mGd3nYXj1Oc

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
    // A laser will not cast shadows so its write to the depth buffer will
    // be transparent
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

// The color for the laser, which will be alpha-masked by the laser texture
DECLARE_TEXTURE(LaserMaterial, materialTextureSampler, Clamp, Clamp);

// The texture for the laser (should have top and bottom fade to alpha, and the
// otherwise white. Recommended size is 512px or less)
DECLARE_TEXTURE(LaserTexture, laserTextureSampler, Wrap, Wrap);

// The alpha mask for the laser, applied on the whole object. This is used to
// make the hard edges at the start and end fade out.
DECLARE_TEXTURE(LaserMask, laserMaskSampler, Clamp, Clamp);

// Amount to multiply the laser color by (since we're in HDR we can go above
// 1 for RGB values to simulate bright objects)
float LaserIntensity;

// Amount of total elapsed game time in floating point seconds
float GameTimeSeconds;

// Speed in XY for the texture UV coordinates to change
float2 LaserSpeed;

struct MainShadingVSInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct MainShadingVSOutput
{
    float4 Position : POSITION0;
    // We declare the following as TEXCOORD to get interpolation across pixels
    float3 Normal : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
    float2 Depth : TEXCOORD2;
    float4 WorldSpacePosition : TEXCOORD3;
    float4 SunSpacePosition : TEXCOORD4;
};

MainShadingVSOutput MainShadingVS(MainShadingVSInput input)
{
    MainShadingVSOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // We need the world-space position value to calculate the view vector for
    // Phong specular, using also the world-space camera position and world-
    // space light directions.
    output.WorldSpacePosition = worldPosition;

    // Compute world-space normal. Multiplying by the world matrix's inverse
    // transform accounts for non-uniform scaling that wouldn't be accounted
    // by a simple multiplication by the world matrix. As long as the scaling
    // is uniform, the operations are identical.
    float3 normal = normalize(mul(input.Normal.xyz, WorldInverseTranspose));

    // Push normal and [0,1] UV texture coordinates to fragment shader
    output.Normal = normal;
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
    offsetPosition.xyz += ShadowMapNormalOffset * normal;

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

// Sample from the laser texture with a time-dependent offset to the UV
float4 ScrollingLaserTexture(float2 uv)
{
    // Add an offset to the UV based on the amount of game time and speed
    return SAMPLE_TEXTURE(laserTextureSampler, uv + LaserSpeed * GameTimeSeconds, LaserTextureGammaCorrection);
}

PixelShaderOutput MainShadingPS(MainShadingVSOutput input)
{
    PixelShaderOutput output;

    input.TextureCoordinate.xy = input.TextureCoordinate.yx;

    float4 laserTextureColor = ScrollingLaserTexture(input.TextureCoordinate);
    float4 materialColor = SAMPLE_TEXTURE(materialTextureSampler, input.TextureCoordinate, LaserMaterialGammaCorrection);

    // Sample the mask for the laser to fade out the edges
    float4 maskColor = SAMPLE_TEXTURE(laserMaskSampler, input.TextureCoordinate, LaserMaskGammaCorrection);

    // Multiply all the lighting so far by the texture color. If there is no
    // texture, this will result in a multiplication by zero, thus black.
    output.Color = materialColor;

    // Multiply the laser texture and its mask
    output.Color *= laserTextureColor * maskColor;

    // Multiply by intensity for HDR, before applying alpha which shouldn't be
    // multiplied
    output.Color *= LaserIntensity;

    // Use the product of the alpha for texture color and laser texture, and
    // the laser mask.
    output.Color.a = materialColor.a * laserTextureColor.a * maskColor.r;

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
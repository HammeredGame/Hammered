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
    float2 Depth : TEXCOORD0;
};

DepthMapVSOutput DepthMapVS(DepthMapVSInput input)
{
    DepthMapVSOutput output;

    // Transform vertex coordinates into light projection-space [-1, 1]
    output.Position = ObjectToProjection(input.Position);

    // Store the depth (in projection space, so Z direction) and the homogenous
    // w component. In the end we want to store z/w in the buffer but doing it
    // in the pixel shader is better than here since we can let the pipeline
    // automatically interpolate the values when declared with the TEXCOORD
    // semantic.
    output.Depth.xy = output.Position.zw;
    return output;
}

float4 DepthMapPS(float2 Depth : TEXCOORD0) : COLOR0
{
    float4 output = Depth.x / Depth.y;
    // Fix the alpha to 1 (TODO: why?)
    output.a = 1;
    return output;
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
DECLARE_TEXTURE(ModelTexture, textureSampler, Clamp, Clamp)

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

PixelShaderOutput MainShadingPS(MainShadingVSOutput input)
{
    PixelShaderOutput output;

    // Sample material texture based on vertex UV passed from the vertex shader
    float4 textureColor = SAMPLE_TEXTURE(textureSampler, input.TextureCoordinate, ModelTextureGammaCorrection);

    // Initialize the default color that we'll add to
    output.Color = float4(0, 0, 0, 0);

    // The specular and diffuse components are added for every directional light
	float3 normal = normalize(input.Normal);
    output.Color = CalculateLightingContributions(normal, input.SunSpacePosition, input.WorldSpacePosition, CameraPosition);

    // Multiply all the lighting so far by the texture color. If there is no
    // texture, this will result in a multiplication by zero, thus black.
    output.Color *= textureColor;

    // Keep alpha at 1 since the value would otherwise be a mess from the
    // various lighting components that didn't care about alpha.
    output.Color.a = 1;

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
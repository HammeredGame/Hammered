﻿#if OPENGL
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

// These three variables are common between both the light shadow map generation
// and the main shading pass, declare them up front. In the light shadow map
// generation, the view and projection matrices will be those of the light; in
// the main shading pass, they will be those of the camera. The world matrix is
// the same across both and is set by the model.
float4x4 World;
float4x4 View;
float4x4 Projection;

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
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

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

// Receive the world matrix's inverse transform, so that we can convert the
// object-space normal to world space taking into account any non-uniform
// scaling. If the model scaling is uniform in XYZ, then this is identical to
// the world matrix.
float3x3 WorldInverseTranspose;

// Allow up to 5 directional lights to be passed in. These should be passed as
// a C# array. Arrays with less than 5 are permitted, in which case the other
// elements will use default values (color = 0, intensity = 0, direction = 0).
#define MAX_DIRECTIONAL_LIGHTS 5
float4 DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
float DirectionalLightIntensities[MAX_DIRECTIONAL_LIGHTS];
float3 DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];

// Allow one ambient light per scene
float4 AmbientLightColor;
float AmbientLightIntensity;

// Material properties, such as the tint to apply for diffuse, ambient, and
// specular (set to white to just let the texture color through).
//
// A material can also have a specular component, which needs to be enabled
// with the boolean and a shininess value above 0 has to be provided.
float4 MaterialDiffuseColor;
float4 MaterialAmbientColor;
bool MaterialHasSpecular;
float4 MaterialSpecularColor;
float MaterialShininess;

// The world-space camera position is used to calculate the view vector from
// the pixel to the camera, which is used by the Phong specular component.
float4 CameraPosition;

// The material texture
texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Whether to perform inverse-gamma correction on the texture. Usually this
// should be true because albedo textures are created in sRGB which is
// already gamma-corrected to be non-linear. We want to load linear textures
// though (to use with HDR, or physically accurate calculations) so we need
// to invert the correction.
// Reference: https://gamedev.stackexchange.com/questions/74324/gamma-space-and-linear-space-with-shader
// Reference: (Section on sRGB textures) https://learnopengl.com/Advanced-Lighting/Gamma-Correction
bool PerformTextureGammaCorrection;

// The sun's shadow map generated on the first technique
texture SunDepthTexture;
sampler2D sunDepthSampler = sampler_state
{
    Texture = (SunDepthTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// When naively done, depth value comparisons during the shading process will
// encounter floating point inconsistencies. This leads to visual artifacts
// known as Shadow Acne (see Shadow Mapping chapter on learnopengl.com). Using
// a depth bias and an offset in the normal are two strategies commonly employed
// to get rid of this. This is the approach documented in DigitalRune:
// https://digitalrune.github.io/DigitalRune-Documentation/html/3f4d959e-9c98-4a97-8d85-7a73c26145d7.htm
float ShadowMapDepthBias;
float ShadowMapNormalOffset;

// We also need the view and projection matrices used by the sun for the shadow
// map generation. This is for converting the world space vertex position to
// light screen space, so we can query the shadow map at the corresponding UV
// position.
float4x4 SunView;
float4x4 SunProj;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    // We declare the following as TEXCOORD to get interpolation across pixels
    float3 Normal : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
    float2 Depth : TEXCOORD2;
    float4 WorldSpacePosition : TEXCOORD3;
    float4 SunSpacePosition : TEXCOORD4;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

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

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    PixelShaderOutput output;

    // Sample material texture based on vertex UV passed from the vertex shader
	float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);

    // Account for gamma (transform from sRGB to linear, so that the HDR
    // tonemapping will bring it back to sRGB). See the comment attached
    // to the declaration of this boolean flag for more info.
    if (PerformTextureGammaCorrection)
    {
        textureColor = pow(textureColor, 2.2);
    }

    // The specular and diffuse components are added for every directional light
	float3 normal = normalize(input.Normal);
	for (int i = 0; i < 5; i++)
	{
		float3 toLight = normalize(DirectionalLightDirections[i]);

        // Diffuse component
        float diffuseWeight = saturate(dot(normal, toLight));
        float4 diffuse = DirectionalLightColors[i] * DirectionalLightIntensities[i] * diffuseWeight * MaterialDiffuseColor;

        // Add Specular component if we have declared one
        if (diffuseWeight > 0 && MaterialHasSpecular)
        {
            // Blinn-Phong using half vectors
            float3 toCamera = normalize(CameraPosition - input.WorldSpacePosition).xyz;
            float3 halfDir = normalize(toLight + toCamera);
            float specularWeight = pow(max(dot(normal, halfDir), 0.0f), MaterialShininess);

            float4 specular = DirectionalLightColors[i] * DirectionalLightIntensities[i] * specularWeight * MaterialSpecularColor;
            output.Color += diffuse + specular;
        }
        else
        {
            // Don't even add the specular component if the material isn't
            // declared specular, because Shininess = 0 creates visual
            // artifacts and isn't equivalent to the material being 100% diffuse.
            output.Color += diffuse;
        }
    }

    // Shadow Component with PCF (percentage component filtering)

    // Find the weighted projection coordinates XY [-1, 1] to use as UV queries
    // into the shadow map.
    float2 sunProjCoords = input.SunSpacePosition.xy / input.SunSpacePosition.w;
    // Shift the coordinates into [0,1] for UV
    sunProjCoords = sunProjCoords * 0.5 + float2(0.5, 0.5);
    // Invert the y coordinate since it if flipped between texture UV (top = 0)
    // and projection coordinates (top = 1)
    sunProjCoords.y = 1.0 - sunProjCoords.y;

    // Find the depth in the sun's screen space for the current pixel. This will
    // be compared against the depth in the shadow map. If the pixel is
    // unobstructed, the value will be same (bar f.p. errors). If the pixel is
    // obstructed, the current depth will be higher.
    float sunCurrentDepth = input.SunSpacePosition.z / input.SunSpacePosition.w;

    // There are many ways to account for the depth bias to tolerate. This is
    // not the approach done by LearnOpenGL (which didn't lead to satisfactory
    // results and wasn't customizable), but instead is the approach taken by
    // kosmonautgames on MonoGame Forums:
    // https://community.monogame.net/t/shadow-mapping-on-monogame/8212
    float bias = clamp(0.001 * tan(acos(dot(normal, normalize(DirectionalLightDirections[1])))), 0.0, ShadowMapDepthBias);

    // We could just compare the tex2D(sunDepthSampler, sunProjCoords) and the
    // current depth, but that would lead to harsh pixelated shadows. Instead,
    // we sample from a 5x5 box around the pixel (a.k.a. PCF) in the depth map
    // to get softened shadows.
    float shadow = 0.0;
    // To get the UV position of the surrounding light-projection pixel, we need
    // to know how big [0,1] is mapping to in pixels, which is [0,2048]
    float texelSize = 1.0 / 2048.0;
    for (int x = -2; x <= 2; x++)
    {
        for (int y = -2; y <= 2; y++)
        {
            // Retrieve the depth from the light shadow map and use the red
            // component, which stores the single floating point value.
            float pcfDepth = tex2D(sunDepthSampler, sunProjCoords + float2(x, y) * texelSize).r;

            // Add it to the shadow contribution as long as the depth of the
            // current pixel (from the light) is further than the closest
            // depth that the light found towards our direction.
            shadow += (sunCurrentDepth - bias) > pcfDepth ? 1.0 : 0.0;
        }
    }
    // Divide by box amount to make range [0,1]
    shadow /= 25.0;

    // Multiply the color so far (diffuse + specular) by (1 - shadow) so that
    // we retain more of the original color if there is less shadow.
    output.Color *= (1 - shadow);

    // Add the ambient component at the end, regardless of shadow. We want to
    // add and not multiply because even the darkest blacks should become
    // lightened up.
    float4 ambient = MaterialAmbientColor * AmbientLightColor * AmbientLightIntensity;
    output.Color += ambient;

    // Multiply all the lighting so far by the texture color. If there is no
    // texture, this will result in a multiplication by zero, thus black.
    output.Color *= textureColor;

    // Keep alpha at 1 since the value would otherwise be a mess from the
    // various lighting components that didn't care about alpha.
	output.Color.a = 1.0f;

    // Write to a depth buffer too, for use in post-processing shaders
    output.Depth = input.Depth.x / input.Depth.y;

    return output;
}

technique MainShading
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
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
};

DepthMapVSOutput DepthMapVS(DepthMapVSInput input)
{

    DepthMapVSOutput output;

    // Transform vertex coordinates into light projection-space [-1, 1]
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

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

// Receive the world matrix's inverse transform, so that we can convert the
// object-space normal to world space taking into account any non-uniform
// scaling. If the model scaling is uniform in XYZ, then this is identical to
// the world matrix.
float3x3 WorldInverseTranspose;

// Allow up to 5 directional lights to be passed in. This includes the sun.
// These should be passed as a C# array. Arrays with less than 5 are permitted,
// in which case the other elements will use default values. The defaults are
// color = 0, intensity = 0, direction = 0.
// This value was chosen arbitrarily, thinking we probably won't need more than five.
#define MAX_DIRECTIONAL_LIGHTS 5
float4 DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
float DirectionalLightIntensities[MAX_DIRECTIONAL_LIGHTS];
float3 DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];

// One of the above directional lights needs to be a sunlight. Should be
// between 0 and MAX_DIRECTIONAL_LIGHTS. This will be the one that the shadow
// map will attenuate. Defaults to 0 (the first light) if unspecified.
int SunLightIndex;

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

// The color for the laser, which will be alpha-masked by the laser texture
texture LaserMaterial;
sampler2D materialTextureSampler = sampler_state
{
    Texture = (LaserMaterial);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Whether to perform inverse-gamma correction on the texture
bool LaserMaterialGammaCorrection;

// The texture for the laser (should have top and bottom fade to alpha, and the
// otherwise white. Recommended size is 512px or less)
texture LaserTexture;
sampler2D laserTextureSampler = sampler_state
{
    Texture = (LaserTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    // Wrap coordinates since we'll keep adding gameTime * speed to UV coords
    AddressU = Wrap;
    AddressV = Wrap;
};

// Whether to perform inverse-gamma correction on the texture
bool LaserTextureGammaCorrection;

// Amount to multiply the laser color by (since we're in HDR we can go above
// 1 for RGB values to simulate bright objects)
float LaserIntensity;

// Amount of total elapsed game time in floating point seconds
float GameTimeSeconds;

// Speed in XY for the texture UV coordinates to change
float2 LaserSpeed;

// The alpha mask for the laser, applied on the whole object. This is used to
// make the hard edges at the start and end fade out.
texture LaserMask;
sampler2D laserMaskSampler = sampler_state
{
    Texture = (LaserMask);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Whether to perform inverse-gamma correction on the texture
bool LaserMaskGammaCorrection;

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

// Diffuse component calculation for pixel shader
float4 PhongDiffuse(float3 normal, float3 toLight, float4 lightColor, float lightIntensity)
{
    float diffuseWeight = saturate(dot(normal, toLight));
    return lightColor * lightIntensity * diffuseWeight * MaterialDiffuseColor;
}

// Specular component calculation for pixel shader using Blinn-Phong and
// half-vectors.
float4 BlinnPhongSpecular(float3 normal, float3 toLight, float4 lightColor, float lightIntensity, float4 pixelWorldPosition)
{
    float3 toCamera = normalize(CameraPosition - pixelWorldPosition).xyz;
    float3 halfDir = normalize(toLight + toCamera);
    float specularWeight = pow(max(dot(normal, halfDir), 0.0f), MaterialShininess);

    return lightColor * lightIntensity * specularWeight * MaterialSpecularColor;
}

// Shadow attenuation component for pixel shader using percentage component
// filtering (PCF) for slightly smoother shadows.
float PCFShadow(float3 normal, float3 toLight, float4 pixelSunProjPosition, float3 lightDirection)
{
    // Find the weighted projection coordinates XY [-1, 1] to use as UV queries
    // into the shadow map.
    float2 sunProjCoords = pixelSunProjPosition.xy / pixelSunProjPosition.w;
    // Shift the coordinates into [0,1] for UV
    sunProjCoords = sunProjCoords * 0.5 + float2(0.5, 0.5);
    // Invert the y coordinate since it if flipped between texture UV (top = 0)
    // and projection coordinates (top = 1)
    sunProjCoords.y = 1.0 - sunProjCoords.y;

    // Find the depth in the sun's screen space for the current pixel. This will
    // be compared against the depth in the shadow map. If the pixel is
    // unobstructed, the value will be same (bar f.p. errors). If the pixel is
    // obstructed, the current depth will be higher.
    float sunCurrentDepth = pixelSunProjPosition.z / pixelSunProjPosition.w;

    // There are many ways to account for the depth bias to tolerate. This is
    // not the approach done by LearnOpenGL (which didn't lead to satisfactory
    // results and wasn't customizable), but instead is the approach taken by
    // kosmonautgames on MonoGame Forums:
    // https://community.monogame.net/t/shadow-mapping-on-monogame/8212
    float bias = clamp(0.001 * tan(acos(dot(normal, normalize(lightDirection)))), 0.0, ShadowMapDepthBias);

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
    return shadow;
}

// Sample from the laser texture with a time-dependent offset to the UV
float4 ScrollingLaserTexture(float2 uv)
{
    // Add an offset to the UV based on the amount of game time and speed
    float4 textureColor = tex2D(laserTextureSampler, uv + LaserSpeed * GameTimeSeconds);

    // Account for gamma (transform from sRGB to linear) since alpha masks
    // are created in sRGB and the nice gradient from invisible to visible
    // will be non-linear unless it's corrected for here.
    if (LaserTextureGammaCorrection)
    {
        textureColor = pow(textureColor, 2.2);
    }

    return textureColor;
}

PixelShaderOutput MainShadingPS(MainShadingVSOutput input)
{
    PixelShaderOutput output;

    input.TextureCoordinate.xy = input.TextureCoordinate.yx;

    float4 laserTextureColor = ScrollingLaserTexture(input.TextureCoordinate);
    float4 materialColor = tex2D(materialTextureSampler, input.TextureCoordinate);

    if (LaserMaterialGammaCorrection)
    {
        materialColor = pow(materialColor, 2.2);
    }

    // Initialize the default color that we'll add to
    output.Color = float4(0, 0, 0, 0);

    // The specular and diffuse components are added for every directional light
	float3 normal = normalize(input.Normal);
	for (int i = 0; i < MAX_DIRECTIONAL_LIGHTS; i++)
	{
        float4 lightContribution = float4(0, 0, 0, 0);

		float3 toLight = normalize(DirectionalLightDirections[i]);

        // Diffuse component
        lightContribution += PhongDiffuse(normal, toLight, DirectionalLightColors[i], DirectionalLightIntensities[i]);

        // Add Specular component only if we have declared the material as such
        // because Shininess = 0 creates visual artifacts and isn't equivalent
        // to the material being 100% diffuse.
        if (dot(normal, toLight) > 0 && MaterialHasSpecular)
        {
            lightContribution += BlinnPhongSpecular(normal, toLight, DirectionalLightColors[i], DirectionalLightIntensities[i], input.WorldSpacePosition);
        }

        // If this light is the sun, attenuate the light contribution by the
        // shadow component too.
        if (i == SunLightIndex)
        {
            float shadow = PCFShadow(normal, toLight, input.SunSpacePosition, DirectionalLightDirections[i]);

            // Multiply the color so far (diffuse + specular) by (1 - shadow) so that
            // we retain more of the original color if there is less shadow.
            lightContribution *= (1 - shadow);
        }

        // Add the final contribution to the output color
        output.Color += lightContribution;
    }

    // Add the ambient component at the end. We want to add this and not
    // multiply because even the darkest blacks should become lightened up.
    float4 ambient = MaterialAmbientColor * AmbientLightColor * AmbientLightIntensity;
    output.Color += ambient;


    // Sample the mask for the laser to fade out the edges
    float4 maskColor = tex2D(laserMaskSampler, input.TextureCoordinate);
    // Make sure to use gamma correction for alpha too, since that is the important part
    if (LaserMaskGammaCorrection)
    {
        maskColor = pow(maskColor, 2.2);
    }

    // Multiply all the lighting so far by the texture color. If there is no
    // texture, this will result in a multiplication by zero, thus black.
    output.Color *= materialColor;

    // Multiply the laser texture and its mask
    output.Color *= laserTextureColor * maskColor;

    // Multiply by intensity for HDR, before applying alpha which shouldn't be
    // multiplied
    output.Color *= LaserIntensity;

    // Use the product of the alpha for texture color and laser texture, and
    // the laser mask.
    output.Color.a = materialColor.a * laserTextureColor.a * maskColor.r;

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
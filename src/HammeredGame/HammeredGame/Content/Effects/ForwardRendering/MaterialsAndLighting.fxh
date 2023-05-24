// Declares material properties and lighting properties and methods.

#include "Macros.fxh"

// Whether to enable lighting contributions or to render flat if false.
bool Lit;

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

// The sun's shadow map generated on the first technique
DECLARE_TEXTURE(SunDepthTexture, sunDepthSampler, Clamp, Clamp);

// When naively done, depth value comparisons during the shading process will
// encounter floating point inconsistencies. This leads to visual artifacts
// known as Shadow Acne (see Shadow Mapping chapter on learnopengl.com). Using
// a depth bias and an offset in the normal are two strategies commonly employed
// to get rid of this. This is the approach documented in DigitalRune:
// https://digitalrune.github.io/DigitalRune-Documentation/html/3f4d959e-9c98-4a97-8d85-7a73c26145d7.htm
float ShadowMapDepthBias;
float ShadowMapNormalOffset;

#define SHADOW_OPACITY 0.7

// We also need the view and projection matrices used by the sun for the shadow
// map generation. This is for converting the world space vertex position to
// light screen space, so we can query the shadow map at the corresponding UV
// position.
float4x4 SunView;
float4x4 SunProj;

// Diffuse component calculation for pixel shader
float4 PhongDiffuse(float3 normal, float3 toLight, float4 lightColor, float lightIntensity)
{
    float diffuseWeight = saturate(dot(normal, toLight));
    return lightColor * lightIntensity * diffuseWeight * MaterialDiffuseColor;
}

// Specular component calculation for pixel shader using Blinn-Phong and
// half-vectors.
float4 BlinnPhongSpecular(float3 normal, float3 toLight, float4 lightColor, float lightIntensity, float4 cameraPosition, float4 pixelWorldPosition)
{
    float3 toCamera = normalize(cameraPosition - pixelWorldPosition).xyz;
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
    [unroll]
    for (int x = -2; x <= 2; x++)
    {
        [unroll]
        for (int y = -2; y <= 2; y++)
        {
            // Retrieve the depth from the light shadow map and use the red
            // component, which stores the single floating point value.
            float pcfDepth = SAMPLE_TEXTURE(sunDepthSampler, sunProjCoords + float2(x, y) * texelSize, false).r;

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

// Calculate the lighting contribution at a certain pixel, so that a return
// value of white means it's completely lit and 0 means not lit at all. The
// result of this should be multiplied by the texture color.
float4 CalculateLightingContributions(float3 normal, float4 pixelSunSpacePosition, float4 pixelWorldSpacePosition, float4 cameraWorldSpacePosition)
{
    if (Lit == false)
    {
        return float4(1.0, 1.0, 1.0, 1.0);
    }

    float4 output = float4(0, 0, 0, 0);
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
            lightContribution += BlinnPhongSpecular(normal, toLight, DirectionalLightColors[i], DirectionalLightIntensities[i], cameraWorldSpacePosition, pixelWorldSpacePosition);
        }

        // If this light is the sun, attenuate the light contribution by the
        // shadow component too.
        if (i == SunLightIndex)
        {
            float shadow = PCFShadow(normal, toLight, pixelSunSpacePosition, DirectionalLightDirections[i]);

            // Multiply the color so far (diffuse + specular) by (1 - shadow) so that
            // we retain more of the original color if there is less shadow.
            // Tone down the shadow opacity slightly
            lightContribution *= (1 - shadow * SHADOW_OPACITY);
        }

        // Add the final contribution to the output color
        output += lightContribution;
    }

    // Add the ambient component at the end. We want to add this and not
    // multiply because even the darkest blacks should become lightened up.
    float4 ambient = MaterialAmbientColor * AmbientLightColor * AmbientLightIntensity;
    return output + ambient;
}
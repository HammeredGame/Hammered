#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Light direction
float3 lightDirection;
float3 lightColor;
float lightIntensity;

// Camera position for specular light
float3 cameraPosition;

// For calculating world-position from depth
float4x4 InverseViewProjection;

texture diffuseMap;
texture normalMap;
texture depthMap;

sampler2D diffuseSampler = sampler_state
{
    Texture = (diffuseMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    MipFilter = LINEAR;
};

sampler2D normalSampler = sampler_state
{
    Texture = (normalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};


sampler2D depthSampler = sampler_state
{
    Texture = (depthMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Get normals from normalMap
    float4 normalData = tex2D(normalSampler, input.TexCoord);

    float3 normal = 2.0f * normalData.xyz - 1.0f;

    float3 lightVector = -normalize(lightDirection);
    float dotProduct = max(0, dot(normal, lightVector));
    float3 diffuseLight = dotProduct * lightColor.rgb * lightIntensity;

    float3 reflectionVector = normalize(reflect(lightVector, normal));

    return float4(diffuseLight.rgb, 1);
}

technique Basic
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}

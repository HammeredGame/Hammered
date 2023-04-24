﻿#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float3x3 WorldInverseTranspose;

float3 DiffuseLightDirection;
float4 DiffuseColor;
float DiffuseIntensity;

float Shininess;
float4 SpecularColor;
float SpecularIntensity;
float4 ViewVector;

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};


struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Normal : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
    float2 Depth : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Compute Diffuse lighting using cosine weighting
    float4 normal = float4(normalize(mul(input.Normal.xyz, WorldInverseTranspose)), 1);
    float lightIntensity = dot(normal.xyz, DiffuseLightDirection);
    // Keep alpha at 1
    output.Color = float4(saturate(DiffuseColor.xyz * DiffuseIntensity * lightIntensity), 1.0);
    // Push normal and texture to fragment shader
    output.Normal = normal;
    output.TextureCoordinate = input.TextureCoordinate;
    output.Depth.xy = output.Position.zw;
    return output;
}

struct PixelShaderOutput
{
    float4 Color : COLOR0;
    float4 Normal : COLOR1;
    half4 Depth : COLOR2;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    PixelShaderOutput output;
    // Get light direction and normal
    float3 light = normalize(DiffuseLightDirection);
    float3 normal = normalize(input.Normal);

    // Compute reflected light direction
    float3 r = normalize(2 * dot(light, normal) * normal - light);
    float3 v = normalize(mul(normalize(ViewVector), World).xyz);

    // Get reflection angle
    float cosThetaR = dot(r, v);

    // Compute the specular highlight intensity
    float4 specular = SpecularIntensity * SpecularColor * max(pow(cosThetaR, Shininess), 0) * length(input.Color);

    // Sample the model's texture
    float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);
    textureColor.a = 1;

//    float4 color = saturate(textureColor * (input.Color) + AmbientColor * AmbientIntensity + specular);
    output.Color = textureColor;
    output.Normal = float4(0.5 * normal + 1.0, 1.0);
    output.Depth = input.Depth.x / input.Depth.y;
    return output;
}

technique Basic
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}

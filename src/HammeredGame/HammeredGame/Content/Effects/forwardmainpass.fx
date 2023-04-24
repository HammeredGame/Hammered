#if OPENGL
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

float4 DirectionalLightColors[5];
float DirectionalLightIntensities[5];
float3 DirectionalLightDirections[5];

float4 AmbientColor;
float AmbientIntensity;

float4 DiffuseColor;

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

    // Compute effects of directional lighting using cosine weighting
    float3 normal = normalize(mul(input.Normal.xyz, WorldInverseTranspose));

    // Push normal and texture to fragment shader
    output.Normal = normal;
    output.TextureCoordinate = input.TextureCoordinate;
    output.Depth.xy = output.Position.zw;
    return output;
}

struct PixelShaderOutput
{
    float4 Color : COLOR0;
    half4 Depth : COLOR1;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    PixelShaderOutput output;

	float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);

    // Get light direction and normal

	float3 normal = normalize(input.Normal);
	for (int i = 0; i < 5; i++)
	{
		float3 toLight = normalize(DirectionalLightDirections[i]);

		float3 r = normalize(2 * dot(toLight, normal) * normal - toLight);
		float3 v = normalize(mul(normalize(ViewVector), World).xyz);

         // Get reflection angle
		float cosThetaR = dot(r, v);
		float4 specular = SpecularIntensity * SpecularColor * max(pow(cosThetaR, Shininess), 0);

		float cosineWeight = saturate(dot(normal.xyz, toLight));
		output.Color += DirectionalLightColors[i] * DirectionalLightIntensities[i] * cosineWeight;
//		+specular;
	}

	output.Color += AmbientColor * AmbientIntensity;

    output.Color *= textureColor;

    // Keep alpha at 1
	output.Color.a = 1.0f;

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

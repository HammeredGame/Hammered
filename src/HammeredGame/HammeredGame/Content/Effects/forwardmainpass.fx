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

float4 AmbientLightColor;
float AmbientLightIntensity;

float4 MaterialDiffuseColor;
float4 MaterialAmbientColor;
float MaterialHasSpecular;
float4 MaterialSpecularColor;
float MaterialShininess;

float4 CameraPosition;

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
    float4 PositionOut : TEXCOORD3;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.PositionOut = output.Position;

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

    float4 ambient = MaterialAmbientColor * AmbientLightColor * AmbientLightIntensity;

	float3 normal = normalize(input.Normal);
	for (int i = 0; i < 5; i++)
	{
		float3 toLight = normalize(DirectionalLightDirections[i]);

        // Diffuse component
        float diffuseWeight = saturate(dot(normal, toLight));
        float4 diffuse = DirectionalLightColors[i] * DirectionalLightIntensities[i] * diffuseWeight * MaterialDiffuseColor;

        // Specular component
        float3 viewDir = normalize(CameraPosition - input.PositionOut).xyz;

        // Original phong: doesn't work with low shininess
        // float3 reflectDir = reflect(-toLight, normal);
        // float specularWeight = pow(max(dot(reflectDir, viewDir), 0.0f), MaterialShininess);

        // Blinn-Phong using half vectors
        if (diffuseWeight > 0 && MaterialHasSpecular)
        {
            float3 halfDir = normalize(toLight + viewDir);
            float specularWeight = pow(max(dot(normal, halfDir), 0.0f), MaterialShininess);

            float4 specular = DirectionalLightColors[i] * DirectionalLightIntensities[i] * specularWeight * MaterialSpecularColor;
            output.Color += diffuse + specular;
        }
        else
        {
            output.Color += diffuse;
        }
    }

    output.Color += ambient;

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

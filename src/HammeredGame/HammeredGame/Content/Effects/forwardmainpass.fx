#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Reference: https://learnopengl.com/Lighting/Materials
// Reference: https://learnopengl.com/Lighting/Basic-Lighting
// Reference: https://learnopengl.com/Advanced-Lighting/Advanced-Lighting

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
bool MaterialHasSpecular;
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

bool PerformTextureGammaCorrection;

texture SunDepthTexture;
sampler2D sunDepthSampler = sampler_state
{
    Texture = (SunDepthTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

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
    // We need the world-space position value to calculate the view vector for phong specular
    // in relation to the world-space camera position and world-space light directions.
    // The POSITION0 semantic gets erased between the vertex and pixel shader (it's written
    // somewhere in the DirectX docs) so we need to create another one as TEXCOORD (so it
    // gets interpolated)
    output.WorldSpacePosition = worldPosition;

    // Compute effects of directional lighting using cosine weighting
    float3 normal = normalize(mul(input.Normal.xyz, WorldInverseTranspose));

    // Push normal and texture to fragment shader
    output.Normal = normal;
    output.TextureCoordinate = input.TextureCoordinate;

    // Write the z depth (world-space in relation to the camera) and the homogenous w for scaling it.
    // This will be interpolated as it goes into the pixel shader (since it has the TEXCOORD semantic),
    // and then in the pixel shader, we'll use the value of z / w as the pixel depth value.
    output.Depth.xy = output.Position.zw;

    float4 sunViewPosition = mul(worldPosition, SunView);
    output.SunSpacePosition = mul(sunViewPosition, SunProj);
    return output;
}

struct PixelShaderOutput
{
    float4 Color : COLOR0;
    float4 Depth : COLOR1;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    PixelShaderOutput output;

	float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);

    // Account for gamma (transform from srgb to linear, so that the hdr tonemapping will bring it back to srgb)
    // Reference: https://gamedev.stackexchange.com/questions/74324/gamma-space-and-linear-space-with-shader
    // Reference: (Section on sRGB textures) https://learnopengl.com/Advanced-Lighting/Gamma-Correction
    if (PerformTextureGammaCorrection)
    {
        textureColor = pow(textureColor, 2.2);
    }

    // Ambient component, added once at the very end
    float4 ambient = MaterialAmbientColor * AmbientLightColor * AmbientLightIntensity;

    // The specular and diffuse components are added for every directional light
	float3 normal = normalize(input.Normal);
	for (int i = 0; i < 5; i++)
	{
		float3 toLight = normalize(DirectionalLightDirections[i]);

        // Diffuse component
        float diffuseWeight = saturate(dot(normal, toLight));
        float4 diffuse = DirectionalLightColors[i] * DirectionalLightIntensities[i] * diffuseWeight * MaterialDiffuseColor;

        // Specular component
        float3 toCamera = normalize(CameraPosition - input.WorldSpacePosition).xyz;

        // Original phong: doesn't work with low shininess
        // float3 reflectDir = reflect(-toLight, normal);
        // float specularWeight = pow(max(dot(reflectDir, toCamera), 0.0f), MaterialShininess);

        // Blinn-Phong using half vectors
        if (diffuseWeight > 0 && MaterialHasSpecular)
        {
            float3 halfDir = normalize(toLight + toCamera);
            float specularWeight = pow(max(dot(normal, halfDir), 0.0f), MaterialShininess);

            float4 specular = DirectionalLightColors[i] * DirectionalLightIntensities[i] * specularWeight * MaterialSpecularColor;
            output.Color += diffuse + specular;
        }
        else
        {
            output.Color += diffuse;
        }
    }

    // Find the distance to the sunlight
    float2 sunProjCoords = input.SunSpacePosition.xy / input.SunSpacePosition.w;
    sunProjCoords = sunProjCoords * 0.5 + float2(0.5, 0.5);
    sunProjCoords.y = 1.0 - sunProjCoords.y;
    float sunClosestDepth = tex2D(sunDepthSampler, sunProjCoords).r;
    float sunCurrentDepth = input.SunSpacePosition.z / input.SunSpacePosition.w;
    float bias = max(0.05 * (1.0 - dot(normal, normalize(DirectionalLightDirections[1]))), 0.005);
    float shadow = (sunCurrentDepth - 0.001) > sunClosestDepth ? 1.0 : 0.0;

    output.Color *= (1 - shadow);

    // Add the ambient component
    output.Color += ambient;

    // Multiply the lighting tints by the texture color. If there is no texture, this will become black
    output.Color *= textureColor;

    // Keep alpha at 1 since the value would otherwise be a mess from the various components
	output.Color.a = 1.0f;

    // Write to a depth buffer too, for use in post-processing shaders
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

struct JustDepthVertexShaderInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
};

struct JustDepthVertexShaderOutput
{
    float4 Position : POSITION0;
    float2 Depth : TEXCOORD2;
};


JustDepthVertexShaderOutput JustDepthVertexShader(JustDepthVertexShaderInput input)
{

    JustDepthVertexShaderOutput output;

    // Transform vertex coordinates into screen-space
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.Depth.xy = output.Position.zw;
    return output;
}

float4 JustDepthPixelShader(JustDepthVertexShaderOutput input) : COLOR0
{
    float4 output = input.Depth.x / input.Depth.y;
    output.a = 1;
    return output;
}

technique JustDepth
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL JustDepthVertexShader();
        PixelShader = compile PS_SHADERMODEL JustDepthPixelShader();

    }
}
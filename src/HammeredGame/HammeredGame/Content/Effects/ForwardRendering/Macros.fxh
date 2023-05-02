// Macro for declaring a texture, its sampler, and a boolean flag for gamma
// correction.
//
// The gamma correction flag should usually be true for albedos or textures
// and likely false for normal maps. This is because textures are created in
// sRGB which is already gamma-corrected to be non-linear. We want to load
// linear textures though (to use with HDR, and to be physically accurate
// in calculations) so we need to invert the correction if necessary.
//
// Reference: https://gamedev.stackexchange.com/questions/74324/gamma-space-and-linear-space-with-shader
// Reference: (Section on sRGB textures) https://learnopengl.com/Advanced-Lighting/Gamma-Correction
#define DECLARE_TEXTURE(VariableName, SamplerName, UFormat, VFormat)             \
    texture VariableName;                                                        \
    sampler2D SamplerName = sampler_state                                        \
    {                                                                            \
        Texture = (VariableName);                                                \
        MinFilter = Linear;                                                      \
        MagFilter = Linear;                                                      \
        AddressU = UFormat;                                                      \
        AddressV = VFormat;                                                      \
    };                                                                           \
    bool VariableName##GammaCorrection;

// Macro for sampling a texture, optionally performing gamma transformation
// from sRGB to linear space (so that the HDR tonemapping will bring it back
// to HDR.
//
// We explicitly check for == true since the MGFX compiler doesn't like it
// otherwise.
#define SAMPLE_TEXTURE(SamplerName, UV, DoInverseGammaCorrect)                   \
    (DoInverseGammaCorrect == true ? pow(tex2D(SamplerName, UV), 2.2) : tex2D(SamplerName, UV))
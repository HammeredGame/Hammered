#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// This shader defines an animated diagonal stripe background.

// GLSL mod function, since there is no equivalent in HLSL.
// HLSL's fmod() function does not work as expected for repeating patterns when
// the first value is negative.
#define mod(x, y) ((x) - (y) * floor((x) / (y)))

// The two colours to use
float4 Color1;
float4 Color2;

// How many pairs of Color1 and Color2 to show on screen at once.
float Divisions;

// How wide Color2 should be with respect to Color1
float Ratio;

// The screen/rectangle aspect ratio, because the shader only knows about [0,1]
// vertex texture coordinates and would calculate angles wrongly without it.
float AspectRatio;

// The speed of horizontal movement
float Speed;

// The angle of the lines in radians. 0 is straight vertical lines moving
// horizontally, while positive Pi/4 would be something like forward slashes,
// and negative Pi/4 would be like backward slashes.
float Angle;

// How much of the right side to cut away for transitioning in, with 0 being
// the start of the transition-in and 1 for the stripes being fully visible.
float TransitionInProgress;

// How much of the left side to cut away, similar to the InProgress parameter.
// 0 is when stripes are completely visible, while 1 means transition completed
// and all is transparent.
float TransitionOutProgress;

// Amount of total elapsed game time in floating point seconds
float GameTimeSeconds;

float4 PixelShaderFunction(float2 TexCoord : TEXCOORD0) : COLOR0
{
    // Imagine that the screen space TexCoord xy in [0,1] is divided into
    // "Divisions" number of divisions, each 1.0 / Division wide. The
    // x coordinate within each of these divisions gets scaled to [0,1],
    // which we do by a modulo and multiplication.
    //
    //   valueWithinOneWidth = mod(TexCoord.x, 1.0 / Divisions) * Divisions
    //
    // Diagonal lines means that depending on the y coordinate, we change
    // the input into the above function. We use the sine and cosine and the
    // aspect ratio to calculate the proper offset. It's funky but it works
    // out if you do the math.
    //
    //   valueWithinOneWidth = mod(TexCoord.x * cos(Angle) / AspectRatio + TexCoord.y * sin(Angle), 1.0 / Divisions) * Divisions
    //
    // Then we animate this by changing the X coordinate by a component of
    // Speed * GameTime.
    //
    // Finally, because each of these [0,1] ranges of X will contain a pair of
    // Color1 and Color2, with Color1 having the width of 1/(Ratio + 1) and
    // Color2 having the width of Ratio/(Ratio + 1), we do a threshold check to
    // return those colours.
    float valueWithinOneWidth = mod(TexCoord.x * cos(Angle) / AspectRatio + Speed * GameTimeSeconds + TexCoord.y * sin(Angle), 1.0 / Divisions) * Divisions;

    float4 color;
    if (valueWithinOneWidth <= (1.0 / (Ratio + 1.0)))
    {
        color = Color1;
    }
    else
    {
        color = Color2;
    }

    // For transitions, we cut the left or right sides diagonally, so
    // essentially we just use the same formula as above but without the
    // containing modulo and multiplication operation, and compare it against
    // some threshold.
    if ((TexCoord.x * cos(Angle) / AspectRatio + TexCoord.y * sin(Angle)) < TransitionOutProgress ||
        (TexCoord.x * cos(Angle) / AspectRatio + TexCoord.y * sin(Angle)) > TransitionInProgress)
    {
        color.a = 0;
    }
    return color;
};

technique StripeBackground
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
// These three variables are common between both the light shadow map generation
// and the main shading pass, declare them up front. In the light shadow map
// generation, the view and projection matrices will be those of the light; in
// the main shading pass, they will be those of the camera. The world matrix is
// the same across both and is set by the model.
float4x4 World;
float4x4 View;
float4x4 Projection;

// Receive the world matrix's inverse transform, so that we can convert the
// object-space normal to world space taking into account any non-uniform
// scaling. If the model scaling is uniform in XYZ, then this is identical to
// the world matrix.
float3x3 WorldInverseTranspose;

float4 ObjectToProjection(float4 position)
{
    // Transform vertex coordinates into light projection-space [-1, 1]
    float4 worldPosition = mul(position, World);
    float4 viewPosition = mul(worldPosition, View);
    return mul(viewPosition, Projection);
}

float4 AddFog(float4 originalColor, float fogStart, float fogEnd, float distance)
{
    return lerp(originalColor, float4(0, 0, 0, 0), saturate((distance - fogStart) / (fogEnd - fogStart)));
}
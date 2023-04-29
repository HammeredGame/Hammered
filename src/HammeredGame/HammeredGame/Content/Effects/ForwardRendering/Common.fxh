// These three variables are common between both the light shadow map generation
// and the main shading pass, declare them up front. In the light shadow map
// generation, the view and projection matrices will be those of the light; in
// the main shading pass, they will be those of the camera. The world matrix is
// the same across both and is set by the model.
float4x4 World;
float4x4 View;
float4x4 Projection;

float4 ObjectToProjection(float4 position)
{
    // Transform vertex coordinates into light projection-space [-1, 1]
    float4 worldPosition = mul(position, World);
    float4 viewPosition = mul(worldPosition, View);
    return mul(viewPosition, Projection);
}

float4 AddFog(float4 originalColor, float fogStart, float fogEnd, float distance)
{
    return lerp(originalColor, float4(1, 1, 1, 0), saturate((distance - fogStart) / (fogEnd - fogStart)));
}
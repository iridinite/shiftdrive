#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix View;
matrix Projection;

texture SkyboxTexture;
sampler SkyboxSampler = sampler_state
{
    Texture = <SkyboxTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    MipFilter = LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float3 rotatedPos = (float3)mul(input.Position, View);

    output.Position = mul(float4(rotatedPos, 1), Projection).xyww;
    output.TexCoord = (float3)input.Position;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    return texCUBE(SkyboxSampler, input.TexCoord);
}

technique SkySphere
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();

        CullMode = CW;
        ZWriteEnable = false;
    }
};

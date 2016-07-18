float4x4 World;
float4x4 View;
float4x4 Projection;

float3 LightDirection = normalize(float3(-1, -1, -1));
float3 AmbientLight = float3(0.35, 0.35, 0.35);

texture ModelTexture;
sampler2D textureSampler = sampler_state {
	Texture = (ModelTexture);
	MinFilter = Linear;
	MagFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VertexShaderInput
{
	float4 Position : SV_POSITION0;
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Normal : TEXCOORD0;
	float2 TexCoord : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.TexCoord = input.TexCoord;
	output.Normal = normalize(mul(input.Normal, World));

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float diffuseAmount = max(-dot(input.Normal, LightDirection), 0);
	float3 lightingResult = saturate(diffuseAmount + float4(AmbientLight, 1));

	return tex2D(textureSampler, input.TexCoord) * float4(lightingResult, 1);
}

technique Basic
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 VertexShaderFunction();
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}
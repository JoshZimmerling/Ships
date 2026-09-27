Shader "Custom/TrueVision"
{
	Properties
	{
		_MainTex("Base Texture", 2D) = "white" {}
		_VisibleWhenShipNearby("Visible When Ship Nearby", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10
	}

	SubShader 
	{
		Tags
		{
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0 

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes 
			{
				float4 positionOS : POSITION;
				float2 uv		  : TEXCOORD0;
				float4 color      : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD2; 
				float2 uv		  : TEXCOORD0;
				float4 color      : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float _VisibleWhenShipNearby;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			
			float4 _GlobalPointsBuffer[128]; 
			int _GlobalPointsBufferCount;

			Varyings vert(Attributes input) 
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.color = input.color;

				return output;
			}

			float4 frag(Varyings input) : SV_TARGET 
			{
				float4 orgColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
				orgColor.rgb *= orgColor.a;
				float4 transColor = float4(0, 0, 0, 0);

				if (_GlobalPointsBufferCount <= 0)
				{
					return orgColor;
				}

				for (int i = 0; i < _GlobalPointsBufferCount; i++)
				{
					float maxRadius = _GlobalPointsBuffer[i].w;
					if(maxRadius > 0)
					{
						float2 diff = _GlobalPointsBuffer[i].xy - input.positionWS.xy;
						float distSq = dot(diff, diff);
						float maxRadiusSq = maxRadius * maxRadius;

						if (distSq < maxRadiusSq)
						{
							if (_VisibleWhenShipNearby == 1.0)
							{
								return orgColor;
							}
							else
							{
								return transColor;
							}
						}
					}
				}

				if (_VisibleWhenShipNearby == 1.0)
				{
					return transColor;
				}
				else
				{
					return orgColor;
				}
			}

			ENDHLSL
		}
	}
}

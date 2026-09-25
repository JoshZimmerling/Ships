Shader "Custom/TrueVision"
{
	Properties
	{
		_MainTex("Base Texture", 2D) = "white" {}
		//_Color("Base Color", Color) = (1, 1, 1, 1)
		_VisibleWhenShipNearby("Visible When Ship Nearby", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10
	}

	SubShader 
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

            #pragma target 4.5 

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

			struct Attributes //Mesh data
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

			struct VisionCone
			{
				float2 position;
				float visionRadius;
			};


			CBUFFER_START(UnityPerMaterial)
				bool _VisibleWhenShipNearby;

				//float4 _Color;
				//float4 _RendererColor;

				float4 _MainTex_ST;
				
				int _PointsBufferCount;
			CBUFFER_END
			StructuredBuffer<VisionCone> _PointsBuffer;

			sampler2D _MainTex;

			Varyings vert(Attributes input) // Physical verticies
			{
				Varyings output = (Varyings)0;

				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.color = input.color;

				return output;
			}

			float4 frag(Varyings input) : SV_TARGET // Visual appearance
			{
				float4 orgColor = tex2D(_MainTex, input.uv) * input.color;
				orgColor.rgb *= orgColor.a;
				float4 transColor = float4(0, 0, 0, 0);

				if (_PointsBufferCount == 0)
					return orgColor;

				for (int i = 0; i < _PointsBufferCount; i++)
				{
					float maxRadius = _PointsBuffer[i].visionRadius;
					if(maxRadius > 0)
					{
						float2 diff = _PointsBuffer[i].position.xy - input.positionWS.xy;
						float distSq = dot(diff, diff);

						float maxRadius = _PointsBuffer[i].visionRadius;
						float maxRadiusSq = maxRadius * maxRadius;

						if (distSq < maxRadiusSq)
						{
							if (_VisibleWhenShipNearby == 1)
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

				if (_VisibleWhenShipNearby == 1)
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
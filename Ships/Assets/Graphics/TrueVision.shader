Shader "Custom/TrueVision"
{
	Properties
	{
		_MainTex("Base Texture", 2D) = "white" {}
		_VisibleWhenShipNearby("Visible When Ship Nearby", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5 // SrcAlpha
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10 // OneMinusSrcAlpha
		
		// Adjust edge falloff thickness (0.0 = sharp edge, 0.5 = very soft blend)
		_VisionSmoothness("Vision Edge Smoothness", Range(0.001, 1.0)) = 0.25
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
				float _VisibleWhenShipNearby;
				float _VisionSmoothness;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			float4 _MainTex_ST;
			
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

			float4 frag(Varyings input) : SV_Target 
			{
				// 1. Fetch base texture color safely without premature RGB alpha premultiplication
				float4 orgColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
				float combinedAlpha = 0;

				if (_GlobalPointsBufferCount <= 0)
				{
					if (_VisibleWhenShipNearby == 1.0)
					{
						return float4(0, 0, 0, 0);
					}
					else
					{
						return orgColor;
					}
				}

				// 2. Scan buffer for nearby point bounds
				for (int i = 0; i < _GlobalPointsBufferCount; i++)
				{
					float maxRadius = _GlobalPointsBuffer[i].w;
					if(maxRadius > 0)
					{
						float2 diff = _GlobalPointsBuffer[i].xy - input.positionWS.xy;
						float dist = length(diff); 

						if (dist < maxRadius) 
						{
							// Calculate the falloff edge threshold boundary
							float innerRadius = maxRadius * (1.0 - _VisionSmoothness);
							
							// Taper intensity dynamically: 1.0 at inner radius down to 0.0 at outer radius perimeter
							float edgeFalloff = 1.0 - smoothstep(innerRadius, maxRadius, dist);
							
							// Scale the stored buffer point alpha by our new edge falloff multiplier
							float pointAlpha = _GlobalPointsBuffer[i].z * edgeFalloff;

							// This merges overlapping falloffs naturally without exceeding 1.0.
							combinedAlpha = combinedAlpha + pointAlpha - (combinedAlpha * pointAlpha);
						}
					}
				}

				// Clamp the final merged value safely between 0.0 and 1.0
				combinedAlpha = saturate(combinedAlpha);

				// 3. Correctly apply dynamic opacity to the texture instead of returning pure black
				if (_VisibleWhenShipNearby == 1.0)
				{
					orgColor.a *= combinedAlpha;
				}
				else
				{
					orgColor.a *= (1.0 - combinedAlpha);
				}

				return orgColor;
			}
			ENDHLSL
		}
	}
}

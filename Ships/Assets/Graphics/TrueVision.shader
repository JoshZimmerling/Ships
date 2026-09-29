Shader "Custom/TrueVision"
{
	Properties
	{
		_MainTex("Base Texture", 2D) = "white" {}
		_VisibleWhenShipNearby("Visible When Ship Nearby", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5 // SrcAlpha
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10 // OneMinusSrcAlpha
		
		// Vision Edge parameters
		_VisionSmoothness("Vision Edge Smoothness", Range(0.001, 1.0)) = 0.25

		// Cloud Noise parameters
		_CloudSpeed("Cloud Morph Speed", Float) = 1.2
		_CloudScale("Cloud Density/Scale", Float) = 4.0
		_MinFogVisibility("Minimum Fog Visibility", Range(0.0, 1.0)) = 0.15
		
		// Controls how hard the second layer warps the first layer
		_WarpStrength("Turbulence / Distortion", Range(0.0, 2.0)) = 0.5
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
				float _CloudSpeed;
				float _CloudScale;
				float _MinFogVisibility;
				float _WarpStrength;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			float4 _MainTex_ST; // Safely outside the CBUFFER for 2D SRP batching
			
			float4 _GlobalPointsBuffer[128]; 
			int _GlobalPointsBufferCount;

			// Helper pseudo-random 2D grid noise function
			float hash2D(float2 p)
			{
				return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
			}

			// Smooth bilinear value noise for soft cloud-like shapes
			float SmoothValueNoise(float2 uv)
			{
				float2 i = floor(uv);
				float2 f = frac(uv);
				
				float2 u = f * f * (3.0 - 2.0 * f);

				return lerp(lerp(hash2D(i + float2(0.0, 0.0)), hash2D(i + float2(1.0, 0.0)), u.x),
							lerp(hash2D(i + float2(0.0, 1.0)), hash2D(i + float2(1.0, 1.0)), u.x), u.y);
			}

			// Two-dimensional warped noise calculation
			float GenerateWarpedCloudNoise(float2 uv, float time)
			{
				// Dimension 1: The Distortion Layer
				float2 warpUV = uv * (_CloudScale * 0.8);
				warpUV.x -= time * 0.2;
				warpUV.y += time * 0.4;
				
				float warpX = SmoothValueNoise(warpUV + float2(0.0, 0.0));
				float warpY = SmoothValueNoise(warpUV + float2(5.2, 1.3));
				float2 distortion = float2(warpX, warpY) * _WarpStrength;

				// Dimension 2: The Main Fog Layer
				float2 mainUV = (uv * _CloudScale) + distortion;
				mainUV.x += time * 0.4;
				mainUV.y -= time * 0.2;

				float value = 0.0;
				float amplitude = 0.5;
				
				value += amplitude * SmoothValueNoise(mainUV);
				
				mainUV *= 2.0;
				amplitude *= 0.5;
				value += amplitude * SmoothValueNoise(mainUV + float2(-time, time) * 0.15);

				return value;
			}

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

				// completely skip all vision logic and render the asset entirely as normal.
				if (_GlobalPointsBufferCount <= 0)
				{
					if (_VisibleWhenShipNearby == 1.0)
					{
						return orgColor;
					}
					else
					{
						return float4(0, 0, 0, 0);
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

							// Additive screen blending merges overlapping falloffs naturally
							combinedAlpha = combinedAlpha + pointAlpha - (combinedAlpha * pointAlpha);
						}
					}
				}

				combinedAlpha = saturate(combinedAlpha);

				// 3. Apply opacity calculations based on proximity toggle
				if (_VisibleWhenShipNearby == 1.0)
				{
					// Standard crisp look: clean alpha multiplication without any noise distortion
					orgColor.a *= combinedAlpha;
				}
				else
				{
					// Cloud effect is active: calculate moving multi-dimensional noise
					float timeValue = _Time.y * _CloudSpeed;
					float cloudNoise = GenerateWarpedCloudNoise(input.uv, timeValue);

					// Map the noise from [_MinFogVisibility to 1.0]
					cloudNoise = lerp(_MinFogVisibility, 1.0, cloudNoise);

					// Out of range (combinedAlpha = 0): Sprite opacity becomes cloudNoise
					// In center of range (combinedAlpha = 1): Sprite opacity becomes 0 (clear reveal)
					float visibilityMask = lerp(cloudNoise, 0.0, combinedAlpha);
					orgColor.a *= visibilityMask;
				}

				return orgColor;
			}
			ENDHLSL
		}
	}
}

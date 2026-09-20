Shader "Custom/Test"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_BaseTexture("Base Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10
		_AphaThreshold("Alpha Threshold", Range(0.0, 1.0)) = 0.5
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "AlphaTest"
		}

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

			#define MAX_POINTS 64
            

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseColor;
				float4 _BaseTexture_ST;

				float3 _Points[MAX_POINTS]; // X, Y, Range
				int _PointCount;
				//float _AlphaThreshold;
			CBUFFER_END

			TEXTURE2D(_BaseTexture);
			SAMPLER(sampler_BaseTexture);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv		  : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv		  : TEXCOORD0;
			};

			Varyings vert(Attributes v) // Physical verticies
			{
				Varyings output = (Varyings)0;

				output.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				output.uv = TRANSFORM_TEX(v.uv, _BaseTexture);

				return output;
			}

			float4 frag(Varyings input) : SV_TARGET // Visual appearance
			{
				half4 col = half4(0, 0, 0, 1);

                // Loop through the passed array of points
                int count = min(_PointCount, MAX_POINTS);
                for (int i = 0; i < count; i++)
                {
                    float dist = distance(input.positionWS, _Points[i].xyz);
                    if (dist < 1.0)
                    {
                        col = half4(1, 0, 0, 1); // Highlight near points
                    }
                }

                return col;

				//float4 outputColor = SAMPLE_TEXTURE2D(_BaseTexture, sampler_BaseTexture, i.uv) * _BaseColor;

				//if(outputColor.a < _AlphaThreshold)
				//{
				//	discard;
				//}

				//return outputColor;



				// 1. Calculate Mask UV based on World XZ Position
                //float2 maskUV = (input.worldPos.xz - _FogMaskParams.xy) / _FogMaskParams.zw;
                
                // 2. Clamp UVs to avoid edge bleeding
                //maskUV = saturate(maskUV);

                // 3. Sample the Fog Mask (Red = Discovered/Visible, Green = Explored/Shroud)
                //half4 fogMask = _FogOfWarMask.Sample(sampler_FogOfWarMask, maskUV);

                // 4. Sample base map color
                //half4 baseColor = _MainTex.Sample(sampler_MainTex, input.uv) * _BaseColor;

                // 5. Calculate visibility value (1 = visible, 0 = black fog, 0.3 = shroud/explored)
                // Red channel dictates immediate vision, Green can handle the persistent shroud
                //half visibility = max(fogMask.r, fogMask.g * 0.25); 

                // 6. Lerp between the original pixel color and the fog color
                //half4 finalColor = lerp(_FogColor, baseColor, visibility);

                //return finalColor;
			}

			ENDHLSL
		}
	}
}
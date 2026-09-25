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
		}

		Pass
		{
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Shader Model 4.5 is required to use StructuredBuffers in standard passes
			#pragma target 4.5 

			#include "UnityCG.cginc"

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

			struct VisionCone
			{
				// Changed to float4 to prevent memory alignment/stride mismatches 
				// between C# GraphicsBuffer/ComputeBuffer and HLSL.
				float4 position;   
				float visionRadius;
				float3 padding;   // Fully pads out the structure block to 32 bytes (multiples of 16)
			};

			bool _VisibleWhenShipNearby;
			float4 _MainTex_ST;
			int _PointsBufferCount;
			
			StructuredBuffer<VisionCone> _PointsBuffer;
			sampler2D _MainTex;

			Varyings vert(Attributes input) 
			{
				Varyings output = (Varyings)0;

				output.positionCS = UnityObjectToClipPos(input.positionOS.xyz);
				
				// FIXED: Replaced URP's TransformObjectToWorld with standard matrix multiplication
				output.positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
				
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.color = input.color;

				return output;
			}

			float4 frag(Varyings input) : SV_TARGET 
			{
				float4 orgColor = tex2D(_MainTex, input.uv) * input.color;
				orgColor.rgb *= orgColor.a;
				float4 transColor = float4(0, 0, 0, 0);

				if (_PointsBufferCount == 0)
				{
					return orgColor; // FIXED: Added missing semicolon
				}

				for (int i = 0; i < _PointsBufferCount; i++)
				{
					float maxRadius = _PointsBuffer[i].visionRadius;
					if(maxRadius > 0)
					{
						// input.positionWS is a 3D coordinate. Assuming a 2D plane gameplay loop,
						// we use .xy matching the VisionCone position vector.
						float2 diff = _PointsBuffer[i].position.xy - input.positionWS.xy;
						float distSq = dot(diff, diff);
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

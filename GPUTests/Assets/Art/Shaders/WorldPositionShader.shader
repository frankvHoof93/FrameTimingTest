Shader "OSVR/WorldPositionShader"
{
    Properties
    {
		[NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normals", 2D) = "white" {}
		[NoScaleOffset] _NormalCraterMap("Craters", 2D) = "white" {}
		_Offset("Offset", Vector) = (0,0,0,0)
		_BlendOffset("Blend Offset", Range(0, 0.5)) = 0.25
		_BlendExponent("Blend Exponent", Range(1, 8)) = 2
		_Scale("Scale", Range(1, 1000)) = 1
		_ScaleBump("bump",Float) = 1
		_HeightRange("HeightRange", Vector) = (0,1,0,200)
		_ColorLowRange("ColorLowRange", Color) = (0,0,0,1)
		_ColorHighRange("ColorhighRange", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			Tags {"LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			// shadow helper functions and macros
			#include "AutoLight.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent: TANGENT;
			};

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1; 
				float3 worldNormal : TEXCOORD2;
				float3 tangentViewDir: TEXCOORD3;
				SHADOW_COORDS(0) // put shadows data into TEXCOORD0
				fixed3 ambient : COLOR1;
            };

            sampler2D _MainTex;
			sampler2D _NormalMap;
			sampler2D _NormalCraterMap;
            float4 _MainTex_ST;
			float4 _ColorLowRange, _ColorHighRange, _HeightRange;

			float _BlendOffset, _BlendExponent, _BlendHeightStrength, _Scale, _ScaleBump;
			float2 _Offset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex );
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.ambient = ShadeSH9(half4(o.worldNormal, 1));

				float3x3 objectToTangent = float3x3(
					v.tangent.xyz,
					cross(v.normal, v.tangent.xyz) * v.tangent.w,
					v.normal
					);
				o.tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(v.vertex));

				TRANSFER_SHADOW(o);
                return o;
            }

			struct TriplanarUV
			{
				float2 x, y, z;
			};

			TriplanarUV GetTriplanarUV(float3 p)
			{
				TriplanarUV triUV;
				triUV.x = p.yz / _Scale;
				triUV.y = p.zx / _Scale;
				triUV.z = p.xy / _Scale;
				return triUV;
			}

			float3 GetTriplanarWeights(float3 n)
			{
				float3 triW = abs(n);
				triW = saturate(triW - _BlendOffset);
				triW = pow(triW, _BlendExponent);
				return triW / (triW.x + triW.y + triW.z);
			}

			float3 BlendTriplanarNormal(float3 mappedNormal, float3 surfaceNormal)
			{
				float3 n;
				n.xy = mappedNormal.xy + surfaceNormal.xy;
				n.z = mappedNormal.z * surfaceNormal.z;
				return n;
			}

			fixed4 frag (v2f i) : SV_Target
            { 
				i.tangentViewDir = normalize(i.tangentViewDir);
				i.tangentViewDir.xy /= (i.tangentViewDir.z + 0.42);
				float height = tex2D(_NormalCraterMap, i.worldPos.xz/_Scale + _Offset).g;
				height -= 0.5;
				height *= _ScaleBump;
				i.worldPos.xyz += i.tangentViewDir.xyz * height;
				TriplanarUV triUV = GetTriplanarUV(i.worldPos);

				float3 albedoX = tex2D(_MainTex, triUV.x + _Offset ).rgb;
				float3 albedoY = tex2D(_MainTex, triUV.y + _Offset ).rgb;
				float3 albedoZ = tex2D(_MainTex, triUV.z + _Offset ).rgb;

				float3 tangentNormalX = UnpackNormal(tex2D(_NormalMap, triUV.x + _Offset) );
				float3 tangentNormalY = UnpackNormal(tex2D(_NormalMap, triUV.y + _Offset) );
				float3 tangentNormalZ = UnpackNormal(tex2D(_NormalMap, triUV.z + _Offset) );

				if (i.worldNormal.x < 0)
				{
					tangentNormalX.x = -tangentNormalX.x;
				}
				if (i.worldNormal.y < 0)
				{
					tangentNormalY.x = -tangentNormalY.x;
				}
				if (i.worldNormal.z >= 0)
				{
					tangentNormalZ.x = -tangentNormalZ.x;
				}

				float3 worldNormalX =
					BlendTriplanarNormal(tangentNormalX, i.worldNormal.zyx).zyx;
				float3 worldNormalY =
					BlendTriplanarNormal(tangentNormalY, i.worldNormal.xzy).xzy;
				float3 worldNormalZ =
					BlendTriplanarNormal(tangentNormalZ, i.worldNormal);

				float3 triW = GetTriplanarWeights(normalize(i.worldNormal));

				i.worldNormal = normalize(
					worldNormalX * triW.x + worldNormalY * triW.y + worldNormalZ * triW.z
				);

				float4 c = clamp(lerp(_ColorLowRange, _ColorHighRange, (i.worldPos.y - _HeightRange.z) / (_HeightRange.w - _HeightRange.z)), _ColorLowRange, _ColorHighRange);

				float4 col = float4(albedoX * triW.x + albedoY * triW.y + albedoZ * triW.z, 1) *c;

				fixed shadow = SHADOW_ATTENUATION(i);
				half diffuse = saturate(dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * 1.2);
				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = diffuse * shadow + i.ambient;
				col.rgb *= lighting;
                return col;
            }
            ENDCG
        }

		// shadow casting support
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}

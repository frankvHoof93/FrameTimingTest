Shader "OSVR/Earth"
{
    Properties
    {
        _Day ("Day", 2D) = "white" {}
		_Night("Night", 2D) = "white" {}
		_Value("Night", Float) = 1
    }

	SubShader
	{
		Pass
		{
		// indicate that our pass is the "base" pass in forward
		// rendering pipeline. It gets ambient and main directional
		// light data set up; light direction in _WorldSpaceLightPos0
		// and color in _LightColor0
		Tags {"LightMode" = "ForwardBase"}

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" // for UnityObjectToWorldNormal
			#include "UnityLightingCommon.cginc" // for _LightColor0

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldNormal : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				// get vertex normal in world space
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				return o;
			}

			sampler2D _Day;
			sampler2D _Night;
			float _Value;

			fixed4 frag(v2f i) : SV_Target
			{
				// sample texture
				fixed4 colDay = tex2D(_Day, i.uv);
				fixed4 colNight = tex2D(_Night, i.uv);
				half diffuse = saturate(dot(_WorldSpaceLightPos0.xyz, i.worldNormal) * _Value);
				// multiply by lighting
				fixed4 col = lerp(colDay , colNight, 1 - diffuse);

				return col;
			}
		ENDCG
		}
	}
}

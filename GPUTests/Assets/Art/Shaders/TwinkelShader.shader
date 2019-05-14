// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TwinkelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_TwinkelSizeMax("TwinkelSizeMax", Float) = 1.25
		_TwinkelSizeMin("TwinkelSizeMin", Float) = 1
		_Value("Value", Float) = 1
    }
    SubShader
    { 
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
				UNITY_DEFINE_INSTANCED_PROP(float, _TwinkelSizeMax)
				UNITY_DEFINE_INSTANCED_PROP(float, _TwinkelSizeMin)
				UNITY_DEFINE_INSTANCED_PROP(float, _Value)
			UNITY_INSTANCING_BUFFER_END(Props)

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float TwinkelMin = UNITY_ACCESS_INSTANCED_PROP(Props, _TwinkelSizeMin);
				float TwinkelMax = UNITY_ACCESS_INSTANCED_PROP(Props, _TwinkelSizeMax);
				float TwinkelOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _Value);

                o.vertex = UnityObjectToClipPos(v.vertex.xyz * (TwinkelMin + ((TwinkelOffset)* (TwinkelMax - TwinkelMin))));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            { 
				UNITY_SETUP_INSTANCE_ID(i);
				fixed4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				
				fixed4 col = tex2D(_MainTex, i.uv) * albedo * 1.3f;
				
                return col;
            }
            ENDCG
        }
    }
}

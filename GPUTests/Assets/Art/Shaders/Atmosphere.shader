Shader "OSVR/Atmosphere"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Red("redding", Color) = (1,0,0,1)
		_Strength("fresnel strength", Float) = 1
		_RedStrength("redding strength", Float) = 1
    }
    SubShader
    { 
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf SimpleLambert alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0


        struct Input
        {
            float2 uv_MainTex;
			float3 worldNormal;
			float3 viewDir;
        };

        fixed4 _Color;
		fixed4 _Red;
		float _Strength;
		float _RedStrength;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half atten)
		{
			half NdotL = dot(s.Normal, lightDir);
			half4 c;
			c.rgb = lerp(s.Albedo,_Red.rgb,pow(1-NdotL,_RedStrength))  * saturate(NdotL * atten);
			c.a = s.Alpha;
			return c;
		}
        void surf (Input IN, inout SurfaceOutput o)
        {
			float fresnel = 1 - saturate(dot( IN.viewDir, IN.worldNormal));
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Alpha = pow(fresnel, _Strength);
        }
        ENDCG
    }
    FallBack "Diffuse"
}

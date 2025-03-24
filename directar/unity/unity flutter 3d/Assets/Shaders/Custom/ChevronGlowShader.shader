Shader "Custom/ChevronGlowShader"
{
    Properties
    {
        _MainTex ("Chevron Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 1, 0, 0.5) // Green Glow
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1
        _FresnelPower ("Fresnel Power", Range(0, 3)) = 1.2
        _ScrollSpeed ("Scroll Speed", Range(-5, 5)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One  // Additive blending for glow effect
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _GlowColor;
            float _GlowIntensity;
            float _FresnelPower;
            float _ScrollSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // ✅ Ensure chevron texture scrolls properly
                o.uv = v.uv;
                o.uv.x -= _Time.y * _ScrollSpeed;

                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // ✅ Improve Fresnel glow effect
                float fresnel = pow(1.0 - saturate(dot(i.viewDir, i.normalDir)), _FresnelPower);
                float4 glow = _GlowColor * fresnel * _GlowIntensity;
                
                return texColor + glow;
            }
            ENDCG
        }
    }
}

Shader "Custom/GlowShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normalDir : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float4 _Color;
            float4 _GlowColor;
            float _GlowIntensity;
            float _FresnelPower;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fresnel glow effect
                float fresnel = pow(1.0 - saturate(dot(i.viewDir, i.normalDir)), _FresnelPower);
                float4 glow = _GlowColor * fresnel * _GlowIntensity;
                
                return _Color + glow;
            }
            ENDCG
        }
    }
}

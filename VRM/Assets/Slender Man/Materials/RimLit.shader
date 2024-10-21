Shader "Custom/RimLitShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range (0.5, 8.0)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RimColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDir = ObjSpaceViewDir(v.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Sample texture and apply color
                half4 tex = tex2D(_MainTex, i.uv) * _Color;

                // Calculate rim lighting
                float rim = 1.0 - saturate(dot(normalize(i.viewDir), normalize(i.viewDir)));
                rim = pow(rim, _RimPower);

                // Apply rim lighting
                return tex + _RimColor * rim;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

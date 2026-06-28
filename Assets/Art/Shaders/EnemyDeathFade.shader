Shader "MidnightMeow/EnemyDeathFade"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _FadeAmount ("Fade Amount", Range(0, 1)) = 0
        _EdgeColor ("Edge Color", Color) = (0.85, 0.95, 1, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 4)) = 1.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FadeAmount;
            fixed4 _EdgeColor;
            float _EdgeIntensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                if (col.a < 0.01)
                    discard;

                float fade = saturate(_FadeAmount);
                float rim = smoothstep(fade + 0.04, fade, 1.0);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb * _EdgeIntensity, rim * 0.35);
                col.a *= 1.0 - fade;
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

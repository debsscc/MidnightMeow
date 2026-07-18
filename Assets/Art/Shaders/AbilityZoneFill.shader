Shader "MidnightMeow/AbilityZoneFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _FillColor ("Fill", Color) = (0.2, 0.75, 1, 0.45)
        _OutlineColor ("Outline", Color) = (0.9, 0.95, 1, 0.95)
        _OutlineWidth ("Outline Width", Range(0.001, 0.2)) = 0.05
        _Shape ("Shape 0=Circle 1=Rect", Float) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Pulse ("Pulse", Range(0, 1)) = 0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 1
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _FillColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _Shape;
            float _Alpha;
            float _Pulse;
            float _PulseStrength;

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
                float2 uv = i.texcoord;
                float pulseWave = 0.85 + 0.15 * sin(_Time.y * 8.0 + _Pulse * 6.28318);
                float pulse = lerp(1.0, pulseWave, saturate(_PulseStrength));

                if (_Shape < 0.5)
                {
                    float dist = length(uv - 0.5) * 2.0;
                    if (dist > 1.0)
                        discard;

                    if (dist >= 1.0 - _OutlineWidth)
                    {
                        fixed4 c = _OutlineColor;
                        c.a *= _Alpha * pulse;
                        return c;
                    }

                    fixed4 c = _FillColor;
                    c.a *= _Alpha * pulse * (1.0 - dist * 0.35);
                    return c;
                }

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    discard;

                float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                if (edge <= _OutlineWidth)
                {
                    fixed4 c = _OutlineColor;
                    c.a *= _Alpha * pulse;
                    return c;
                }

                fixed4 fill = _FillColor;
                fill.a *= _Alpha * pulse;
                return fill;
            }
            ENDCG
        }
    }
}

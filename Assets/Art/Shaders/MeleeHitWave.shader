Shader "MidnightMeow/MeleeHitWave"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _FillColor ("Fill", Color) = (1, 0.4, 0.1, 0.3)
        _WaveEdgeColor ("Wave Edge", Color) = (1, 0.9, 0.3, 0.95)
        _OutlineColor ("Outline", Color) = (1, 0.7, 0.2, 0.6)
        _OutlineWidth ("Outline Width", Range(0.001, 0.2)) = 0.04
        _WaveProgress ("Wave Progress", Range(0, 1)) = 0
        _WaveEdgeWidth ("Wave Edge Width", Range(0.01, 0.3)) = 0.08
        _NearHalfWidth ("Near Half Width", Range(0.01, 1)) = 0.35
        _FarHalfWidth ("Far Half Width", Range(0.01, 1.5)) = 1.1
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            fixed4 _WaveEdgeColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _WaveProgress;
            float _WaveEdgeWidth;
            float _NearHalfWidth;
            float _FarHalfWidth;
            float _Alpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float TrapezoidHalfWidth(float depth01)
            {
                return lerp(_NearHalfWidth, _FarHalfWidth, saturate(depth01));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float depth = uv.y;
                float lateral = abs(uv.x - 0.5) * 2.0;

                if (depth < 0.0 || depth > 1.0)
                    discard;

                float halfWidth = TrapezoidHalfWidth(depth);
                if (lateral > halfWidth)
                    discard;

                float edge = min(min(uv.x, 1.0 - uv.x) * 2.0, min(depth, 1.0 - depth));
                if (edge <= _OutlineWidth)
                {
                    fixed4 outline = _OutlineColor;
                    outline.a *= _Alpha;
                    return outline;
                }

                float waveFront = saturate(_WaveProgress);
                float waveBand = _WaveEdgeWidth;
                float distToWave = abs(depth - waveFront);

                if (depth <= waveFront + waveBand * 0.5)
                {
                    float waveMix = 1.0 - saturate(distToWave / max(waveBand, 0.001));
                    fixed4 wave = _WaveEdgeColor;
                    wave.a *= _Alpha * (0.35 + waveMix * 0.65);
                    if (waveMix > 0.55)
                        return wave;
                }

                if (depth > waveFront)
                    discard;

                float trail = 1.0 - depth / max(waveFront, 0.001);
                fixed4 fill = _FillColor;
                fill.a *= _Alpha * (0.25 + trail * 0.55);
                return fill;
            }
            ENDCG
        }
    }
}

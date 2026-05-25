Shader "MidnightMeow/TelegraphFill"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _BackgroundColor ("Interior (amarelo)", Color) = (1, 0.92, 0.22, 0.55)
        _FillColor ("Preenchimento (vermelho)", Color) = (0.9, 0.12, 0.08, 0.85)
        _OutlineColor ("Borda (vermelho)", Color) = (0.95, 0.15, 0.1, 1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _OutlineWidth ("Outline Width", Range(0.001, 0.2)) = 0.06
        _Shape ("Shape 0=Circle 1=Rect", Float) = 0
        _FillMode ("Fill Mode", Float) = 0
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
            fixed4 _BackgroundColor;
            fixed4 _FillColor;
            fixed4 _OutlineColor;
            float _FillAmount;
            float _OutlineWidth;
            float _Shape;
            float _FillMode;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float circleDist(float2 uv)
            {
                return length(uv - 0.5) * 2.0;
            }

            float rectFillMetric(float2 uv)
            {
                float along = uv.y;
                if (_FillMode > 0.5)
                    along = 1.0 - along;
                return along;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float fillMetric;

                if (_Shape < 0.5)
                {
                    fillMetric = circleDist(uv);
                    if (fillMetric > 1.0)
                        discard;

                    if (fillMetric >= 1.0 - _OutlineWidth)
                        return _OutlineColor;

                    if (fillMetric <= _FillAmount)
                        return _FillColor;

                    return _BackgroundColor;
                }

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    discard;

                fillMetric = rectFillMetric(uv);

                if (fillMetric >= 1.0 - _OutlineWidth || fillMetric <= _OutlineWidth)
                    return _OutlineColor;

                if (fillMetric <= _FillAmount)
                    return _FillColor;

                return _BackgroundColor;
            }
            ENDCG
        }
    }
}

Shader "Custom/DissolveSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.08
        _EdgeColor ("Edge Color", Color) = (0.85, 0.95, 1, 1)
        _EdgeIntensity ("Edge Intensity", Range(1, 8)) = 3.5
        _SparkleIntensity ("Sparkle Intensity", Range(0, 6)) = 2
        _SparkleScale ("Sparkle Scale", Float) = 40
        _NoiseScale ("Noise Scale", Float) = 5.0
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            float     _DissolveAmount;
            float     _EdgeWidth;
            fixed4    _EdgeColor;
            float     _EdgeIntensity;
            float     _SparkleIntensity;
            float     _SparkleScale;
            float     _NoiseScale;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                float n = lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);

                float2 uv2 = uv * 2.1;
                float2 i2  = floor(uv2);
                float2 f2  = frac(uv2);
                float2 u2  = f2 * f2 * (3.0 - 2.0 * f2);
                float  a2  = hash(i2);
                float  b2  = hash(i2 + float2(1, 0));
                float  c2  = hash(i2 + float2(0, 1));
                float  d2  = hash(i2 + float2(1, 1));
                n += lerp(lerp(a2, b2, u2.x), lerp(c2, d2, u2.x), u2.y) * 0.5;

                return n / 1.5;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color    = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord) * IN.color;
                if (texColor.a < 0.01)
                    discard;

                float n = noise(IN.texcoord * _NoiseScale);
                clip(n - _DissolveAmount);

                float edgeBand = smoothstep(_DissolveAmount, _DissolveAmount + _EdgeWidth, n);
                fixed4 edgeColor = _EdgeColor * _EdgeIntensity;
                edgeColor.a *= texColor.a;

                float edgeProximity = 1.0 - saturate((n - _DissolveAmount) / max(_EdgeWidth, 0.001));
                float sparkleCell = hash(floor(IN.texcoord * _SparkleScale + float2(_DissolveAmount * 12.0, 0.0)));
                float sparkle = step(0.82, sparkleCell) * edgeProximity * _SparkleIntensity;
                edgeColor.rgb += sparkle;

                fixed4 finalColor = lerp(edgeColor, texColor, edgeBand);
                finalColor.a = texColor.a * step(_DissolveAmount, n);

                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

Shader "Weike/JIXRYBannerShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1, 0, 0, 1)
        _ColorB ("Color B", Color) = (0, 0, 1, 1)
        _ColorC ("Color C", Color) = (0, 1, 0, 1)
        _ColorCount ("Color Count (2-3)", Float) = 3
        _Speed  ("Wave Speed", Float) = -0.1
        _Repeat ("Repeat Count", Float) = 0.25
        _DistanceMask ("Distance Mask", Float) = 7
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Shader/Utils/Blend/VividLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Material properties
            fixed4 _ColorA;
            fixed4 _ColorB;
            fixed4 _ColorC;
            float  _ColorCount;  // 2 or 3
            float  _Speed;       // scroll speed
            float  _Repeat;      // number of bars visible across width
            float  _DistanceMask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Clamp color count to [1..3]
                int count = (int)clamp(_ColorCount, 1.0, 3.0);

                // Colors
                fixed3 cA = _ColorA.rgb;
                fixed3 cB = _ColorB.rgb;
                fixed3 cC = _ColorC.rgb;
                fixed3 colors[3] = { cA, cB, cC };

                // Scroll parameter
                float t = frac(i.uv.x * max(_Repeat, 0.0001) + _Time.y * _Speed);

                // Segment index [0..count-1] and local progress [0..1]
                float segF = floor(t * count);
                float segT = frac(t * count);

                fixed3 waveCol;

                if (count == 1)
                {
                    // --- Single color: scrolling effect, no over-bright ---
                    waveCol = cA;
                }
                else if (count == 2)
                {
                    // --- Two colors: pure linear, balanced fade ---
                    int segIndex  = (int)segF;
                    int nextIndex = (segIndex + 1) % 2;

                    float3 startLin = GammaToLinearSpace(colors[segIndex]);
                    float3 endLin   = GammaToLinearSpace(colors[nextIndex]);
                    float3 waveLin  = lerp(startLin, endLin, segT);
                    waveCol         = LinearToGammaSpace(waveLin);
                }
                else // count == 3
                {
                    // --- Three colors: smootherstep easing, no artificial blendWidth ---
                    int segIndex  = (int)segF;
                    int nextIndex = (segIndex + 1) % 3;

                    float w = segT * segT * segT * (segT * (segT * 6 - 15) + 10); // smootherstep

                    float3 startLin = GammaToLinearSpace(colors[segIndex]);
                    float3 endLin   = GammaToLinearSpace(colors[nextIndex]);
                    float3 waveLin  = lerp(startLin, endLin, w);
                    waveCol         = LinearToGammaSpace(waveLin);
                }

                // Blend with the texture using vivid light
                fixed3 vivid = VividLight(tex, fixed4(waveCol, 1.0));

                // Radial alpha mask
                float d = length(i.uv - float2(0.5, 0.5));
                d = smoothstep(_DistanceMask, 0.0, d);

                return fixed4(vivid, tex.a * d);
            }
            ENDCG
        }
    }
}
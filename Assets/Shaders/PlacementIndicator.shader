Shader "KingdomWar/PlacementIndicator"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.2, 0.6, 1.0, 0.3)
        _BorderColor ("Border Color", Color) = (0.5, 0.8, 1.0, 0.8)
        _BorderWidth ("Border Width", Range(0.01, 0.3)) = 0.05
        _FadeDistance ("Fade Distance", Range(0.0, 1.0)) = 0.3
        [Toggle] _IsValid ("Is Valid Placement", Float) = 1
        _InvalidColor ("Invalid Color", Color) = (1.0, 0.2, 0.2, 0.3)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _MainColor;
            fixed4 _BorderColor;
            float _BorderWidth;
            float _FadeDistance;
            float _IsValid;
            fixed4 _InvalidColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center-based UV: (0,0) at center, extends to edges
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float dist = length(centeredUV);

                // Circle mask
                float circle = 1.0 - smoothstep(0.85, 1.0, dist);

                // Border ring
                float border = smoothstep(0.85 - _BorderWidth, 0.85, dist) * (1.0 - smoothstep(0.85, 0.85 + _BorderWidth, dist));
                
                // Inner area to fade
                float innerFade = smoothstep(0.0, _FadeDistance, dist);
                float innerMask = circle * innerFade;

                // Choose color based on validity
                fixed4 areaColor = _IsValid ? _MainColor : _InvalidColor;
                fixed4 borderFinal = _IsValid ? _BorderColor : _InvalidColor;
                borderFinal.a = _BorderColor.a;

                // Combine: border on top of inner area
                fixed4 finalColor = areaColor * innerMask + borderFinal * border;
                
                // Grid lines (subtle)
                float gridX = sin(centeredUV.x * 12.0) * 0.5 + 0.5;
                float gridY = sin(centeredUV.y * 12.0) * 0.5 + 0.5;
                float gridLine = (gridX < 0.08 || gridY < 0.08) ? 0.15 : 0.0;
                finalColor += areaColor * gridLine * (1.0 - dist);

                // Pulsing border animation hint (time-based)
                float pulse = sin(_Time.y * 2.0) * 0.15 + 0.85;
                finalColor.a *= pulse;

                return finalColor;
            }
            ENDCG
        }
    }
}

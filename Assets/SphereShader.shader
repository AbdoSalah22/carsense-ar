Shader "Custom/GlowingPulsingSphere" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0.5,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0, 15)) = 1.0
        _PulseMinimum ("Pulse Minimum", Range(0, 1)) = 0.2
        _PulseMax ("Pulse Maximum", Range(1, 3)) = 1.5
    }
    
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseMinimum;
            float _PulseMax;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Calculate fresnel effect (stronger glow on edges)
                float fresnel = pow(1.0 - saturate(dot(normalize(i.normal), normalize(i.viewDir))), 3.0);
                
                // Calculate pulsing effect - enhanced sin wave pattern
                // Maps sin from [-1,1] to [_PulseMinimum, _PulseMax]
                float pulse = lerp(_PulseMinimum, _PulseMax, (sin(_Time.y * _PulseSpeed) * 0.5) + 0.5);
                
                // Apply glow with pulsing effect
                float3 glow = _GlowColor.rgb * _GlowIntensity * fresnel * pulse;
                
                // Make core also glow with pulsing
                float innerGlow = pulse * 0.5; // Reduced inner glow
                col.rgb = lerp(col.rgb, _GlowColor.rgb, innerGlow);
                
                // Combine base color with edge glow
                col.rgb += glow;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
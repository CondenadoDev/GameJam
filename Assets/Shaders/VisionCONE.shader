Shader "Custom/VisionCone"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,0.3)
        _EdgeFade ("Edge Fade", Range(0,1)) = 0.5
        _CenterIntensity ("Center Intensity", Range(0,1)) = 0.8
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            fixed4 _Color;
            float _EdgeFade;
            float _CenterIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calcular distancia desde el centro
                float2 centerUV = float2(0.5, 0.5);
                float distFromCenter = distance(i.uv, centerUV) * 2.0;
                
                // Calcular fade desde el borde
                float edgeFade = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, distFromCenter);
                
                // Intensidad basada en la distancia al centro
                float intensity = lerp(_CenterIntensity, 1.0, distFromCenter);
                
                // Color final
                fixed4 col = _Color;
                col.a *= edgeFade * intensity;
                
                // Aplicar fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
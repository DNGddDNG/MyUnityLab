Shader "Unlit/ShenweiShader"
{
    Properties
    {
        _CenterHeight("CenterHeight",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Cull Off
            
            HLSLPROGRAM
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
                float4 worldPos:TEXCOORD1;
            };

            float _CenterHeight;
            
            v2f vert (appdata v)
            {
                v2f o;
                float4 worldCenterPos=mul(unity_ObjectToWorld,v.vertex);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex);
                o.worldPos.y=lerp(0,_CenterHeight,abs(distance(v.vertex.xz,float2(0,0)))/0.5);
                o.vertex = mul(unity_ObjectToWorld,o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(i.vertex.yyy,0);
            }
            ENDHLSL
        }
    }
}

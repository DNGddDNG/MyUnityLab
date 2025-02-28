Shader "SimpleLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("BaseColor",Color)=(1,0.7,0.6,1)
        _RimColor("RimColor",Color)=(1,1,1,1)
        _EdgePower("EdgePower",Range(0,1))=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal:NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos:TEXCOORD1;
                float3 worldNormal:TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RimColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex);
                o.worldNormal=normalize(UnityObjectToWorldNormal(v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 visionVec=normalize(UnityWorldSpaceViewDir(i.worldPos));
                float strength=1-saturate(dot(visionVec,i.worldNormal));
                
                half4 col=_RimColor*strength+_Color*(1-strength);
                return col;
            }
            ENDHLSL
        }
    }
}

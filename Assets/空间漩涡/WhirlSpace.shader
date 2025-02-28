Shader "Unlit/WhirlSpace"
{
    Properties
    {
        _Center("Center",Vector)=(0,0,0)
        _ConstNum("G(M+m)",Float)=0
        _T("time",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldPos:TEXCOORD0;
            };

            float3 _Center;
            float _ConstNum;
            float _T;

            float4 Trans(float4 worldPos)
            {
                float dis=distance(worldPos,_Center);
                float3 vec=normalize(worldPos-_Center);
                float newDis=dis*(1
                    -_ConstNum*_T*_T/(2*pow(dis,3)
                        -_ConstNum*_ConstNum*pow(_T,4)/(12*pow(dis,6))));
                float4 newPos=float4(_Center+newDis*vec,1);
                return newPos;
                
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos=mul(unity_ObjectToWorld,v.vertex);
                o.worldPos=Trans(o.worldPos);
                o.vertex=UnityWorldToClipPos(o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(i.worldPos.y-_Center.y);
                return float4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}

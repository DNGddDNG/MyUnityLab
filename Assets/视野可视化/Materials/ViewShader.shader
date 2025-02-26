Shader "Unlit/ViewShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry+1"
        }

        Pass
        {
            ZWrite Off
            ZTest Off
            
            
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
                float4 screenPos:TEXCOORD0;
            };
            
            float3 viewCenter;
            float viewRadius;
            sampler2D _CameraDepthTexture;

            float3 DepthToWorldPosition(float4 screenPos)
            {
                float4 depth=Linear01Depth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,screenPos)));
                float4 ndcPos=(screenPos/screenPos.w)*2-1;
                float3 clipPos=float3(ndcPos.x,ndcPos.y,1)*_ProjectionParams.z;

                float3 viewPos=mul(unity_CameraInvProjection,clipPos.xyzz).xyz*depth;
                float3 worldPos=mul(UNITY_MATRIX_I_V,float4(viewPos,1)).xyz;
                return worldPos;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos=ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos=DepthToWorldPosition(i.screenPos);
                float4 localPos=mul(unity_WorldToObject,float4(worldPos,1));
                clip(float3(0.5,0.5,0.5)-abs(localPos.xyz));

                return float4(1,1,0,1);
            }
            ENDHLSL
        }

    }
    
}

Shader "Unlit/ViewShader"
{
    Properties
    {
        _Color("Color",Color)=(1,1,1,1)
// 视点中心
    viewCenter("View Center", Vector) = (0, 0, 0, 0)
    // 视点半径
    viewRadius("View Radius", Float) = 10.0
    // 视角
    viewAngle("View Angle", Float) = 45.0
    // 视点朝向
    viewForward("View Forward", Vector) = (0, 0, 1, 0)
    // 深度纹理
    _depthTex("Depth Texture", 2D) = "white" {}
    // 远近平面
    far("Far Clip", Float) = 1000.0
    // 近平面
    near("Near Clip", Float) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Off
            ZClip Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos:TEXCOORD0;
                float3 worldNormal:TEXCOORD1;
            };
            
            float3 viewCenter;
            float viewRadius;
            float viewAngle;
            float3 viewForward;
            sampler2D _depthTex;
            float4x4 depthViewMatrix;
            float4x4 depthProjMatrix;
            float4x4 invDepthProjMatrix;
            float far;
            float near;
            
            float4 _Color;
            sampler2D _CameraDepthTexture;

            float4 LinearEyeDepthByDepthMatrix(float depth)
            {
                #ifdef UNITY_REVERSED_Z
                    float x = -1+far/near;
                    float y = 1;
                    float z = x/far;
                    float w = 1/far;
                #else
                    float x = 1-far/near;
                    float y = far/near;
                    float z = x/far;
                    float w = y/far;
                #endif
            
                return 1.0 / (z * depth + w);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos=ComputeScreenPos(o.vertex);
                o.worldNormal=UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float3 DepthToWorldPosition(float4 screenPos)
            {
                float depth=Linear01Depth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,screenPos)));
                float4 ndcPos=(screenPos/screenPos.w)*2-1;
                float3 clipPos=float3(ndcPos.x,ndcPos.y,1)*_ProjectionParams.z;
                
                float3 viewPos=mul(unity_CameraInvProjection,clipPos.xyzz).xyz*depth;
                float3 worldPos=mul(UNITY_MATRIX_I_V,float4(viewPos,1)).xyz;
                return worldPos;
            }

            float2 GetUVFromWorldPos(float4 clipPos)
            {
                //float4 clipPos=mul(depthProjMatrix,viewPos);
                float4 screenPos=ComputeScreenPos(clipPos);
                float2 uv=(screenPos/screenPos.w).xy;
                return uv;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                //贴花剔除
                float3 worldPos=DepthToWorldPosition(i.screenPos);
                float4 localPos=mul(unity_WorldToObject,float4(worldPos,1));
                clip(float3(0.5,0.5,0.5)-abs(localPos.xyz));

                //剔除自身周围一定范围内
                float worldRadius=distance(worldPos.xz,viewCenter.xz);
                clip(worldRadius-0.5);

                //剔除半径外
                float xzDistance=distance(worldPos.xz,viewCenter.xz);
                clip(viewRadius-xzDistance);

                //剔除角度外
                float2 xzWorldVec=normalize(worldPos.xz-viewCenter.xz);
                float xzAngle=degrees(acos(dot(xzWorldVec,viewForward.xz)));
                clip(viewAngle-xzAngle*2);

                //剔除垂直面
                
                float4 viewPos=mul(depthViewMatrix,float4(worldPos,1));
                //剔除视锥体外
                float4 clipPos=mul(depthProjMatrix,viewPos);
                float3 ndcPos=clipPos.xyz/clipPos.w;
                clip(ndcPos+float3(1,1,1));
                clip(float3(1,1,1)-ndcPos);
                
                //剔除被遮挡
                float2 uv=GetUVFromWorldPos(clipPos);
                float depth=UNITY_SAMPLE_DEPTH(tex2D(_depthTex,uv));
                float actualDepth=-viewPos.z;
                float texDepth=LinearEyeDepthByDepthMatrix(depth);
                clip(texDepth-actualDepth);

                float4 col=_Color;
                return col;
            }
            ENDHLSL
        }

    }
    
}

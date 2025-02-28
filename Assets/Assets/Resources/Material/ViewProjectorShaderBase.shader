// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/ViewProjectorShaderBase"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        _v1Tex ("v1Texture", 2D) = "white" {}
        _v2Tex ("v2Texture", 2D) = "white" {}
        
        _Color("Color",Color)=(0,1,0,0.5)
        // 深度摄像机的投影参数 (通常是一个 `float4`，可以表示近平面、远平面等)
        _DepthCameraProjectionParams ("Depth Camera Projection Params", Vector) = (1, 1, 1, 1)

        // 深度纹理 (Sampler2D_float 表示浮点数深度纹理)
        _DepthTex ("Depth Texture", 2D) = "black" {}

        // 深度摄像机的视图矩阵
        //_DepthCameraViewMatrix ("Depth Camera View Matrix", ) = {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}

        // 深度摄像机的投影矩阵
        //_DepthCameraProjMatrix ("Depth Camera Projection Matrix", Matrix) = {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}

        // 深度摄像机的逆视投影矩阵
        //_DepthCameraMatrix_I_VP ("Depth Camera Inverse View-Projection Matrix", Matrix) = {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}
        
        // XZ 视图中心和方向 (float4)
        _xzViewCenterAndDirection ("XZ View Center and Direction", Vector) = (0, 0, 0, 0)

        // 视野角度
        _ViewAngle ("View Angle", Float) = 90.0

        // 视野半径
        _ViewRadius ("View Radius", Float) = 10.0

        // 警戒半径
        _AlarmRadius ("Alarm Radius", Float) = 5.0

        // 蹲下检测高度
        _CharCrouchingCheckHeight ("Character Crouching Check Height", Float) = 1.0

        // 站立检测高度
        _CharStandingCheckHeight ("Character Standing Check Height", Float) = 2.0
    
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            // 开启透明混合
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.0
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
                float4 vertex:POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos:TEXCOORD1;
                float3 normal:TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _DepthCameraProjectionParams;
            sampler2D_float _DepthTex;
            float4x4 _DepthCameraViewMatrix;
            float4x4 _DepthCameraProjMatrix;
            float4x4 _DepthCameraMatrix_I_VP;
            fixed4 _Color;
            float4 _xzViewCenterAndDirection;
            float _ViewAngle;
            float _ViewRadius;
            float _AlarmRadius;
            fixed _CharCrouchingCheckHeight;
            fixed _CharStandingCheckHeight;

            sampler2D _v1Tex;
            sampler2D _v2Tex;

            float LinearDepth(float nonLinearDepth)
            {
                float zNear = _DepthCameraProjectionParams.x;  // 近平面
                float zFar = _DepthCameraProjectionParams.y;   // 远平面

                float x = 1 - zFar/zNear;
                float y = 1;//zFar/zNear;
                float z = x/zFar;
                float w = y/zFar;
                
                return 1.0 / (z * nonLinearDepth + w);
                //return 1.0/((1-zFar/zNear)*nonLinearDepth+zFar/zNear);
            }
            
            float2 getDepthTexUVByWorldPos(float4 worldPos)
            {
                float4 clipPos = mul(_DepthCameraProjMatrix,mul(_DepthCameraViewMatrix,worldPos));
                float4 ndcPos = clipPos / clipPos.w;
                float2 uvPos = ndcPos.xy * 0.5 + 0.5;
                return uvPos;
            }

            float getDepthValueByDepthTex(float4 worldPos)
            {
                float2 depthUV = getDepthTexUVByWorldPos(worldPos).xy;   
                float depth = SAMPLE_DEPTH_TEXTURE(_DepthTex,depthUV);
                depth = LinearDepth(depth);
                return depth;
            }
            
            float getDepthValueByDepthTex2(float4 worldPos)
            {
                const float2 depthUV = getDepthTexUVByWorldPos(worldPos).xy;   
                float depth = SAMPLE_DEPTH_TEXTURE(_DepthTex,depthUV);
                return depth;
            }
            
            
            // float getDepthValueByWorldPos(float4 worldPos)
            // {
            //     float depth = getDepthTexUVByWorldPos(worldPos).z;
            //     depth = Linear01Depth(depth);
            //     return depth;
            // }
            //
            // float getDepthValueByWorldPos2(float4 worldPos)
            // {
            //     float depth = getDepthTexUVByWorldPos(worldPos).z;
            //     return depth;
            // }

            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos=mul(unity_ObjectToWorld,float4(v.vertex.xyz, 1));
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //剔除非平面(法线夹角在20度以内)
                float3 normal = normalize(i.normal);
                float3 up = float3(0.0, 1.0, 0.0);
                float angle = acos(dot(normal, up));
                clip(20-degrees(angle));
                //裁去圆外
                float2 xzViewCenter=_xzViewCenterAndDirection.xy;
                clip(_ViewRadius-length(i.worldPos.xz-xzViewCenter));
                //裁去角度外
                float2 xzViewDirection=_xzViewCenterAndDirection.zw;
                float2 xzDirToPixel=normalize(i.worldPos.xz-xzViewCenter);
                float halfAngle=degrees(acos(dot(xzDirToPixel,xzViewDirection)));
                clip(_ViewAngle-2*halfAngle);

                //上面没有问题
                //剔除站着也被挡的
                float4 standWorldPos=i.worldPos+float4(0,_CharStandingCheckHeight,0,0);
                
                // depth value from depth texture 
                float v1 = LinearDepth(getDepthValueByDepthTex2(standWorldPos));  
                
                float4 clipPos = mul(_DepthCameraProjMatrix,mul(_DepthCameraViewMatrix,standWorldPos));
                
                // depth value from world pos
                float v2 = 1-clipPos.z/clipPos.w;//getDepthValueByWorldPos2(standWorldPos);
                
                clip(v1 - v2);
                
                fixed4 col = _Color;
                return float4(v1, v2, 0, 1);
            }
            ENDHLSL
        }
    }
}

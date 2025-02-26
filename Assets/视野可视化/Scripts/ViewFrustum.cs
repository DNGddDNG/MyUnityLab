using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Serialization;

public class ViewFrustum : MonoBehaviour
{
    public float viewRadius;
    public float viewAngle;
    public float viewHeight;

    public int viewPixelWidth;
    public int viewPixelHeight;
    
    public LayerMask viewCullingMask;
    [SerializeField]
    private Camera depthCamera;
    [SerializeField]
    private RenderTexture depthTexture;
    private Renderer viewRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private void Awake()
    {
        if(!TryGetComponent<Camera>(out depthCamera)){Debug.LogError("未能正常获取深度相机");}
        if(!TryGetComponent<Renderer>(out viewRenderer)){Debug.LogError("未能正常获取renderer");}
        materialPropertyBlock = new MaterialPropertyBlock();
        CreateDepthTexture();
        SetCamera();
    }
    void Update()
    {
        transform.localScale=new Vector3(viewRadius*2,viewHeight*2,viewRadius*2);
        materialPropertyBlock.SetVector("viewCenter", transform.position);
        materialPropertyBlock.SetFloat("viewRadius", viewRadius);
        viewRenderer.SetPropertyBlock( materialPropertyBlock);
    }

    void CreateDepthTexture()
    {
        RenderTextureDescriptor depthTexDesc = new RenderTextureDescriptor();
        depthTexDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        depthTexDesc.width = viewPixelWidth;
        depthTexDesc.height = viewPixelHeight;
        depthTexDesc.volumeDepth = 1;
        depthTexDesc.colorFormat = RenderTextureFormat.Depth;
        depthTexDesc.depthBufferBits = 32;
        depthTexDesc.msaaSamples = 1;//禁用MSAA
        depthTexture = new RenderTexture(depthTexDesc);
        depthTexture.Create();
    }
    void SetCamera()
    {
        depthCamera.clearFlags = CameraClearFlags.Depth;
        depthCamera.cullingMask = viewCullingMask.value;
        depthCamera.targetTexture = depthTexture;
        depthCamera.aspect = viewPixelWidth / (float)viewPixelHeight;
        float horizontalAngle = viewAngle;  // 例如设定为90度
        float aspectRatio = depthCamera.aspect;
        float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(horizontalAngle * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
        depthCamera.fieldOfView = verticalFOV;
        depthCamera.farClipPlane = viewRadius;
        depthCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }
}

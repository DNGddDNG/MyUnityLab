using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
/*
角色进入警觉半径时，触发警觉，可视化范围显现
角色在视锥半径内，警觉范围外进行除潜行外的动作时触发警觉，可视化范围显现

高处敌人视锥可以覆盖低处，低处看高处相当于警觉范围外，视锥半径内
 */


public class ViewFrustumForEnemy : MonoBehaviour
{
    public Vector3 viewCenter;
    [Range(0, 180)]
    public float viewAngle = 90f;//视锥角度
    public float viewRadius = 10f;//视锥半径
    public float alarmRadius = 7f;//警觉半径
    public float viewColliderHeight = 3f;//视锥上下高度的一半；应当使敌人可以看见上一层楼，无法看见上第二层楼

    public LayerMask obstacleMask;//障碍物的层级
    public LayerMask targetMask;//可发现目标的层级

    private EnemyAI _enemyAI;//控制enemy行为的组件
    public float charCrouchingCheckHeight = 5;//角色潜行时的检测高度
    public float charStandingCheckRadius = 10;//其他检测高度

    private CapsuleCollider _viewCollider;//视野范围，进入范围后打开可视化
    public int depthTexWidth = 256;
    public int depthTexHeight = 256;
    [HideInInspector]
    public bool visualizationEnable;//可视化开关
    [HideInInspector]
    public Camera depthCamera;//获取敌人视野深度图的相机
    [SerializeField]
    private RenderTexture depthTex;//用来初始化viewProjector
    //public Camera heightCamera;//获取周围高度图（其实也是深度图）的camera
    public Projector viewProjector;//可视化的投影器
    //[SerializeField]
    //private RenderTexture heightTex;

    public List<GameObject> targetInSight;//视野内发现的目标
    public float alarmSearchSpeed = 1f;//发现目标后，警觉速度

    [SerializeField]
    public GameObject targetLocked;//锁定的目标 
    private void Awake()
    {
        viewCenter = transform.position; 
        if (!gameObject.TryGetComponent(out _enemyAI))
        {
            if (!transform.parent.TryGetComponent(out _enemyAI))
            {
                if ((_enemyAI = transform.parent.GetComponentInChildren<EnemyAI>()) == null)
                {
                    Debug.Log("缺少EnemyAI脚本");
                }
            }
        }
        CreateViewCollider();
        SetDepthCameraAndTexture();
        InitViewProjector();
        visualizationEnable = false;
    }
    private void Start()
    {
        //开启alarm计时器
        StartCoroutine("AlarmDetectTimer");
    }

    [HideInInspector]
    public List<GameObject> targetInViewCollider;
    private void OnTriggerEnter(Collider obj)
    {
        if (CheckTargetLayer(obj.gameObject, targetMask))
        {
            //开启可视化
            visualizationEnable = true;

            Debug.Log($"{obj.gameObject.name}进入{gameObject.name}的ViewCollider");
            targetInViewCollider.Add(obj.gameObject);
        }
    }
    private void OnTriggerStay(Collider obj)
    {
        Debug.Log($"{obj.gameObject.name}位于{gameObject.name}的ViewCollider中");

        //若发现target(用layer判断)
        if (CheckTargetLayer(obj.gameObject, targetMask))
        {
            //判断是否能看到
            if (IsInSight(obj.gameObject))
            {
                //开启可视化
                visualizationEnable = true;

                //如果视野内没有此目标
                if (!targetInSight.Contains(obj.gameObject))
                {
                    targetInSight.Add(obj.gameObject);//视野中目标队列中加入此目标

                    //如果状态优先级低于巡逻状态，开始警觉
                    if (_enemyAI?.state < EnemyState.alarm)
                    {
                        Debug.Log($"发现目标{obj.gameObject.name}，进入警觉状态");
                        _enemyAI.state = EnemyState.alarm;//状态切换到alarm（记得切换回去）
                        Debug.Log($"{gameObject.name}状态变为{_enemyAI.state}");
                    }
                }
            }
            else
            {
                if (targetInSight.Contains(obj.gameObject))
                {
                    targetInSight.Remove(obj.gameObject);
                    Debug.Log($"目标{obj.gameObject.name}离开视野，但仍处于ViewCollider中");
                }
            }
        }
    }
    private void OnTriggerExit(Collider obj)
    {
        if (targetInSight.Contains(obj.gameObject))
        {
            targetInSight.Remove(obj.gameObject);
            Debug.Log($"目标{obj.gameObject.name}离开视野,离开ViewCollider");
        }
        //如果是target离开，关闭可视化
        if (CheckTargetLayer(obj.gameObject, targetMask))
        {
            targetInViewCollider.Remove(obj.gameObject);
            //如果范围内不再有target
            if (targetInViewCollider.Count == 0)
            {
                Debug.Log($"开启协程关闭{gameObject.name}视野范围可视化");
                float waitTime = 1f;
                Debug.Log($"恢复平静{waitTime}秒后关闭可视化");
                StartCoroutine("DisableVisualization", waitTime);
            }
        }
        //离开范围一秒后关闭可视化
    }
    private void Update()
    {
        switch (_enemyAI?.state)
        {
            case EnemyState.standby:
                break;
            case EnemyState.alarm:
                break;
            case EnemyState.CombatState:
                break;
        }
    }
    //警觉计时器，进入警觉状态后开始逐个排查视野内的target（将target的距离与time*alarmSearchSpeed）
    //同时检测视野中target数量，当数量降为0时，进入afterAlarm状态，timer开始减少，timer归零后，状态变为standby
    IEnumerator AlarmDetectTimer()
    {
        Debug.Log($"{gameObject.name}开启alarm计时器");

        float timer = 0f;
        while (true)
        {
            yield return new WaitUntil(() => (_enemyAI?.state == EnemyState.alarm));
            if (targetInSight.Count > 0)
            {
                timer += Time.deltaTime;
                float alarmSearchDistance = timer * alarmSearchSpeed;//当前探测的最远距离
                foreach (var target in targetInSight)
                {
                    Vector2 xzTargetPos = new Vector2(target.transform.position.x, target.transform.position.z);
                    Vector2 xzViewCenter = new Vector2(viewCenter.x, viewCenter.z);
                    float distanceToTarget = Vector2.Distance(xzTargetPos, xzViewCenter);
                    //如果探测到了目标
                    if(distanceToTarget < alarmSearchDistance)
                    {
                        Debug.Log($"timer={timer},alarmSearchDistance={alarmSearchDistance}\n{gameObject.name}发现{target.name},坐标为{target.transform.position}");

                        LockTarget(target);//更新锁定的目标*2//记得在EnemyAI中，离开CombatState状态后解锁目标
                        _enemyAI.state = EnemyState.CombatState;

                        Debug.Log($"{gameObject.name}锁定目标更新为{target.name},state更新为{_enemyAI.state}");
                        break;
                    }
                }
            }
            else
            {
                Debug.Log($"{gameObject.name}处于{_enemyAI?.state}状态，视野中target数量为0");
                timer -= Time.deltaTime;
                if(timer <= 0f) 
                { 
                    timer = 0f;
                    _enemyAI.state = EnemyState.standby;//归零后state回归到standby
                    Debug.Log($"timer归零，{gameObject.name}状态更新为{_enemyAI?.state}");
                }
            }
        }
    }

    //关闭可视化的协程
    IEnumerator DisableVisualization(float second)
    {
        yield return new WaitUntil(() => (_enemyAI.state <= EnemyState.standby));
        yield return new WaitForSeconds(second);
        visualizationEnable = false;
        Debug.Log($"{gameObject.name}视野范围可视化关闭完成");
    }

    private void LateUpdate()
    {
        if (visualizationEnable)
        {
            UpdateViewProjectorMaterialData();
            depthCamera.gameObject.SetActive(true);
            //heightCamera.gameObject.SetActive(true);
            viewProjector.gameObject.SetActive(true);
        }//绘制视野
        else
        {
            depthCamera.gameObject.SetActive(false);
            //heightCamera.gameObject.SetActive(false);
            viewProjector.gameObject.SetActive(false);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5);

        Gizmos.color = Color.green;
        Vector3 center = new Vector3(transform.position.x, 0, transform.position.z);
        float startAngle = -viewAngle / 2f;
        float endAngle = viewAngle / 2f;
        float radius = viewRadius;
        float segments = viewAngle;
        Vector3 dir=Vector3.Normalize(new Vector3( transform.forward.x,0,transform.forward.z));
        // 绘制扇形的两条边线（从中心到角度）
        Gizmos.DrawLine(center, center + Quaternion.AngleAxis(startAngle,Vector3.up)*dir * radius);
        Gizmos.DrawLine(center, center + Quaternion.AngleAxis(endAngle, Vector3.up) * dir * radius );

        // 绘制圆弧（在起始和结束角度之间）
        for (float angle = startAngle; angle <= endAngle; angle += (endAngle - startAngle) / segments)
        {
            // 计算当前角度的方向
            Vector3 pointA = center + Quaternion.AngleAxis(angle, Vector3.up) * dir * radius;
            Vector3 pointB = center + Quaternion.AngleAxis(angle + (endAngle - startAngle) / segments, Vector3.up) * dir * radius;

            // 画出当前的两点之间的线
            Gizmos.DrawLine(pointA, pointB);
        }
    }

    public void LockTarget(GameObject target) { _enemyAI.target = target; targetLocked = target; }
    private bool CheckTargetLayer(GameObject obj, LayerMask mask) { return (mask & (1 << obj?.layer)) != 0; }
    private bool IsInSight(GameObject obj)
    {

        //判断是否在视野夹角内
        //默认视野方向即为物体朝向
        Vector3 directionToTarget = Vector3.Normalize(obj.transform.position - viewCenter);
        float distanceToTarget = Vector3.Distance(obj.transform.position, viewCenter);
        Vector2 xzViewForward = new Vector2(transform.forward.x, transform.forward.z).normalized;
        Vector2 xzDirToTarget = new Vector2(directionToTarget.x, directionToTarget.z).normalized;
        if (Vector2.Angle(xzViewForward, xzDirToTarget) < viewAngle / 2)
        {
            //判断是否能看见,对于房子上方可以在周围加上透明的遮挡物，使蹲着时不会被发现
            RaycastHit hit;
            //检测距离默认为distanceToTarget
            float checkRadius = distanceToTarget + 0.01f;//防止误差加0.01f
            //如果目标是人且处于潜行状态，检测距离替换为alarmRadius
            PlayerController tryGet;
            if (obj.TryGetComponent(out tryGet) && tryGet.state == PlayState.crouching) { checkRadius = alarmRadius; }

            Physics.Raycast(viewCenter, directionToTarget, out hit, checkRadius, obstacleMask | targetMask);
            //如果射线检测到target，没有被遮挡
            if (hit.collider != null && CheckTargetLayer(hit.collider.gameObject, targetMask))
            {
                return true;
            }
        }

        return false;
    }
    private void CreateViewCollider()
    {
        _viewCollider = gameObject.AddComponent<CapsuleCollider>();
        _viewCollider.center = viewCenter;
        _viewCollider.radius = viewRadius + 1.5f;//预留1.5f
        _viewCollider.height = viewColliderHeight + 2 * viewRadius;
        _viewCollider.direction = 1;//Y_Axis
        _viewCollider.isTrigger = true;
    }
    private void SetDepthCameraAndTexture()
    {
        transform.Find("[depth camera]").TryGetComponent(out depthCamera);

        if (depthCamera != null )
        {
            depthCamera.clearFlags = CameraClearFlags.Depth;
            depthCamera.cullingMask = obstacleMask.value;
            
            RenderTextureDescriptor depthTextureDesc = default;
            depthTextureDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            depthTextureDesc.width = depthTexWidth;
            depthTextureDesc.height = depthTexHeight;
            depthTextureDesc.volumeDepth = 1;
            depthTextureDesc.colorFormat = RenderTextureFormat.Depth;
            depthTextureDesc.depthBufferBits = 32;
            depthTextureDesc.msaaSamples = 1;//禁用MSAA
            RenderTexture viewDepthMap = new RenderTexture(depthTextureDesc);
            viewDepthMap.Create();
            depthTex = viewDepthMap;
            
            depthCamera.targetTexture = viewDepthMap;
            
            
            depthCamera.aspect = depthTexWidth / (float)depthTexHeight;
            //注意把FOV Axis改为Horizontal
            float horizontalAngle = viewAngle;  // 例如设定为90度
            float aspectRatio = depthCamera.aspect;
            float verticalFOV = 2f * Mathf.Atan(Mathf.Tan(horizontalAngle * Mathf.Deg2Rad / 2f) / aspectRatio) * Mathf.Rad2Deg;
            depthCamera.fieldOfView = verticalFOV;
            
            depthCamera.nearClipPlane = 1f;
            depthCamera.farClipPlane = viewRadius;

            depthCamera.rect = new Rect(0f, 0f, 1f, 1f);
            
            depthCamera.allowMSAA = false;

            depthCamera.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("[depth camera]初始化失败");
        }
    }
    
    private void InitViewProjector()
    {
        viewProjector = GetComponentInChildren<Projector>();
        if (viewProjector != null)
        {
            viewProjector.nearClipPlane = 0.3f;
            viewProjector.farClipPlane = 50f;
            viewProjector.aspectRatio = 1f;
            viewProjector.orthographic = true;
            viewProjector.orthographicSize = viewRadius;
            int allLayers = ~0; // 获取所有层
            int ignoreLayers = allLayers & ~obstacleMask.value;
            //viewProjector.ignoreLayers = ignoreLayers;//忽略除了obstacleMask的全部
            if (viewProjector.material == null) { Debug.LogError($"{gameObject.name}的viewProjector的material没有正常赋值"); }
            
            //传入material数据
            viewProjector.material = new Material(viewProjector.material);
            viewProjector.material.SetVector("_DepthCameraProjectionParams",
                new Vector4(depthCamera.nearClipPlane, depthCamera.farClipPlane, 1.0f + (1.0f / depthCamera.farClipPlane), depthCamera.orthographic ? 0 : 1));
            viewProjector.material.SetTexture("_DepthTex", depthTex);
            viewProjector.material.SetMatrix("_DepthCameraViewMatrix", depthCamera.worldToCameraMatrix);
            
            
            //注意这里
            var projMatrix=GL.GetGPUProjectionMatrix(depthCamera.projectionMatrix, true);
            viewProjector.material.SetMatrix("_DepthCameraProjMatrix", projMatrix);
            var depthCameraMatrix_I_VP=(projMatrix*depthCamera.worldToCameraMatrix).inverse;
            viewProjector.material.SetMatrix("_DepthCameraMatrix_I_VP",depthCameraMatrix_I_VP);
            
            viewProjector.material.SetFloat("_CharCrouchingCheckHeight", charCrouchingCheckHeight);
            viewProjector.material.SetFloat("_CharStandingCheckHeight", charStandingCheckRadius);
            

            viewProjector.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("ViewProjector初始化失败");
        }
    }
    
    private void UpdateViewProjectorMaterialData()
    {
        //viewProjector.material.SetFloat("_CharCrouchingCheckHeight", charCrouchingCheckHeight);
        //viewProjector.material.SetFloat("_CharStandingCheckHeight", charStandingCheckRadius);

        viewProjector.material.SetVector("_xzViewCenterAndDirection", new Vector4(viewCenter.x, viewCenter.z, transform.forward.x, transform.forward.z));
        viewProjector.material.SetFloat("_ViewAngle", viewAngle);
        viewProjector.material.SetFloat("_ViewRadius", viewRadius);
        viewProjector.material.SetFloat("_AlarmRadius", alarmRadius);
    }
}

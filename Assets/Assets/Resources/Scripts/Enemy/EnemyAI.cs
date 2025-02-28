using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState { standby, alarm, CombatState }
public class EnemyAI : MonoBehaviour
{
    public EnemyState state;
    public GameObject target;//锁定的目标
    [SerializeField]
    private bool isAlive;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void UnlockTarget()
    {
        target = null;
        ViewFrustumForEnemy tryGet;
        if (gameObject.TryGetComponent(out tryGet))
        {
            tryGet.targetLocked = null;
        }
    }
}
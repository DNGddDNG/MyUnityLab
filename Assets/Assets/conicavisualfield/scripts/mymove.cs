using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class mymove : MonoBehaviour
{
    public Transform[] point;
    private int index;
    private NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        index = 0;
        agent.SetDestination(point[0].position);
    }

    void Update()
    {
        float ff = Vector3.Distance(transform.position, point[index].position);
        Debug.Log("ff =" + ff);
        if (ff < 1.5f)
        {
            if (index < (point.Length - 1)) 
                index++;
            else index = 0;
          

           // agent.ResetPath();
            agent.SetDestination(point[index].position);

          
        }
    }
}

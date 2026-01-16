using Common;
using UnityEngine;
using UnityEngine.AI;

public class MainCharacterMove : MonoBehaviour
{
    public NavMeshAgent Agent;

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (PartyHealth.IsDead())
        {
            Agent.isStopped = true;
            return;
        }
        
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Agent.SetDestination(hit.point);
            }
        }
    }
}

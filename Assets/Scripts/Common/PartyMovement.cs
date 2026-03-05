using System;
using System.Collections;
using Camping;
using Common;
using Deckbuilding.Interactables;
using Deckbuilding.Windows;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PartyMovement : SingletonMonoBehavior<PartyMovement>
{
    public NavMeshAgent Agent;
    private Interactable _target;
    private float _distanceToTarget;
    private bool _ignoreNextClick;
    private bool _clicked;

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.updatePosition = true;
        Agent.updateRotation = true;
    }

    void Update()
    {
        if (PartyHealth.IsDead())
        { 
            if (!Agent.enabled)
                return;

            Agent.SetDestination(transform.position);
            Agent.velocity = Vector3.zero;
            Agent.enabled = false;
            return;
        }

        Agent.enabled = true;

        if (!_ignoreNextClick && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Agent.SetDestination(hit.point);
            }

            Windows.Instance.CloseAll();
            
            _target = null;
        }
        else if (_target != null)
        {
            if (Vector3.Distance(transform.position, _target.transform.position) <= _distanceToTarget)
            {
                _target.InteractOnEndOfMovement();
                _target = null;
                Agent.ResetPath();
                //Agent.velocity = Vector3.zero;
            }
            else
            {
                Agent.SetDestination(_target.transform.position);
            }
        }

        _ignoreNextClick = false;
    }

    public void Set(Interactable target, float distance)
    {
        _target = target;
        _distanceToTarget = distance;
        _ignoreNextClick = true;
        Windows.Instance.CloseAll();
    }
    
    public static bool IsMoving()
    {
        if (Instance == null)
            return false;
        return Instance.Agent.velocity.sqrMagnitude > 0;
    }
}

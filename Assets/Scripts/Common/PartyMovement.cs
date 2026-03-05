using System;
using System.Collections;
using Camping;
using Common;
using Deckbuilding;
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
    private bool _clicked;
    private Vector3 _targetPosition;

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.updatePosition = true;
        Agent.updateRotation = true;
        _targetPosition = transform.position;
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
        
        if (_target != null)
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
        else
        {
            Agent.SetDestination(_targetPosition);
        }
    }

    public void Set(Interactable target, float distance)
    {
        _target = target;
        _distanceToTarget = distance;
        Windows.Instance.CloseAll();
    }
    
    public void Set(Vector3 target)
    {
        _targetPosition = target;
        _target = null;
        Windows.Instance.CloseAll();
        PartyMembers.Instance.SelectedMember = null;
    }
    
    public static bool IsMoving()
    {
        if (Instance == null)
            return false;
        return Instance.Agent.velocity.sqrMagnitude > 0;
    }

    public static void NewSeason()
    {
        Instance._targetPosition = Instance.transform.position = TavernPoint.Instance.transform.position;
    }
}

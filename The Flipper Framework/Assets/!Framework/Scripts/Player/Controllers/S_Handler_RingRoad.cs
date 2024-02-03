﻿using UnityEngine;
using System.Collections;

public class S_Handler_RingRoad : MonoBehaviour {

    S_CharacterTools Tools;
    S_PlayerPhysics player;
    public bool HasTarget { get; set; }
    public GameObject TargetObject { get; set; }
    S_ActionManager Actions;

    float TargetSearchDistance = 10;
    Transform Icon;
    float IconScale;

    [HideInInspector] public GameObject[] Targets;
    [HideInInspector] public GameObject[] TgtDebug;

    LayerMask layer;
    Transform MainCamera;


    void Awake()
    {
        if (Actions == null)
        {
            Tools = GetComponent<S_CharacterTools>();
            AssignTools();
            AssignStats();
        }
        
    }

    void Start()
    {

        StartCoroutine(ScanForTargets());
    }


    private void FixedUpdate()
    {
        if(Actions.Action == S_ActionManager.States.RingRoad)
        {
            Collider[] TargetsInRange = GetCloseTargets(TargetSearchDistance);

            if (TargetsInRange.Length > 0)
            {
                //yield return new WaitForFixedUpdate();
                TargetObject = GetClosestTarget(TargetsInRange);
            }
        }
    }

    private IEnumerator ScanForTargets()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();
           

            if (Actions.Action == S_ActionManager.States.Jump || Actions.Action == S_ActionManager.States.Regular)
            {
                Collider[] TargetsInRange = GetCloseTargets(TargetSearchDistance * 1.45f);

                if(TargetsInRange.Length > 0)
                {
                    yield return new WaitForFixedUpdate();
                    TargetObject = GetClosestTarget(TargetsInRange);
                    performRR();
                }

            }
            else
            {
                yield return new WaitForFixedUpdate();
            }
        }
        
    }

    void performRR()
    {
        //Do a LightDash Attack
        if (Actions.Action != S_ActionManager.States.RingRoad && Actions.InteractPressed && TargetObject != null)
        {
            //Debug.Log("LightDash");
            Actions.CamResetPressed = false;
            Actions.ChangeAction(S_ActionManager.States.RingRoad);
            Actions.Action07.InitialEvents();
        }
    }

    Collider[] GetCloseTargets(float maxDistance)
    {
        Collider[] TargetsInRange = Physics.OverlapSphere(transform.position, maxDistance, layer, QueryTriggerInteraction.Collide);
        return TargetsInRange;
    }

	GameObject GetClosestTarget(Collider[] TargetsInRange)
	{
        HasTarget = false;
      
        int checkLimit = 0;
        Transform closestTarget = null;
        foreach (Collider t in TargetsInRange)
        {
            if (t != null)
            {
                Transform target = t.transform;
                closestTarget = checkTarget(target, closestTarget);

                checkLimit++;
                if (checkLimit > 3)
                    break;
            }

        }

        if (closestTarget != null)
            return closestTarget.gameObject;
        else
            return null;

    }

    Transform checkTarget(Transform thisTarget, Transform current)
    {
        float dis = Vector3.Distance(transform.position, thisTarget.position);

        if (current == null)
            return thisTarget;
        else
        {
            float closDis = Vector3.Distance(transform.position, current.position);
            if (closDis > dis)
            {
                HasTarget = true;
                return thisTarget;
            }

        }
       
        return current;
    }

    void AssignStats()
    {
        TargetSearchDistance = Tools.coreStats.RingTargetSearchDistance;
        IconScale = Tools.coreStats.RingRoadIconScale;
        layer = Tools.coreStats.RingRoadLayer;
    }

    void AssignTools()
    {
        Actions = GetComponent<S_ActionManager>();
        player = GetComponent<S_PlayerPhysics>();

        Icon = Tools.homingIcons.GetComponent<Transform>();
        MainCamera = Tools.MainCamera;
    }


}

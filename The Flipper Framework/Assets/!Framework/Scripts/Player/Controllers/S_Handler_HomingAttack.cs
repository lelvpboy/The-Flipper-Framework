﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class S_Handler_HomingAttack : MonoBehaviour
{
    S_CharacterTools tools;

    public bool HasTarget { get; set; }
    [HideInInspector] public GameObject TargetObject;
    GameObject previousTarget;
    
    S_ActionManager Actions;
    S_PlayerPhysics player;

    bool scanning = true;

    float _targetSearchDistance_ = 10;
    float _faceRange_ = 66;
    LayerMask _TargetLayer_;
    LayerMask _BlockingLayers_;
    float _fieldOfView_;
    float _facingAmount_;
    float distance;

    AudioSource IconSound;
    GameObject AlreadyPlayed;
    Animator IconAnim;
    Animator CharacterAnimator;


    Transform Icon;
    float _iconScale_;
    GameObject normalIcon;
    GameObject damageIcon;

    public static GameObject[] Targets;
    [HideInInspector] public GameObject[] TgtDebug;

    Transform MainCamera;
    float _iconDistanceScaling_;

    int HomingCount;
    public bool HomingAvailable { get; set; }


    void Awake()
    {
        if (player == null)
        {
            tools = GetComponent<S_CharacterTools>();
            AssignTools();

            AssignStats();
        }

    }

    void Start()
    {
        //var tgt = GameObject.FindGameObjectsWithTag("HomingTarget");
        //Targets = tgt;
        //TgtDebug = tgt;

        Icon.parent = null;
        StartCoroutine(ScanForTargets(.10f));
    }



    void FixedUpdate()
    {

        //Prevent Homing attack spamming

        HomingCount += 1;

        if (Actions.whatAction == S_Enums.PlayerStates.Homing)
        {
            HomingAvailable = false;
            HomingCount = 0;
        }
        if (HomingCount > 3)
        {
            HomingAvailable = true;
        }




        if (HasTarget && TargetObject != null)
        {
            Icon.position = TargetObject.transform.position;
            float camDist = Vector3.Distance(transform.position, MainCamera.position);
            Icon.localScale = (Vector3.one * _iconScale_) + (Vector3.one * (camDist * _iconDistanceScaling_));

            if (AlreadyPlayed != TargetObject)
            {
                AlreadyPlayed = TargetObject;
                IconSound.Play();
                IconAnim.SetTrigger("NewTgt");
            }

        }
        else
        {
            Icon.localScale = Vector3.zero;
        }

    }

    IEnumerator ScanForTargets(float secondsBetweenChecks)
    {
        while (scanning)
        {

            while (!player.Grounded && Actions.whatAction != S_Enums.PlayerStates.Rail)
            {
                UpdateHomingTargets();
                if (!HasTarget)
                    yield return new WaitForSeconds(secondsBetweenChecks);
                else
                {
                    //Debug.Log(Vector3.Distance(transform.position, TargetObject.transform.position));
                    yield return new WaitForSeconds(secondsBetweenChecks * 1.5f);                  
                }
            }
            previousTarget = null;
            HasTarget = false;
            yield return new WaitForSeconds(.1f);
        }


    }

    //This function will look for every possible homing attack target in the whole level. 
    //And you can call it from other scritps via [ HomingAttackControl.UpdateHomingTargets() ]
    public void UpdateHomingTargets()
    {
        HasTarget = false;
        TargetObject = null;
        TargetObject = GetClosestTarget(_TargetLayer_, _targetSearchDistance_);
        previousTarget = TargetObject;

    }

    public GameObject GetClosestTarget(LayerMask layer, float Radius)
    {
        ///First we use a spherecast to get every object with the given layer in range. Then we go through the
        ///available targets from the spherecast to find which is the closest to Sonic.

        GameObject closestTarget = null;
        distance = 0f;
        int checkLimit = 0;
        RaycastHit[] NewTargetsInRange = Physics.SphereCastAll(transform.position, 10f, Camera.main.transform.forward, _faceRange_, layer);
        foreach (RaycastHit t in NewTargetsInRange)
        {
            if (t.collider.gameObject.GetComponent<S_Data_HomingTarget>())
            {

                Transform target = t.collider.transform;
                closestTarget = checkTarget(target, Radius, closestTarget, 1.5f);
            }

            checkLimit++;
            if (checkLimit > 3)
                break;
        }

        checkLimit = 0;
        if (closestTarget == null)
        {
            Collider[] TargetsInRange = Physics.OverlapSphere(transform.position, Radius, layer);
            foreach (Collider t in TargetsInRange)
            {

                if (t.gameObject.GetComponent<S_Data_HomingTarget>())
                {
 
                    Transform target = t.gameObject.transform;
                    closestTarget = checkTarget(target, Radius, closestTarget, 1);
                }

                checkLimit++;
                if (checkLimit > 3)
                    break;

            }

            if (previousTarget != null)
            {
   
                closestTarget = checkTarget(previousTarget.transform, Radius, closestTarget, 1.3f);
            }
        }
        
        return closestTarget;
    }

    GameObject checkTarget(Transform target, float Radius, GameObject closest, float maxDisMod)
    {
        Vector3 Direction = CharacterAnimator.transform.position - target.position;
        float TargetDistance = (Direction.sqrMagnitude / Radius) / Radius;

        if(TargetDistance < maxDisMod * Radius)
        {
            bool Facing = Vector3.Dot(CharacterAnimator.transform.forward, Direction.normalized) < _facingAmount_; //Make sure Sonic is facing the target enough

            Vector3 screenPoint = Camera.main.WorldToViewportPoint(target.position); //Get the target's screen position
            bool onScreen = screenPoint.z > 0.3f && screenPoint.x > 0.08 && screenPoint.x < 0.92f && screenPoint.y > 0f && screenPoint.y < 0.95f; //Make sure the target is on screen

            if ((TargetDistance < distance || distance == 0f) && Facing && onScreen)
            {
                if (!Physics.Linecast(transform.position, target.position, _BlockingLayers_))
                {
                    HasTarget = true;
                    //Debug.Log(closestTarget);
                    distance = TargetDistance;
                    return target.gameObject;
                }
            }
        }
        
        return closest;
    }

    private void AssignTools()
    {

        Actions = GetComponent<S_ActionManager>();
        player = GetComponent<S_PlayerPhysics>();
        CharacterAnimator = tools.CharacterAnimator;

        MainCamera = tools.MainCamera;

        Icon = tools.homingIcons.transform;
        normalIcon = tools.normalIcon;
        damageIcon = tools.weakIcon;

        IconSound = Icon.gameObject.GetComponent<AudioSource>();
        IconAnim = Icon.gameObject.GetComponent<Animator>();
    }

    private void AssignStats()
    {
        _targetSearchDistance_ = tools.Stats.HomingSearch.targetSearchDistance;
        _faceRange_ = tools.Stats.HomingSearch.faceRange;
        _TargetLayer_ = tools.Stats.HomingSearch.TargetLayer;
        _BlockingLayers_ = tools.Stats.HomingSearch.blockingLayers;
        _fieldOfView_ = tools.Stats.HomingSearch.fieldOfView;
        _facingAmount_ = tools.Stats.HomingSearch.facingAmount;

        _iconScale_ = tools.Stats.HomingSearch.iconScale;
        _iconDistanceScaling_ = tools.Stats.HomingSearch.iconDistanceScaling;
    }


} 


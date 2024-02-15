﻿using UnityEngine;
using System.Collections;

public class S_Action11_JumpDash : MonoBehaviour
{
    S_CharacterTools Tools;

    S_ActionManager Action;
    S_VolumeTrailRenderer HomingTrailScript;
    Animator CharacterAnimator;
    S_Action02_Homing Action02;
    S_PlayerPhysics Player;
    S_PlayerInput Inp;
    S_Handler_Camera Cam;

    [HideInInspector] public bool _isAdditive_;
    [HideInInspector] public float _AirDashSpeed_;
    [HideInInspector] public float _AirDashDuration_;

    float XZmag;
    GameObject HomingTrailContainer;
    GameObject JumpDashParticle;
    GameObject JumpBall;

    float Timer;
    float Aspeed;


    public float skinRotationSpeed;

    Vector3 Direction;



    void Awake()
    {
        if (Player == null)
        {
            Tools = GetComponent<S_CharacterTools>();
            AssignTools();

            AssignStats();
        }

        Action02.HomingAvailable = true;
    }


    public void InitialEvents()
    {
        if(!Action.lockJumpDash)
        {
            if (Action02.HomingAvailable)
            {

                HomingTrailScript.emitTime = _AirDashDuration_ + 0.5f;
                HomingTrailScript.emit = true;

                JumpBall.SetActive(false);
                Action.SpecialPressed = false;
                Action.HomingPressed = false;

                Timer = 0;
                Action02.HomingAvailable = false;

                XZmag = Player._horizontalSpeedMagnitude;

                AirDashParticle();

                if (XZmag < _AirDashSpeed_)
                {
                    Aspeed = _AirDashSpeed_;
                }
                else
                {
                    Aspeed = XZmag;
                }

                Direction = CharacterAnimator.transform.forward;
                //Direction = Vector3.RotateTowards(Direction, lateralToInput * Direction, turnRate * 40f, 0f);


            }
            else
            {
                //Action.ChangeAction(Action.PreviousAction);
            }
        }

    }

    void Update()
    {

        //Set Animator Parameters
        CharacterAnimator.SetInteger("Action", 11);
        CharacterAnimator.SetFloat("YSpeed", Player._RB.velocity.y);
        CharacterAnimator.SetFloat("GroundSpeed", Player._RB.velocity.magnitude);
        CharacterAnimator.SetBool("Grounded", Player._isGrounded);

        //Set Animation Angle
        Vector3 VelocityMod = new Vector3(Player._RB.velocity.x, 0, Player._RB.velocity.z);
        if(VelocityMod != Vector3.zero)
        {
            Quaternion CharRot = Quaternion.LookRotation(VelocityMod, transform.up);
            CharacterAnimator.transform.rotation = Quaternion.Lerp(CharacterAnimator.transform.rotation, CharRot, Time.deltaTime * skinRotationSpeed);
        }    



    }

    void FixedUpdate()
    {
        //Debug.Log(Timer);

        Timer += Time.deltaTime;

        if (Inp.inputPreCamera != Vector3.zero)
        {
            if(Timer > 0.03)
            {
                
                Vector3 FaceDir = CharacterAnimator.transform.position - Cam.Cam.transform.position;
                bool Facing = Vector3.Dot(CharacterAnimator.transform.forward, FaceDir.normalized) < 0f;
                if (Facing)
                {
                    Inp.inputPreCamera.x = -Inp.inputPreCamera.x;
                }


                //Direction = CharacterAnimator.transform.forward;
                Direction = Vector3.RotateTowards(new Vector3 (Direction.x, 0, Direction.z), CharacterAnimator.transform.right, Mathf.Clamp(Inp.inputPreCamera.x * 4, -2.5f, 2.5f) * Time.deltaTime, 0f);
            }           

            //Direction.y = Player.fallGravity.y * 0.1f;
          
        }
        else
        {
            //Direction = (transform.TransformDirection(Player.PreviousRawInput).normalized + (Player.rb.velocity).normalized * 2);
            //Direction.y = Player.fallGravity.y * 0.1f;

        }

        Vector3 newVec = Direction.normalized * Aspeed;
        if(Player._RB.velocity.y < 0)
            newVec.y = Player._fallGravity_.y * 0.5f;

        Player.setCoreVelocity(newVec);

        //End homing attck if in air for too long
        if (Timer > _AirDashDuration_)
        {
            JumpBall.SetActive(true);
            Action.ChangeAction(S_Enums.PlayerStates.Jump);
        }
        else if (Player._isGrounded)
        {
            CharacterAnimator.SetInteger("Action", 0);
            CharacterAnimator.SetBool("Grounded", Player._isGrounded);
            Action.ChangeAction(S_Enums.PlayerStates.Regular);
        }
    }

    public void AirDashParticle()
    {
        GameObject JumpDashParticleClone = Instantiate(JumpDashParticle, HomingTrailContainer.transform.position, Quaternion.identity) as GameObject;
        //if (Player.SpeedMagnitude > 60)
        //    JumpDashParticleClone.GetComponent<ParticleSystem>().startSize = Player.SpeedMagnitude / 60f;
        //else
        //    JumpDashParticleClone.GetComponent<ParticleSystem>().startSize = 1f;

        if (Player._speedMagnitude > 60)
            JumpDashParticleClone.transform.localScale = new Vector3(Player._speedMagnitude / 60f, Player._speedMagnitude / 60f, Player._speedMagnitude / 60f);
        //else
        //    JumpDashParticleClone.GetComponent<ParticleSystem>().startSize = 1f;

        JumpDashParticleClone.transform.position = HomingTrailContainer.transform.position;
        JumpDashParticleClone.transform.rotation = HomingTrailContainer.transform.rotation;
        //JumpDashParticleClone.transform.parent = HomingTrailContainer.transform;
    }

    private void AssignStats()
    {
        _isAdditive_ = Tools.Stats.JumpDashStats.shouldUseCurrentSpeedAsMinimum;
        _AirDashSpeed_ = Tools.Stats.JumpDashStats.dashSpeed;
        _AirDashDuration_ = Tools.Stats.JumpDashStats.duration;
    }

    private void AssignTools()
    {
        //HomingAttackControl.TargetObject = null;
        Player = GetComponent<S_PlayerPhysics>();
        Action = GetComponent<S_ActionManager>();
        Action02 = GetComponent<S_Action02_Homing>();
        Inp = GetComponent<S_PlayerInput>();
        Cam = GetComponent<S_Handler_Camera>();


        CharacterAnimator = Tools.CharacterAnimator;
        HomingTrailScript = Tools.HomingTrailScript;
        HomingTrailContainer = Tools.HomingTrailContainer;
        JumpBall = Tools.JumpBall;
        JumpDashParticle = Tools.JumpDashParticle;
    }

}

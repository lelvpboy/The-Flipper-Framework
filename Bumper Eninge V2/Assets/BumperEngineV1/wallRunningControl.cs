using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wallRunningControl : MonoBehaviour
{
    CharacterStats Stats;
    CharacterTools Tools;

    Action01_Jump JumpAction;
    Action00_Regular RegularAction;
    PlayerBhysics Player;
    ActionManager Actions;
    HomingAttackControl homingControl;
    CameraControl Cam;

    SonicSoundsControl sounds;
    Animator CharacterAnimator;
    public float skinRotationSpeed;
    GameObject JumpBall;

    [HideInInspector] public GameObject bannedWall;

    [Header("Detecting Wall Run")]
    bool canCheck = false;
    Action12_WallRunning WallRun;
    float WallCheckDistance;
    LayerMask wallLayerMask;
    float CheckModifier;

    private RaycastHit leftWallDetect;
    private bool wallLeft;
    private RaycastHit rightWallDetect;
    private bool wallRight;
    private RaycastHit frontWallDetect;
    private bool wallFront;

    [Header ("Quickstepping")]
    bool StepRight;
    float StepDistance = 50f;
    float DistanceToStep;


    // Start is called before the first frame update
    void Start()
    {
        //Assigns tool and stats for later use.
        if (Player == null)
        {
            Tools = GetComponent<CharacterTools>();
            AssignTools();

            Stats = GetComponent<CharacterStats>();
            AssignStats();
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //If jumping, step stats reflect jump versions.
        if (Actions.Action == 1)
        {
            StepRight = JumpAction.StepRight;
            StepDistance = JumpAction.StepDistance;
            DistanceToStep = JumpAction.DistanceToStep;
            canCheck = true;
        }
        //If moving normally, step stats refelct normal versions.
        else if (Actions.Action == 0)
        {
            StepRight = RegularAction.StepRight;
            StepDistance = RegularAction.airStepDistance;
            DistanceToStep = RegularAction.DistanceToStep;
            canCheck = true;
        }
        //Manages what actions can and cannot traverse into a wall run.
        else if (Actions.Action == 6 || Actions.Action == 11)
            canCheck = true;

        else canCheck = false;
        
        //If able, check for wall run. Player must be in air, pressing skid, and able to.
        if (Actions.SkidPressed)
        {
            if (!Player.Grounded && canCheck)
                checkWallRun();
        }

        if (Player.Grounded)
        {
            bannedWall = null;
        }
   
    }


    //Responsible for swtiching to wall run if specifications are met.
    private void checkWallRun()
    {

        //If High enough above ground and not at an odd rotation
        if (enoughAboveGround())
        {
            //Checks for nearby walls using raycasts
            CheckForWall();

            //If detecting a wall in front with a near horizontal normal
            if (wallFront && frontWallDetect.normal.y <= 0.3 && frontWallDetect.normal.y >= -0.2 && Player.HorizontalSpeedMagnitude > 30f)
            {
                //If facing the wall enough
                if (Vector3.Dot(CharacterAnimator.transform.forward, frontWallDetect.normal) < -0.85f)
                {
                    //Enter wall run as a climb

                    WallRun.InitialEvents(true, frontWallDetect, false, WallCheckDistance * CheckModifier);
                    Actions.ChangeAction(12);
                }

            }

            //If detecting a wall to the side

            //If detecting a wall on left with correct angle.
            else if (wallLeft && DistanceToStep < StepDistance / 2  && leftWallDetect.normal.y <= 0.4 && Player.HorizontalSpeedMagnitude > 40f &&
                leftWallDetect.normal.y >= -0.4 && !(DistanceToStep > 0 && StepRight))
            {
                //Enter a wallrun with wall on left.
                WallRun.InitialEvents(false, leftWallDetect, false);
                Actions.ChangeAction(12);
            }

            //If detecting a wall on right with correct angle.
            else if (wallRight && DistanceToStep < StepDistance / 2 && rightWallDetect.normal.y <= 0.4 && Player.HorizontalSpeedMagnitude > 40f &&
                rightWallDetect.normal.y >= -0.4 && !(DistanceToStep > 0 && !StepRight))
            {
                //Enter a wallrun with wall on right.
                WallRun.InitialEvents(false, rightWallDetect, true);
                Actions.ChangeAction(12);
            }
        }
    }


    private bool enoughAboveGround()
    {
        //If racycast does not detect ground
        return !Physics.Raycast(CharacterAnimator.transform.position, -Vector3.up, 5f, wallLayerMask);
    }

    private void CheckForWall()
    {

        //Checks for wall in front using raycasts, outputing hits and booleans
        wallFront = Physics.Raycast(new Vector3(transform.position.x, transform.position.y - 0.3f, transform.position.z), CharacterAnimator.transform.forward, out frontWallDetect,
            WallCheckDistance, wallLayerMask);

        //If there isn't a wall and moving fast enough
        if (!wallFront && Player.HorizontalSpeedMagnitude > 20f)
        {
            //Increases check range based on speed
            CheckModifier = (Player.HorizontalSpeedMagnitude * 0.015f) + .5f;
            wallFront = Physics.Raycast(new Vector3(transform.position.x, transform.position.y - 0.3f, transform.position.z), CharacterAnimator.transform.forward, out frontWallDetect,
            WallCheckDistance * CheckModifier, wallLayerMask);

            //If there is no wall in front, checks for walls on sides instead. Only if moving fast enough.
            if (!wallFront && Player.HorizontalSpeedMagnitude > 30f)
            {
                //Checks for nearby walls using raycasts, outputing hits and booleans
                wallRight = Physics.Raycast(CharacterAnimator.transform.position, CharacterAnimator.transform.right, out rightWallDetect, WallCheckDistance, wallLayerMask);
                wallLeft = Physics.Raycast(CharacterAnimator.transform.position, -CharacterAnimator.transform.right, out leftWallDetect, WallCheckDistance, wallLayerMask);

                //If no walls directily on sides, checks at angles with greater range.
                if (!wallRight && !wallLeft)
                {
                    //Checks for wall on right first. Sets angle between right and forward and uses it.
                    Vector3 direction = Vector3.Lerp(CharacterAnimator.transform.right, CharacterAnimator.transform.forward, 0.4f);
                    wallRight = Physics.Raycast(CharacterAnimator.transform.position, direction, out rightWallDetect, WallCheckDistance * 2, wallLayerMask);

                    //If no wall on right, checks left.
                    if (!wallRight)
                    {
                        //Same as before but left
                        direction = Vector3.Lerp(-CharacterAnimator.transform.right, CharacterAnimator.transform.forward, 0.4f);
                        wallLeft = Physics.Raycast(CharacterAnimator.transform.position, direction, out leftWallDetect, WallCheckDistance * 2, wallLayerMask);

                        //If they find the wall, apply force towards it.
                        if (wallLeft)
                        {
                            Debug.Log("Angle left");
                            //Player.p_rigidbody.AddForce(direction * 20f);
                        }
                    }
                    //If they find the wall, apply force towards it.
                    else if (wallRight)
                    {
                        Debug.Log("Angle Right");
                        //Player.p_rigidbody.AddForce(direction * 20f);
                    }
                }
            }
        }

        //Checks if the wall can be used. Banned walls are set when the player jumps off the wall.
        if (wallFront)
        {
            if (frontWallDetect.collider.gameObject == bannedWall)
                wallFront = false;
        }
        if (wallRight)
        {
            if (rightWallDetect.collider.gameObject == bannedWall)
                wallRight = false;
        }
        if (wallLeft)
        {
            if (leftWallDetect.collider.gameObject == bannedWall)
                wallLeft = false;
        }



        //Lines being drawn for testing

        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + CharacterAnimator.transform.right * WallCheckDistance, Color.red);
        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + -CharacterAnimator.transform.right * WallCheckDistance, Color.red);
        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + CharacterAnimator.transform.forward * WallCheckDistance, Color.red);

        //CheckModifier = (Player.HorizontalSpeedMagnitude * 0.015f) + .5f;
        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + CharacterAnimator.transform.forward * (WallCheckDistance * CheckModifier), Color.blue);

        //Vector3 directions = Vector3.Lerp(CharacterAnimator.transform.right, CharacterAnimator.transform.forward, 0.4f);
        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + directions * (2 * WallCheckDistance), Color.blue);

        //directions = Vector3.Lerp(-CharacterAnimator.transform.right, CharacterAnimator.transform.forward, 0.4f);
        //Debug.DrawLine(CharacterAnimator.transform.position, CharacterAnimator.transform.position + directions * (2 * WallCheckDistance), Color.blue);

        //if (wallRight)
        //    Debug.Log(rightWallDetect.normal);
        //if (wallLeft)
        //    Debug.Log(leftWallDetect.normal);
        //if (wallFront)
        //    Debug.Log(frontWallDetect.normal);
    }

    //Reponsible for assigning stats from the stats script.
    private void AssignStats()
    {

        wallLayerMask = Stats.wallLayerMask;
        WallCheckDistance = Stats.WallCheckDistance;
    }

    //Responsible for assigning objects and components from the tools script.
    private void AssignTools()
    {
        Player = GetComponent<PlayerBhysics>();
        Actions = GetComponent<ActionManager>();
        Cam = GetComponent<CameraControl>();
        homingControl = GetComponent<HomingAttackControl>();
        WallRun = Actions.Action12;
        JumpAction = Actions.Action01;
        RegularAction = Actions.Action00;

        CharacterAnimator = Tools.CharacterAnimator;
        sounds = Tools.SoundControl;
        JumpBall = Tools.JumpBall;
    }
}

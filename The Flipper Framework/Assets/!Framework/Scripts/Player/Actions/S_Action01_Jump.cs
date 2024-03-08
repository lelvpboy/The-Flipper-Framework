﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(S_ActionManager))]
public class S_Action01_Jump : MonoBehaviour, IMainAction
{
	/// <summary>
	/// Properties ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region properties

	//Unity
	#region Unity Specific Properties
	private S_CharacterTools      _Tools;
	private S_PlayerPhysics       _PlayerPhys;
	private S_PlayerInput         _Input;
	private S_ActionManager       _Actions;
	private S_Handler_Camera      _CamHandler;
	private S_Control_PlayerSound _Sounds;

	private Animator              _CharacterAnimator;
	private GameObject            _JumpBall;
	#endregion

	//General
	#region General Properties

	//Stats - See Stats scriptable objects for tooltips explaining their purpose.
	#region Stats
	//Main jump
	private float       _maxJumpTime_;
	private float       _minJumpTime_;
	private float       _startSlopedJumpDuration_;
	private float       _startJumpSpeed_;
	private float       _jumpSlopeConversion_;
	private float       _stopYSpeedOnRelease_;

	//Additional jumps
	private int         _maxJumps_;
	private float       _doubleJumpSpeed_;
	private float       _doubleJumpDuration_;
	private float       _speedLossOnDoubleJump_;
	#endregion

	// Trackers
	#region trackers

	private int         _positionInActionList;         //In every action script, takes note of where in the Action Managers Main action list this script is. 

	public float        _skinRotationSpeed;

	[HideInInspector]
	public Vector3      _upwardsDirection;	//The direction the jump will move in. If on the ground, follows the normal of the floor, otherwise is upwards.
	[HideInInspector]
	public float        _counter;		//Tracks how long is jumping for
	[HideInInspector]
	public bool         _isJumping;	//Will only apply force if this is true, when false, it means another jump can now be performed
	private bool        _isJumpingFromGround;	//Seperates grounded and air jumps

	private float       _thisJumpDuration;		//Cancels jump when counter exceeds this. Affected by stats and situation
	private float       _slopedJumpDuration;	//When exceeded, jump will only move upwards, even if started on a slope.	
	private float       _thisJumpSpeed;		
	private float       _jumpSlopeSpeed;		//Jump speed is different on slopes, determined by upwards speed and conversion stat


	private float       _jumpSpeedModifier = 1f;
	private float       _jumpDurationModifier = 1f;

	#endregion
	#endregion
	#endregion

	/// <summary>
	/// Inherited ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region Inherited

	// Start is called before the first frame update
	void Start () {

	}

	// Called when the script is enabled, but will only assign the tools and stats on the first time.
	private void OnEnable () {
		ReadyAction();
		_JumpBall.SetActive(true);
	}

	private void OnDisable () {
		_JumpBall.SetActive(false);
	}


	// Update is called once per frame
	void Update () {
		//Set Animator Parameters
		_Actions.ActionDefault.HandleAnimator(1);
		_Actions.ActionDefault.SetSkinRotationToVelocity(_skinRotationSpeed);

		//Actions
		if (!_Actions.isPaused)
		{
			HandleInputs();
		}
	}

	private void FixedUpdate () {
		//Tracking length of jump
		_counter += Time.fixedDeltaTime;
		_Actions._actionTimeCounter += Time.fixedDeltaTime;

		ApplyForce();

		CheckShouldEndAction();
	}

	//Called when checking if this action is to be performed, including inputs.
	public bool AttemptAction () {
		if (_Input.JumpPressed)
		{
			switch (_Actions.whatAction)
			{
				case S_Enums.PrimaryPlayerStates.Default:
					//Normal grounded Jump
					if (_PlayerPhys._isGrounded)
					{
						AssignStartValues(_PlayerPhys._groundNormal, true);
						StartAction();
					}
					//Jump from regular action due to coyote time
					else if (_Actions.ActionDefault.enabled && _Actions.ActionDefault._isCoyoteInEffect)
					{
						AssignStartValues(_Actions.ActionDefault._coyoteRememberDirection, true);
						StartAction();
					}
					//Jump when in the air
					else if (_Actions._jumpCount < _maxJumps_ && !_Actions.lockDoubleJump)
					{
						AssignStartValues(Vector3.up, false);
						StartAction();
					}
					return true;
				case S_Enums.PrimaryPlayerStates.Jump:
					if (!_isJumping && _Actions._jumpCount < _maxJumps_ && !_Actions.lockDoubleJump)
					{
						AssignStartValues(Vector3.up, false);
						StartAction();
					}
					return true;
				case S_Enums.PrimaryPlayerStates.Rail:
					AssignStartValues(transform.up, true);
					StartAction();
					return true;
				case S_Enums.PrimaryPlayerStates.WallRunning:
					AssignStartValues(_Actions.Action12._jumpAngle, true);
					StartAction();
					return true;
			}
		}
		return false;
	}

	public void StartAction () {

		ReadyAction();

		//Setting private
		_isJumping = true;
		_counter = 0;

		//Setting public
		_Input.RollPressed = false;
		_Actions._actionTimeCounter = 0;
		_PlayerPhys.SetIsGrounded(false);

		//Effects
		_CharacterAnimator.SetInteger("Action", 1);
		_CharacterAnimator.SetTrigger("ChangedState");
		_Sounds.JumpSound();
		_Actions.ActionDefault.SwitchSkin(false);

		//Snap off of ground to make sure you do jump
		transform.position += (_upwardsDirection * 0.3f);

		//If performing a grounded jump. JumpCount may be changed externally to allow for this.
		if (_isJumpingFromGround)
		{
			if (_Actions.eventMan != null) _Actions.eventMan.JumpsPerformed += 1;

			//Sets jump stats for this specific jump.
			_thisJumpSpeed = _startJumpSpeed_ * _jumpSpeedModifier;
			_thisJumpDuration = _maxJumpTime_ * _jumpDurationModifier;
			_slopedJumpDuration = _startSlopedJumpDuration_ * _jumpDurationModifier;

			//Jump higher depending on the speed and the slope you're in
			if (_PlayerPhys._RB.velocity.y > 5 && _upwardsDirection.y > 1)
			{
				_jumpSlopeSpeed = _PlayerPhys._RB.velocity.y * _jumpSlopeConversion_;
			}
			else if (Mathf.Abs(_upwardsDirection.y) < 0.1f && _PlayerPhys._RB.velocity.y < -5)
			{
				_upwardsDirection.y = 4;
				_upwardsDirection.Normalize();
			}

			_Actions._jumpCount = 1; //Number of jumps set to 1, allowing for double jumps.
		}
		else
		{
			if (_Actions.eventMan != null) _Actions.eventMan.DoubleJumpsPerformed += 1;

			//Sets jump stats for this specific jump.
			_thisJumpSpeed = _doubleJumpSpeed_ * _jumpSpeedModifier;
			_thisJumpDuration = _doubleJumpDuration_ * _jumpDurationModifier;
			_slopedJumpDuration = _doubleJumpDuration_ * _jumpDurationModifier;

			_jumpSlopeSpeed = 0;

			_Actions._jumpCount = Mathf.Clamp(_Actions._jumpCount + 1, 1, _Actions._jumpCount + 1); //Track this new jump

			JumpInAir();
		}

		_Actions.ChangeAction(S_Enums.PrimaryPlayerStates.Jump);
	}

	public void StopAction () {
		if (enabled) enabled = false;
		else return;

		_Actions.ActionDefault.SwitchSkin(true);
	}

	//This has to be set up in Editor. The invoker is in the PlayerPhysics script component, adding this event to it will mean this is called whenever the player lands.
	public void EventOnGrounded() {
		_Actions._jumpCount = 0;
	}

	#endregion

	/// <summary>
	/// Private ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region private

	//Called when entering the action, to ready any variables needed for performing it.
	private void AssignStartValues ( Vector3 normaltoJump, bool fromGround = false ) {
		if (1 - Mathf.Abs(normaltoJump.y) < 0.1f)
			normaltoJump = Vector3.up;

		//Sets jump direction
		_upwardsDirection = normaltoJump;
		_isJumpingFromGround = fromGround;
	}

	public void HandleInputs () {
		if (!_Actions.isPaused)
		{
			//Moving camera behind
			_CamHandler.AttemptCameraReset();

			//Action Manager goes through all of the potential action this action can enter and checks if they are to be entered
			_Actions.HandleInputs(_positionInActionList);
		}
	}

	//Additional effects if a jump is being made from in the air.
	private void JumpInAir () {

		//Take some horizontal speed on jump and remove vertical speed to ensure jump has an upwards force.
		Vector3 newVec;
		if (_PlayerPhys._RB.velocity.y > 10)
			newVec = new Vector3(_PlayerPhys._RB.velocity.x * _speedLossOnDoubleJump_, _PlayerPhys._RB.velocity.y, _PlayerPhys._RB.velocity.z * _speedLossOnDoubleJump_);
		else
			newVec = new Vector3(_PlayerPhys._RB.velocity.x * _speedLossOnDoubleJump_, 0, _PlayerPhys._RB.velocity.z * _speedLossOnDoubleJump_);
		_PlayerPhys.SetCoreVelocity(newVec, false, true);

		//Add particle effect during jump
		GameObject JumpDashParticleClone = Instantiate(_Tools.JumpDashParticle, _Tools.FeetPoint.position, Quaternion.identity) as GameObject;
		JumpDashParticleClone.transform.position = _Tools.FeetPoint.position;
		JumpDashParticleClone.transform.rotation = Quaternion.LookRotation(Vector3.up);

	}

	private void ApplyForce() {
		//Ending Jump Early
		if (!_Input.JumpPressed && _counter > _minJumpTime_ && _isJumping)
		{
			_counter = _thisJumpDuration;
			_isJumping = false;
		}
		//Ending jump after max duration
		else if (_counter > _thisJumpDuration && _isJumping && _Input.JumpPressed)
		{
			_counter = _thisJumpDuration;
			_isJumping = false;
			_Input.JumpPressed = false;
		}
		//Add Jump Speed
		else if (_isJumping)
		{
			//Jump move at angle
			if (_counter < _slopedJumpDuration && _jumpSlopeSpeed > 0)
			{
				_PlayerPhys.AddCoreVelocity(_upwardsDirection * (_jumpSlopeSpeed * 0.75f), false);
				_PlayerPhys.AddCoreVelocity(Vector3.up * (_jumpSlopeSpeed * 0.25f), false); //Extra speed to ballance out direction
			}
			//Move straight up in world.
			else
			{
				_PlayerPhys.AddCoreVelocity(Vector3.up * (_thisJumpSpeed), false);
			}
		}
	}

	private void CheckShouldEndAction() {
		//End Action on landing. Has to have been in the air for some time first though to prevent immediately becoming grounded.
		if (_PlayerPhys._isGrounded && _counter > _slopedJumpDuration)
		{ 

			//Prevents holding jump to keep doing so forever.
			_Input.JumpPressed = false;

			_Actions.ActionDefault.StartAction();
		}
	}

	#endregion

	/// <summary>
	/// Public ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region public 

	#endregion

	/// <summary>
	/// Assigning ----------------------------------------------------------------------------------
	/// </summary>
	#region Assigning

	public void ReadyAction () {
		if (_PlayerPhys == null)
		{
			//Assign all external values needed for gameplay.
			_Tools = GetComponent<S_CharacterTools>();
			AssignTools();
			AssignStats();

			//Get this actions placement in the action manager list, so it can be referenced to acquire its connected actions.
			for (int i = 0 ; i < _Actions._MainActions.Count ; i++)
			{
				if (_Actions._MainActions[i].State == S_Enums.PrimaryPlayerStates.Default)
				{
					_positionInActionList = i;
					break;
				}
			}
		}
	}

	//Responsible for assigning objects and components from the tools script.
	private void AssignTools () {
		_PlayerPhys = GetComponent<S_PlayerPhysics>();
		_Actions = GetComponent<S_ActionManager>();
		_CamHandler = GetComponent<S_Handler_Camera>();
		_Input = GetComponent<S_PlayerInput>();

		_CharacterAnimator = _Tools.CharacterAnimator;
		_Sounds = _Tools.SoundControl;
		_JumpBall = _Tools.JumpBall;

	}

	//Responsible for assigning stats from the stats script.
	private void AssignStats () {
		_maxJumpTime_ = _Tools.Stats.JumpStats.startJumpDuration.y;
		_minJumpTime_ = _Tools.Stats.JumpStats.startJumpDuration.x;
		_startJumpSpeed_ = _Tools.Stats.JumpStats.startJumpSpeed;
		_startSlopedJumpDuration_ = _Tools.Stats.JumpStats.startSlopedJumpDuration;
		_jumpSlopeConversion_ = _Tools.Stats.JumpStats.jumpSlopeConversion;
		_stopYSpeedOnRelease_ = _Tools.Stats.JumpStats.stopYSpeedOnRelease;

		_maxJumps_ = _Tools.Stats.MultipleJumpStats.maxJumpCount;
		_doubleJumpDuration_ = _Tools.Stats.MultipleJumpStats.doubleJumpDuration;
		_doubleJumpSpeed_ = _Tools.Stats.MultipleJumpStats.doubleJumpSpeed;

		_speedLossOnDoubleJump_ = _Tools.Stats.MultipleJumpStats.speedLossOnDoubleJump;
	}
	#endregion
}

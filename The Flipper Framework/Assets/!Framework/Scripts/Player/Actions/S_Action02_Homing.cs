﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using UnityEditor;
using System.Threading;

[RequireComponent(typeof(S_ActionManager))]
[RequireComponent(typeof(S_Handler_HomingAttack))]
public class S_Action02_Homing : MonoBehaviour, IMainAction
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
	private S_VolumeTrailRenderer _HomingTrailScript;
	private S_Handler_HomingAttack _HomingControl;
	private S_Control_PlayerSound  _Sounds;

	private GameObject            _HomingTrailContainer;
	private GameObject            _JumpBall;
	private Animator               _CharacterAnimator;
	private Transform               _Skin;
	[HideInInspector]
	public Transform              _Target;
	#endregion

	//General
	#region General Properties

	//Stats
	#region Stats
	private float       _homingAttackSpeed_;
	private bool        _canBeControlled_;
	private float       _homingTimerLimit_;
	private float       _homingTurnSpeed_;
	private bool        _CanBePerformedOnGround_;
	private int         _homingSkidAngleStartPoint_;
	private int	_homingDeceleration_;
	private int         _homingAcceleration_;
	private float       _homingBouncingPower_;
	private int         _minSpeedGainOnHit_;
	private float       _lerpToPreviousDirection_;
	private float       _lerpToNewInput_;
	private int         _maxHomingSpeed_;
	private int         _minHomingSpeed_;
	#endregion

	// Trackers
	#region trackers
	private int         _positionInActionList;

	public float        _skinRotationSpeed;

	private bool        _isHoming;

	private float       _speedBeforeAttack;
	private Vector3     _directionBeforeAttack;
	private float       _currentSpeed;
	private float       _speedAtStart;

	[HideInInspector]
	public Vector3      _targetDirection;             //Set at the start of the action to be used by other scripts on hit.
	private float       _distanceFromTarget;
	private Vector3     _currentDirection;
	private Vector3     _horizontalDirection;
	private Vector3     _currentInput;
	private float       _inputAngle;

	private float       _timer;
	#endregion

	#endregion
	#endregion

	/// <summary>
	/// Inherited ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region Inherited

	// Called when the script is enabled, but will only assign the tools and stats on the first time.
	private void OnEnable () {
		ReadyAction();
		_JumpBall.SetActive(false);
	}

	private void OnDisable () {
		_timer = 0;
		_HomingTrailContainer.transform.DetachChildren();
		_Actions._isHomingAvailable = true;

		//Removes one count of control being locked to counteract one being added when this action is start. This can also be done elsewhere but that will disable _isHoming
		if (_PlayerPhys._listOfCanControl.Count > 0 && _isHoming)
		{
			_PlayerPhys._listOfCanControl.RemoveAt(0);
		}
	}

	// Update is called once per frame
	void Update () {

		if (_isHoming)
		{
			//Set Animator Parameters
			_Actions.Action00.HandleAnimator(1);

			//Set Animation Angle
			_Actions.Action00.SetSkinRotationToVelocity(_skinRotationSpeed);

			HandleInputs();
		}
	}

	private void FixedUpdate () {
		if (_isHoming)
		{
			HomeInOnTarget();
		}
	}

	//Called when checking if this action is to be performed, including inputs.
	public bool AttemptAction () {

		//Depending on stats, this can only be performed when grounded.
		if (!_PlayerPhys._isGrounded || _CanBePerformedOnGround_)
		{
			//Must have a valid target.
			if (_HomingControl._HasTarget && _HomingControl._TargetObject && _Input.HomingPressed)
			{
				//Do a homing attack
				if (_HomingControl._delayCounter <= 0 && _Actions._isHomingAvailable)
				{
					StartAction();
					return true;
				}
			}
		}
		return false;
	}

	public void StartAction () {

		ReadyAction();

		//Setting private
		_isHoming = true;
		_inputAngle = 0; //The difference between movement direction and input

		_timer = 0;
		_speedBeforeAttack = _PlayerPhys._horizontalSpeedMagnitude; //Saved so it can be called back to on hit or end of action.
		_directionBeforeAttack = _PlayerPhys._RB.velocity.normalized;

		//Gets the direction to move in, rotate a lot faster than normal for the first frame.
		_Target = _HomingControl._TargetObject.transform;
		_targetDirection = _Target.position - transform.position;
		_currentDirection = Vector3.RotateTowards(_Skin.forward, _targetDirection, Mathf.Deg2Rad * _homingTurnSpeed_ * 3, 0.0f);

		//Setting public
		_PlayerPhys._isGravityOn = false;
		_JumpBall.SetActive(false);
		_Actions._isHomingAvailable = false;
		_PlayerPhys._listOfCanControl.Add(false);
		_Input.JumpPressed = false;

		//Effects
		_CharacterAnimator.SetInteger("Action", 1);
		_CharacterAnimator.SetTrigger("ChangedState");
		_Sounds.HomingAttackSound();
		_HomingTrailScript.emitTime = _homingTimerLimit_ + 0.06f;
		_HomingTrailScript.emit = true;

		//Get speed of attack and speed to return to on hit.		
		_speedAtStart = Mathf.Max(_speedBeforeAttack * 0.9f, _homingAttackSpeed_);
		_speedAtStart = Mathf.Min(_speedAtStart, _maxHomingSpeed_);
		_currentSpeed = _speedAtStart;
		

		_speedBeforeAttack = Mathf.Max(_speedBeforeAttack, _minSpeedGainOnHit_);

		_Actions.ChangeAction(S_Enums.PrimaryPlayerStates.Homing);
	}

	public void StopAction () {
		enabled = false;
	}

	#endregion

	/// <summary>
	/// Private ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region private

	//Handles player movement towards the target. Exits state if appropriate.
	private void HomeInOnTarget () {
		_timer += Time.deltaTime;

		//Ends homing attack if in air for too long or target is lost
		if ((_Target == null || _timer > _homingTimerLimit_))
		{
			_Actions.Action00.StartAction();
			return;
		}

		//Get direction to move in.
		Vector3 newDirection = _Target.position - transform.position;
		_distanceFromTarget = Vector3.Distance(_Target.position, transform.position);
		float thisTurn =  _homingTurnSpeed_;

		//Set Player location when close enough, for precision.
		if (_distanceFromTarget < (_currentSpeed * Time.fixedDeltaTime))
		{
			transform.position = _Target.transform.position;
			return;
		}
		//If something is blocking the way, bounce off it.
		else if (Physics.Raycast(transform.position, newDirection, out RaycastHit hit, ((_currentSpeed / 10) + 1) * 2, _PlayerPhys._Groundmask_))
		{
			StartCoroutine(HittingObstacle(hit.normal));
		}
		//Turn faster when close to target and fast to make missing very hard.
		else
		{
			if (_distanceFromTarget < 40)
				thisTurn *= 2f;
			if (_currentSpeed > 90)
				thisTurn *= 1.3f;
		}

		//If there is input, then alter direction slightly to left or right.
		if (_PlayerPhys._moveInput.sqrMagnitude > 0.2f && _canBeControlled_ && _timer > 0.02f)
		{
			//Get horizontal input
			_currentInput = _PlayerPhys._moveInput;
			_currentInput.y = 0;

			//Get current horizontal direction
			_horizontalDirection = newDirection;
			float rememberY = _horizontalDirection.y;
			_horizontalDirection.y = 0;

			_inputAngle = Vector3.Angle(_horizontalDirection, _currentInput);

			//Will only add control if input is not pointing directily behind character as that will lead to zigzagging
			if (_inputAngle < 130) 
			{
				//Limit how different the input can be to the move direction (no more than x degrees).
				Vector3 useInput = Vector3.RotateTowards(_horizontalDirection, _currentInput, Mathf.Deg2Rad * 80, 0);

				//Get a horizontal direction between the two but don't change vertical.
				float percentageRelevantDif = Vector3.Angle(_horizontalDirection, useInput) * 0.8f;
				if (_distanceFromTarget < 40)
				{
					percentageRelevantDif *= 0.3f;
				}

				//A lerp would go through 0, while rotating by difference means it goes outwards without losing magnitude.
				Vector3 temp = Vector3.RotateTowards(_horizontalDirection, useInput, Mathf.Deg2Rad * percentageRelevantDif, 0);

				temp.y = rememberY;
				newDirection = temp.normalized;
			}
		}

		_currentDirection = Vector3.RotateTowards(_currentDirection, newDirection, Mathf.Deg2Rad * thisTurn, 0.0f);
		_PlayerPhys.SetCoreVelocity(_currentDirection * _currentSpeed);
	}

	public void HandleInputs () {
		if (!_Actions.isPaused)
		{
			//Action Manager goes through all of the potential action this action can enter and checks if they are to be entered
			_Actions.HandleInputs(_positionInActionList);
		}
	}

	#endregion

	/// <summary>
	/// Public ----------------------------------------------------------------------------------
	/// </summary>
	/// 
	#region public 

	//What happens to the character after they hit a target, the directions they bounce based on input, stats and target.
	public void HittingTarget (S_Enums.HomingRebounding whatRebound) {

		_CharacterAnimator.SetInteger("Action", 1);

		AddDelay();
		Vector3 newSpeed = Vector3.zero;

		switch(whatRebound)
		{
			case S_Enums.HomingRebounding.BounceThrough:
				if (_Input.HomingPressed) { additiveHit(); }
				else { bounceUpHit(); }
				_PlayerPhys.SetCoreVelocity(newSpeed);
				break;
			case S_Enums.HomingRebounding.Rebound:
				StartCoroutine(HittingObstacle());
				return;
			case S_Enums.HomingRebounding.bounceOff: 
				bounceUpHit(); 
				break;
		}

		//An additive hit that keeps momentum
		void additiveHit () {
			//Disable inputs so actions don't happen immediately after hitting
			_Input.SpecialPressed = false;
			_Input.HomingPressed = false;

			GetDirectionPostHit();

			//Send player in new horizontal direction by speed before attack, but vertical speed is determined by bounce power.
			newSpeed.y = 0;
			newSpeed.Normalize();
			newSpeed *= Mathf.Min(_speedBeforeAttack, _currentSpeed);
			newSpeed.y = _homingBouncingPower_;

		}

		void bounceUpHit()
			{
			GetDirectionPostHit();
			newSpeed.y = 0;
			newSpeed.Normalize();
			newSpeed *= 3;
			newSpeed.y = _homingBouncingPower_; ;
		}

		void GetDirectionPostHit() {
			//Get current movement direction
			newSpeed = _PlayerPhys._RB.velocity.normalized;

			//If trying to move in the direction taken by the attack at the end, then will move that way
			if(Vector3.Angle(newSpeed, _PlayerPhys._moveInput) / 180 < _lerpToNewInput_)
			{
				//Rotate towards new input by percentage
				float partDifference = Vector3.Angle(newSpeed, _PlayerPhys._moveInput) * _lerpToNewInput_;
				newSpeed = Vector3.RotateTowards(newSpeed, _PlayerPhys._moveInput, partDifference * Mathf.Deg2Rad, 0);
			}
			//otherwise will move in previous direction.
			else
			{
				//Rotate towards previous direction by percentage
				float partDifference = Vector3.Angle(newSpeed, _directionBeforeAttack) * _lerpToPreviousDirection_;
				newSpeed = Vector3.RotateTowards(newSpeed, _directionBeforeAttack, partDifference * Mathf.Deg2Rad, 0);
			}
		}
	}

	//Applies knockback and a temporary locked state
	public IEnumerator HittingObstacle ( Vector3 wallNormal = default(Vector3), float force = 25 ) {

		float duration = 0.6f * 55;

		_isHoming = false; //Prevents the rest of the code in Update and FixedUpdate from happening.

		//Gets a direction to make the player face and rebound away from. This is either the way they were already going, or slightly affected by what they hit.
		Vector3 faceDirection = _PlayerPhys._RB.velocity.normalized;
		if (wallNormal != default(Vector3))
		{
			faceDirection = Vector3.Lerp(faceDirection, wallNormal, 0.5f);
		}

		_PlayerPhys.SetTotalVelocity(Vector3.up * 2, true);
		yield return new WaitForFixedUpdate();//For optimisation, freezes movement for a bit before applying the new physics.
		yield return new WaitForFixedUpdate();//For optimisation, freezes movement for a bit before applying the new physics.

		//Bounce backwards and upwards (halfway between the two).
		Vector3 ReboundDirection = new Vector3(-faceDirection.x, 0.8f, -faceDirection.z);
		_PlayerPhys.AddCoreVelocity(ReboundDirection * force, true);

		for (int i = 0 ; i < duration * 0.2f && !_PlayerPhys._isGrounded ; i++)
		{
			//Rotation
			_Skin.rotation = Quaternion.LookRotation(faceDirection, transform.up);

			yield return new WaitForFixedUpdate();
		}

		//Returns control partway through the rebound.
		_PlayerPhys._isGravityOn = true;
		if (enabled) {_PlayerPhys._listOfCanControl.RemoveAt(0); }

		for (int i = 0 ; i < duration * 0.8f && !_PlayerPhys._isGrounded ; i++)
		{
			//Rotation
			_Skin.rotation = Quaternion.LookRotation(faceDirection, transform.up);

			yield return new WaitForFixedUpdate();
		}

		_Actions.Action00.StartAction();
	}

	//Called only by the skid subaction script, and only if this state is stet to have skidding as a subaction.
	public bool TryHomingSkid () {
		//Different start point from the other two skid types.
		if (_inputAngle > _homingSkidAngleStartPoint_ && !_Input._isInputLocked)
		{
			_currentSpeed -= _homingDeceleration_ * Time.deltaTime;
			_currentSpeed = Mathf.Clamp(_currentSpeed, Mathf.Max(_minHomingSpeed_, 20), _speedAtStart);
			return true;
		}
		else if (_inputAngle < 40 && !_Input._isInputLocked)
		{
			_currentSpeed += _homingAcceleration_ * Time.deltaTime;
			_currentSpeed = Mathf.Clamp(_currentSpeed, 0, _speedAtStart);
		}
		return false;
	}

	//Called upon successful attacks to set the counter (which will tick down when above 0)
	public void AddDelay() {
		_HomingControl._delayCounter = _HomingControl._homingDelay_;
	}

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
		_Input =		GetComponent<S_PlayerInput>();
		_PlayerPhys =	GetComponent<S_PlayerPhysics>();
		_Actions =	GetComponent<S_ActionManager>();
		_HomingControl =	GetComponent<S_Handler_HomingAttack>();
		_Sounds =		_Tools.SoundControl;
		_Skin =		_Tools.mainSkin;

		_CharacterAnimator =	_Tools.CharacterAnimator;
		_HomingTrailScript =	_Tools.HomingTrailScript;
		_HomingTrailContainer =	_Tools.HomingTrailContainer;
		_JumpBall =		_Tools.JumpBall;
	}

	//Reponsible for assigning stats from the stats script.
	private void AssignStats () {
		_homingAttackSpeed_ =	_Tools.Stats.HomingStats.attackSpeed;
		_homingTimerLimit_ =	_Tools.Stats.HomingStats.timerLimit;
		_CanBePerformedOnGround_ =	_Tools.Stats.HomingStats.canBePerformedOnGround;
		_homingTurnSpeed_ =		_Tools.Stats.HomingStats.turnSpeed;
		_homingSkidAngleStartPoint_ = _Tools.Stats.SkiddingStats.angleToPerformHomingSkid;
		_canBeControlled_ =		_Tools.Stats.HomingStats.canBeControlled;
		_homingBouncingPower_ =	_Tools.Stats.EnemyInteraction.homingBouncingPower;
		_minSpeedGainOnHit_ =	_Tools.Stats.HomingStats.minimumSpeedOnHit;
		_lerpToPreviousDirection_ =	_Tools.Stats.HomingStats.lerpToPreviousDirectionOnHit;
		_lerpToNewInput_ =		_Tools.Stats.HomingStats.lerpToNewInputOnHit;
		_maxHomingSpeed_ =		_Tools.Stats.HomingStats.maximumSpeed;
		_homingDeceleration_ =	_Tools.Stats.HomingStats.deceleration;
		_homingAcceleration_ =	_Tools.Stats.HomingStats.acceleration;
		_minHomingSpeed_ =		_Tools.Stats.HomingStats.minimumSpeed;
}
	#endregion


}

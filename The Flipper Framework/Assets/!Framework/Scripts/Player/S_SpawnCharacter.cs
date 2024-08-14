﻿using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S_SpawnCharacter : MonoBehaviour {

	[SerializeField] 
	private GameObject	_DefaultCharacter;
	public CinemachineBrain _CameraBrain;
	public int	_spawnDelay = 5;

	private GameObject            _CharacterToSpawn;
	public static Transform _SpawnedPlayer;

	public S_DeactivateOnStart[] _ListOfDeactivationsToDelay;

	// Use this for initialization
	void Awake () {
		//Some object shouldn't deactivate until the player is spawned in (like the start camera).
		for(int i  = 0; i < _ListOfDeactivationsToDelay.Length; i++)
		{
			_ListOfDeactivationsToDelay[i]._delayInSeconds = (_spawnDelay + 1) * Time.fixedDeltaTime;
		}
		StartCoroutine(Spawn(_spawnDelay));
	}
	
	IEnumerator Spawn(int delay)
    {
		//Dont spawn until enough frames have passed.
		for (int i = 0 ; i < _spawnDelay ; i++)
		{
			yield return new WaitForFixedUpdate();
		}

		if (GameObject.Find("CharacterSelector") != null)
		{
			_CharacterToSpawn = GameObject.Find("CharacterSelector").GetComponent<S_CharacterSelect>().DesiredCharacter;
		}
		else
		{
			_CharacterToSpawn = _DefaultCharacter;
		}

		GameObject Player = Instantiate(_CharacterToSpawn, transform.position, Quaternion.identity, transform);

		S_CharacterTools Tools = Player.GetComponentInChildren<S_CharacterTools>();
		Tools.MainCamera = _CameraBrain;

		_SpawnedPlayer = Tools.transform;

		yield return null;
	}
}

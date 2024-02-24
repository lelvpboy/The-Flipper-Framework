﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S_SpawnCharacter : MonoBehaviour {

	[SerializeField] private GameObject PlayerObject;

	// Use this for initialization
	void Awake () {

		StartCoroutine(Spawn());
	}
	
	IEnumerator Spawn()
    {
		if (GameObject.Find("CharacterSelector") != null)
		{
			PlayerObject = GameObject.Find("CharacterSelector").GetComponent<S_CharacterSelect>().DesiredCharacter;
		}
		GameObject Player = Instantiate(PlayerObject, transform.position, Quaternion.identity, transform);
		//Player.transform.position = transform.position;

		yield return null;

		//Player.GetComponentInChildren<S_CharacterTools>().CharacterAnimator.transform.forward = transform.forward;
		//Player.transform.forward = transform.forward;
		//if (GameObject.Find("CharacterSelector") != null)
		//{
		//	Destroy(GameObject.Find("CharacterSelector"));
		//}
	}

	// Update is called once per frame
	void Update () {
		
	}
}

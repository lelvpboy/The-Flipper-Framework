using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S_Data_Base : MonoBehaviour
{
	public event        EventHandler onObjectValidate;
	private void OnValidate () {
		if(onObjectValidate != null)
			onObjectValidate.Invoke(null, null);
	}
}

﻿using UnityEngine;
using System.Collections;

public class S_RingGiverControl : MonoBehaviour {

    public int Rings = 10;
    public float Duration = 1;
    float counter;
    bool hasadded = false;

	void Update () {

        counter += Time.deltaTime;
        if(counter > Duration)
        {
            if (!hasadded)
            {
                hasadded = true;
                S_Interaction_Objects.RingAmount += Rings;
                GetComponent<AudioSource>().Play();
            }
        }
        if(counter > Duration + 2f)
        {
            Destroy(gameObject);
        }

	}
}

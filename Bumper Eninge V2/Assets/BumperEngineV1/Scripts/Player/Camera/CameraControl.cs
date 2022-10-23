﻿using UnityEngine;
using Cinemachine;
using System.Collections;

public class CameraControl : MonoBehaviour {

    public HedgeCamera Cam;
    public CinemachineVirtualCamera virtCam;
    [HideInInspector] public float InitialDistance;

    void Awake()
    {
        InitialDistance = Cam.CameraMaxDistance;
    }

	public void OnTriggerEnter(Collider col)
    {
        if(col.tag == "CameraTrigger")
        {
            if(col.GetComponent<CameraTriggerData>() != null)
            {
                if (col.GetComponent<CameraTriggerData>().Type == TriggerType.LockToDirection)
                {
                    Vector3 dir = col.transform.forward;
                    Cam.SetCamera(dir, 2f, col.GetComponent<CameraTriggerData>().CameraAltitude, col.GetComponent<CameraTriggerData>().FaceSpeed);
                    Cam.Locked = true;
                    if (col.GetComponent<CameraTriggerData>().changeDistance)
                    {
                        Cam.CameraMaxDistance = col.GetComponent<CameraTriggerData>().ChangeDistance;
                    }
                    else
                    {
                        Cam.CameraMaxDistance = InitialDistance;
                    }
                }
                else if (col.GetComponent<CameraTriggerData>().Type == TriggerType.SetFree)
                {
                    Cam.CameraMaxDistance = InitialDistance;
                    Cam.Locked = false;
                }
                else if (col.GetComponent<CameraTriggerData>().Type == TriggerType.SetFreeAndLookTowards)
                {
                    Vector3 dir = col.transform.forward;
                    Cam.SetCamera(dir, 2.5f, col.GetComponent<CameraTriggerData>().CameraAltitude, col.GetComponent<CameraTriggerData>().FaceSpeed);
                    if (!col.GetComponent<CameraTriggerData>().changeDistance)
                    {
                        Cam.CameraMaxDistance = InitialDistance;
                    }
                    else
                    {
                        Cam.CameraMaxDistance = col.GetComponent<CameraTriggerData>().ChangeDistance;
                    }
                    Cam.Locked = false;
                }
            }
        }

        if (col.tag == "Rail")
        {
            Cam.Locked = true;
        }

    }

	public void OnTriggerExit(Collider col)
	{
		if (col.tag == "CameraTrigger") {
			if (col.GetComponent<CameraTriggerData> () != null) {
				if (col.GetComponent<CameraTriggerData> ().Type == TriggerType.LockToDirection && col.GetComponent<CameraTriggerData> ().ReleaseOnExit) {
					Cam.CameraMaxDistance = InitialDistance;
					Cam.Locked = false;
					Vector3 dir = col.transform.forward;
					Cam.SetCamera(dir, 2.5f, col.GetComponent<CameraTriggerData>().CameraAltitude);
				}

                else if(col.GetComponent<CameraTriggerData>().ReleaseOnExit)
                {
                    Cam.CameraMaxDistance = InitialDistance;
                }
			}
		}
	}

}

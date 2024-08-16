﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplineMesh
{
	/// <summary>
	/// Example of component to places assets along a spline. This component can be used as-is but will most likely be a base for your own component.
	/// 
	/// In this example, the user gives the prefab to place, a spacing value between two placements, the prefab scale and an horizontal offset to the spline.
	/// These three last values have an additional range, allowing to add some randomness. for each placement, the computed value will be between value and value+range.
	/// 
	/// Prefabs are placed from the start of the spline at computed spacing, unitl there is no lentgh remaining. Prefabs are stored, destroyed
	/// and built again each time the spline or one of its curves change.
	/// 
	/// A random seed is used to obtain the same random numbers at each update. The user can specify the seed to test some other random number set.
	/// 
	/// Place prefab along a spline and deform it easily have a lot of usages if you have some imagination : 
	///  - place trees along a road
	///  - create a rocky bridge
	///  - create a footstep track with decals
	///  - create a path of firefly in the dark
	///  - create a natural wall with overlapping rocks
	///  - etc.
	/// </summary>
	[RequireComponent(typeof(S_Data_objectOnSpline))]
	[ExecuteInEditMode]
	[SelectionBase]
	[DisallowMultipleComponent]
	public class S_PlaceOnSpline : MonoBehaviour
	{
		private GameObject generated;
		private Spline _Spline = null;
		private bool toUpdate = true;

		public bool UpdateThis;

		[Header("Placement")]
		public bool justStart = true;
		public bool onlyOne = false;
		public bool none = false;
		public bool onEnd;

		[Header("Object")]
		public GameObject prefab = null;
		public bool asPrefab = true;

		[Header("Transform")]
		public float scale = 1, scaleRange = 0;
		public float spacing = 20, spacingRange = 0;
		public float offset = 0, offsetRange = 0;
		public Vector3 Offset3d, offsetRotation;

		[Header("on Spline")]
		public float initialSpacing = 0f;
		public float EndingSpacing = 0f;
		public bool isRandomYaw = false;
		public int randomSeed = 0;
		[Space]
		public bool alingwithterrain = false;

#if UNITY_EDITOR
		private void OnEnable () {
			CheckNow();
		}

		void CheckNow () {
			string generatedName = "generated by " + GetType().Name;
			var generatedTranform = transform.Find(generatedName);
			generated = generatedTranform != null ? generatedTranform.gameObject : UOUtility.Create(generatedName, gameObject);

			_Spline = GetComponentInParent<Spline>();
			_Spline.NodeListChanged += ( s, e ) =>
			{
				toUpdate = true;
				foreach (CubicBezierCurve curve in _Spline.GetCurves())
				{
					curve.Changed.AddListener(() => toUpdate = true);
				}
			};
			foreach (CubicBezierCurve curve in _Spline.GetCurves())
			{
				curve.Changed.AddListener(() => toUpdate = true);
			}
		}

		private void OnValidate () {
			toUpdate = true;
		}

		private void Update () {
			if (UpdateThis)
			{
				CheckNow();
				UpdateThis = false;
			}

			if (toUpdate)
			{
				Sow();
				toUpdate = false;
			}
		}

		public void Sow () {
			UOUtility.DestroyChildren(generated);


			UnityEngine.Random.InitState(randomSeed);
			if (spacing + spacingRange <= 0 ||
			    prefab == null)
				return;

			float distance = initialSpacing;

			if (onEnd)
			{
				placeElement(_Spline.Length);
			}

			if (none)
				return;

			else if (justStart)
			{
				placeElement(1);
			}
			else if (onlyOne)
			{
				placeElement(initialSpacing);
			}
			else
			{
				while (distance <= (_Spline.Length - EndingSpacing))
				{
					placeElement(distance);
					distance += spacing + UnityEngine.Random.Range(0, spacingRange);
				}
			}

		}

		void placeElement ( float distance ) {
			CurveSample sample = _Spline.GetSampleAtDistance(distance);

			GameObject go;
			if (!asPrefab)
			{
				go = Instantiate(prefab, generated.transform);
			}
			else
			{
#if UNITY_EDITOR
				go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#else
                    go = Instantiate(prefab, generated.transform);
#endif
				go.transform.parent = generated.transform;
			}
			go.transform.localRotation = Quaternion.identity;
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one;

			// move along spline, according to spacing + random
			go.transform.position = transform.position + (transform.rotation * sample.location);
			// apply scale + random
			float rangedScale = scale + UnityEngine.Random.Range(0, scaleRange);
			go.transform.localScale = new Vector3(rangedScale, rangedScale, rangedScale);
			// rotate with random yaw
			if (isRandomYaw)
			{
				go.transform.Rotate(0, 0, UnityEngine.Random.Range(-180, 180));
			}
			else
			{
				go.transform.rotation = transform.rotation * sample.Rotation * Quaternion.Euler(offsetRotation);
			}
			// move orthogonaly to the spline, according to offset + random
			Vector3 binormal = sample.tangent;
			binormal = Quaternion.LookRotation(Vector3.right, Vector3.up) * binormal;
			var localOffset = offset + UnityEngine.Random.Range(0, offsetRange * Math.Sign(offset));
			localOffset *= sample.scale.x;
			binormal *= localOffset;
			binormal += transform.rotation * sample.Rotation * Offset3d;
			go.transform.position += binormal;


			if (GetComponent<S_Data_objectOnSpline>())
			{
				GetComponent<S_Data_objectOnSpline>().affectObject(go);
			}

			if (alingwithterrain) GroundAlign(go.transform);
		}

		void GroundAlign ( Transform obj ) {
			RaycastHit hit;
			Ray ray = new Ray(obj.position, Vector3.down);
			ray = new Ray(obj.position, -obj.up);
			if (Physics.Raycast(ray, out hit))
			{
				obj.position = hit.point;
				obj.rotation = Quaternion.FromToRotation(obj.up, hit.normal) * obj.rotation;
				obj.position += Offset3d;
				//Debug.Log(obj.name + " aligned.", this);
			}
			else
			{
				//Debug.Log("No surface found for " + obj.name,this);
			}
		}
#endif
	}
}
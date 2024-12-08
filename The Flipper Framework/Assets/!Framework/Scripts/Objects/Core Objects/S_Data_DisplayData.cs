using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using TMPro;
using System.ComponentModel;

#if UNITY_EDITOR
[ExecuteAlways]
public class S_Data_DisplayData : MonoBehaviour
{

	[SerializeField]
	private bool	_updateAutomatically;
	[SerializeField]
	private bool	_onlyDisplayWhenSelected;
	private bool	_previousOnlyDisplayWhenSelected;
	[SerializeField]
	private bool        _updateTransform = true;
	[SerializeField]
	private Vector3     _placeAboveObject;
	private Vector3     _previousLocalPosition;
	[SerializeField]
	private float	_scale = 1;

	[SerializeField]
	private string _displayTitle;

	[SerializeField]
	private TextMeshPro _3DTitle;
	[SerializeField]
	private TextMeshPro _3DText;

	[SerializeReference]
	public GameObject[]		_ObjectsToReference;
	private List<S_Data_Base>	_DataSources = new List<S_Data_Base>();

	[Serializable]
	public struct StrucDataToDisplay {
		public string variableName;
		public string displayName;
		public S_EditorEnums.CasingTypes casing;
		public string structName;
		[ReadOnly(true)]
		public string value;
	}
	public List<StrucDataToDisplay> _DataToDisplay;


	private bool _isSelected;

	private void Update () {
		if(_isSelected)
		{
			if (transform.localPosition != _previousLocalPosition && _placeAboveObject != Vector3.zero)
			{
				_placeAboveObject = transform.position - transform.parent.position;
			}
			if(_updateTransform) HandleTransform();
		}
	}

	#region SelectionManagement
	//These methods will handle whether or not text should be hidden if set to only appear when selected.
	private void OnEnable () {
		Selection.selectionChanged += OnSelectionChanged;
		HandleTransform();
	}
	private void OnDisable () {
		Selection.selectionChanged -= OnSelectionChanged;
	}
	private void OnSelectionChanged () {
		//If a reference object or this is selected, reveal the text.
		if (S_S_EditorMethods.IsThisOrReferenceSelected(transform, _ObjectsToReference))
		{
			_isSelected = true;
			RevealOrHide(true);
			return;
		}

		//If none are, hide the text.
		_isSelected = false;
		RevealOrHide(false);
	}


	private void RevealOrHide(bool visible ) {
		if (!_onlyDisplayWhenSelected) { return; }

		_3DTitle.gameObject.SetActive(visible);
		_3DText.gameObject.SetActive(visible);
	}
	#endregion

	//Called whenever a property is updated
	private void OnValidate () {
		Validate(null, null);
	}

	public void Validate ( object sender, EventArgs e ) {

		if (!_updateAutomatically) { return; }

		if (!_onlyDisplayWhenSelected || _isSelected)
		{
			UpdateData();
		}
	}

	public void UpdateData () {
		GetDataSources();

		for (int i = 0 ; i < _DataToDisplay.Count ; i++)
		{
			StrucDataToDisplay ThisData = _DataToDisplay[i];

			//Ensures the name taken in from a human matches code style, so it can find a field.
			string translatedVariableName = S_S_EditorMethods.TranslateStringToVariableName(ThisData.variableName, ThisData.casing);
			if (translatedVariableName == "") continue;

			string translatedStructName = ThisData.structName == "" ? "" : S_S_EditorMethods.TranslateStringToVariableName(ThisData.structName, S_EditorEnums.CasingTypes.PascalCase);

			//Goes through each data source until a field matching the given name is found, and returns that value
			object value = null;
			for (int s = 0 ; value == null & s < _DataSources.Count ; s++)
				value = (S_S_EditorMethods.FindFieldByName(_DataSources[s], translatedVariableName, translatedStructName));

			if(value == null) { _updateAutomatically = false; break; }

			string displayValue = value.ToString();
			displayValue = S_S_EditorMethods.CleanBracketsInString(displayValue);

			//Updates the data
			StrucDataToDisplay Temp = new StrucDataToDisplay
			{
				casing = ThisData.casing,
				displayName = ThisData.displayName,
				variableName = translatedVariableName,
				structName = translatedStructName,
				value = displayValue
			};
			_DataToDisplay[i] = Temp;
		}

		Update3DText();
	}

	//Go through each object set in editor, and search for scripts using the dataInterface, then add them to a list to use as sources later.
	public void GetDataSources () {
		HandleValidateEventsOfSources(false);

		_DataSources.Clear(); //Clear first, because getting from an array into a list
		//Finds all data sources in the provided objects, then adds them to this
		for (int i = 0 ; i < _ObjectsToReference.Length ; i++)
		{
			if(_ObjectsToReference[i] == null) { continue;}
			S_Data_Base[] ObjectsDataComponents = _ObjectsToReference[i].GetComponents<S_Data_Base>();
			for (int j = 0 ; j < ObjectsDataComponents.Length ; j++) { _DataSources.Add(ObjectsDataComponents[j]); }
		}

		HandleValidateEventsOfSources(true);
	}

	//To ensure data updates onValidate for source objects, not just itself, events are used. This will remove them before they're added back.
	private void HandleValidateEventsOfSources (bool add) {
		for (int i = 0 ; i < _DataSources.Count ; i++)
		{
			if (add)	_DataSources[i].onObjectValidate += Validate;
			else	_DataSources[i].onObjectValidate -= Validate;
		}
	}

	public void Update3DText () {
		_3DTitle.text = _displayTitle;

		if(_updateTransform) HandleTransform();

		//Goes through each data element, and makes a new line in the text to include display and value.
		string DisplayText = "";
		for (int i = 0 ; i < _DataToDisplay.Count ; i++) {
			string newText = _DataToDisplay[i].displayName;
			DisplayText += newText + " = " + _DataToDisplay[i].value;
			DisplayText += "\n";
		}

		_3DText.text = DisplayText;
	}

	private void HandleTransform () {

		if (_placeAboveObject != Vector3.zero & transform.parent != null)
		{
			transform.position = transform.parent.position + _placeAboveObject;
			_previousLocalPosition = transform.localPosition;
		}

		//Make both text objects face player. Only works if they are children of this script, and this has no rotation.
		S_S_EditorMethods.FaceSceneViewCamera(_3DText.transform, 180); //180 makes them face the other way, as if the Rect transforms faced the player, they'd actually be looking away.
		S_S_EditorMethods.FaceSceneViewCamera(_3DTitle.transform, 180);
		transform.localRotation = Quaternion.identity; //If in line with parent, scaling for children will be as if they have no parents, as this object "resets" it.

		transform.localScale = S_S_ObjectMethods.LockScale(transform, _scale); //Ensures object is never stretched. Cannot rotate this object, else calculations will fail.
	}

	public S_O_CustomInspectorStyle _InspectorTheme;
}


[CustomEditor(typeof(S_Data_DisplayData))]
public class DisplayDataEditor : S_CustomInspector_Base
{
	S_Data_DisplayData _OwnerScript;

	private void OnEnable () {
		//Setting variables
		_OwnerScript = (S_Data_DisplayData)target;
		_InspectorTheme = _OwnerScript._InspectorTheme;

		if (_OwnerScript._InspectorTheme == null) { return; }
		ApplyStyle();
	}

	public override S_O_CustomInspectorStyle GetInspectorStyleFromSerializedObject () {
		return _OwnerScript._InspectorTheme;
	}

	public override void DrawInspectorNotInherited () {
		EditorGUILayout.TextArea("Details.", EditorStyles.textArea);

		EditorGUILayout.Space(_spaceSize); EditorGUILayout.LabelField("Settings", _NormalHeaderStyle);
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_updateAutomatically", "Update Automatically");
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_onlyDisplayWhenSelected", "Only Display When Selected");
		EditorGUILayout.Space(_spaceSize); EditorGUILayout.LabelField("Transform", _NormalHeaderStyle);
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_updateTransform", "Update Transform");
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_scale", "Scale");
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_placeAboveObject", "Place Above Object");
		EditorGUILayout.Space(_spaceSize); EditorGUILayout.LabelField("Object References", _NormalHeaderStyle);
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_ObjectsToReference", "Objects To Reference", false, true);
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_3DTitle", "Title Object");
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_3DText", "Text Object");

		EditorGUILayout.Space(_spaceSize);

		EditorGUILayout.LabelField("Data", _NormalHeaderStyle);
		S_S_CustomInspectorMethods.DrawEditableProperty(serializedObject, "_displayTitle", "Object Title");

		//Add new element button.
		if (S_S_CustomInspectorMethods.IsDrawnButtonPressed(serializedObject, "Manually Update Data", _BigButtonStyle, _OwnerScript, "Update 3D Text"))
		{
			_OwnerScript.UpdateData();
		}

		if (S_S_CustomInspectorMethods.IsDrawnButtonPressed(serializedObject, "Add New Data", _BigButtonStyle, _OwnerScript))
		{
			_OwnerScript._DataToDisplay.Add(new S_Data_DisplayData.StrucDataToDisplay());
		}

		S_S_CustomInspectorMethods.DrawListCustom(serializedObject, "_DataToDisplay", _SmallButtonStyle, _OwnerScript,
			DrawListElementName, DrawWithEachListElement);
	}

		public void DrawListElementName ( int i, SerializedProperty element ) {
		EditorGUILayout.PropertyField(element, new GUIContent("Data " + i + " - " + _OwnerScript._DataToDisplay[i].displayName));
	}

	public void DrawWithEachListElement (int i) {
		return;
	}
}
#endif

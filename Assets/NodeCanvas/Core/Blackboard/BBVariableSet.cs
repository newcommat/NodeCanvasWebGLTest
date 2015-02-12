using UnityEngine;
using System.Collections.Generic;
using System;

namespace NodeCanvas.Variables{

	///A collection of multiple BBVariables
	[Serializable]
	public partial class BBVariableSet{

		[SerializeField]
		private string selectedTypeName = null;
		private BBVariable _selectedBBVariable;
		private Type _selectedType;
		private bool hasInit;

		//value set
		[SerializeField]
		private BBBool boolValue             = new BBBool{value = true};
		[SerializeField]
		private BBFloat floatValue           = new BBFloat();
		[SerializeField]
		private BBInt intValue               = new BBInt();
		[SerializeField]
		private BBString stringValue         = new BBString();
		[SerializeField]
		private BBVector2 vector2Value       = new BBVector2();
		[SerializeField]
		private BBVector vectorValue         = new BBVector();
		[SerializeField]
		private BBQuaternion quaternionValue = new BBQuaternion();
		[SerializeField]
		private BBColor colorValue           = new BBColor();
		[SerializeField]
		private BBAnimationCurve curveValue  = new BBAnimationCurve();
		[SerializeField]
		private BBGameObject goValue         = new BBGameObject();
		[SerializeField]
		private BBComponent componentValue   = new BBComponent();
		[SerializeField]
		private BBObject unityObjectValue    = new BBObject();
		[SerializeField]
		private BBEnum enumValue             = new BBEnum();
		//[SerializeField]
		//private BBVar systemObjectValue      = new BBVar();
		

		partial void AddExtraBBVariablesToSet(List<BBVariable> bbVarSet);

		public Blackboard bb{
			set
			{
				for (int i = 0; i < allVariables.Count; i++)
					allVariables[i].bb = value;
			}
		}

		public bool blackboardOnly{
			set
			{
				for (int i = 0; i < allVariables.Count; i++)
					allVariables[i].blackboardOnly = value;
			}
		}

		private List<BBVariable> allVariables{
			get
			{
				var bbVarSet = new List<BBVariable>{
					boolValue,
					floatValue,
					intValue,
					stringValue,
					vector2Value,
					vectorValue,
					quaternionValue,
					colorValue,
					curveValue,
					goValue,
					componentValue,
					unityObjectValue,
					enumValue
				};

				AddExtraBBVariablesToSet(bbVarSet);
				
				//system object last...
				//bbVarSet.Add(systemObjectValue);
				return bbVarSet;
			}
		}

		public List<Type> availableTypes{
			get
			{
				var typeList = new List<Type>();
				foreach (BBVariable bbVar in allVariables){
					if (bbVar is IMultiCastable)
						typeList.Add( (bbVar as IMultiCastable).baseType );
					typeList.Add(bbVar.varType);
				}
				return typeList;
			}
		}

		public Type selectedType{
			get
			{
				if (_selectedType != null)
					return _selectedType;

				for (int i = 0; i < availableTypes.Count; i++){
					if (selectedTypeName == availableTypes[i].ToString()){
						_selectedType = availableTypes[i];
						break;
					}
				}

				return _selectedType;
			}
			set
			{
				_selectedType = value;
				selectedBBVariable = null;
				selectedTypeName = value != null? value.ToString() : null ;

				foreach (BBVariable bbVar in allVariables){
					var multiCastable = bbVar as IMultiCastable;
					if (multiCastable != null && multiCastable.baseType.NCIsAssignableFrom(value) )
						multiCastable.type = value;
				}
			}
		}

		public BBVariable selectedBBVariable{
			get
			{
				if (hasInit || _selectedBBVariable != null)
					return _selectedBBVariable;

				for (int i = 0; i < allVariables.Count; i++){
					if (allVariables[i].varType == selectedType){
						_selectedBBVariable = allVariables[i];
						break;
					}
				}

				if (Application.isPlaying) hasInit = true;
				return _selectedBBVariable;
			}
			private set
			{
				_selectedBBVariable = value;
				hasInit = false;
			}
		}

		public object objectValue{
			get
			{
				if (selectedBBVariable != null)
					return selectedBBVariable.objectValue;
				return null;
			}
			set
			{
				if (selectedBBVariable != null)
					selectedBBVariable.objectValue = value;
			}
		}

		public override string ToString(){
			if (selectedBBVariable != null)
				return selectedBBVariable.ToString();
			return "Non Initialized VariableSet";
		}
	}
}
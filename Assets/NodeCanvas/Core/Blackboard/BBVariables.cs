using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NodeCanvas.Variables{

	///Marks the BBVariable possible to only pick values from blackboard
	[AttributeUsage(AttributeTargets.Field)]
	public class BlackboardOnlyAttribute : Attribute{
	}

	///Defines a derived type for an IMultiCastable BBVariable
	[AttributeUsage(AttributeTargets.Field)]
	public class VariableType : Attribute{

		public System.Type type;

		public VariableType(System.Type type){
			this.type = type;
		}
	}

	///Denotes that the BBVariable can be set to other derived types of the original contained.
	interface IMultiCastable{
		System.Type baseType{get;}
		System.Type type {get;set;}
	}

	///Base class for Variables that allow linking to a Blackboard variable or specifying one directly.
	[Serializable]
	abstract public class BBVariable{

		[SerializeField][HideInInspector]
		private Blackboard _bb;
		[SerializeField]
		private string _dataName;
		[SerializeField][HideInInspector]
		private VariableData _dataRef;
		[SerializeField]
		private bool _useBlackboard = false;
		[SerializeField][HideInInspector]
		private bool _blackboardOnly = false;
		[SerializeField][HideInInspector]
		private bool _isDynamic;
		//


		///Set the blackboard provided for all BBVariable fields on the object provided
		public static void SetBBFields(Blackboard bb, object o){

			InitBBFields(o);

			foreach (FieldInfo field in o.GetType().NCGetFields()){

				if (typeof(IList).NCIsAssignableFrom(field.FieldType) && !field.FieldType.IsArray){

					var list = field.GetValue(o) as IList;
					if (list == null)
						continue;

                    if (typeof(BBVariable).NCIsAssignableFrom(field.FieldType.NCGetGenericArguments()[0])){
						foreach(BBVariable bbVar in list)
							bbVar.bb = bb;
					}
				}

				if (typeof(BBVariable).NCIsAssignableFrom(field.FieldType))
					(field.GetValue(o) as BBVariable).bb = bb;

				if (typeof(BBVariableSet) == field.FieldType)
					(field.GetValue(o) as BBVariableSet).bb = bb;
			}
		}

		///Check for null bb fields and init them if null on provided object. Also make use of variable attributes
		public static void InitBBFields(object o){
			
			foreach (FieldInfo field in o.GetType().NCGetFields()){

				if (typeof(BBVariable).NCIsAssignableFrom(field.FieldType)){
					if (field.GetValue(o) == null)
						field.SetValue(o, Activator.CreateInstance(field.FieldType));
					if (field.GetCustomAttributes(typeof(BlackboardOnlyAttribute), true ).FirstOrDefault() != null)
						(field.GetValue(o) as BBVariable).blackboardOnly = true;
				}

				if (typeof(IMultiCastable).NCIsAssignableFrom(field.FieldType)){
					var typeAtt = field.GetCustomAttributes(typeof(VariableType), true ).FirstOrDefault() as VariableType;
					if (typeAtt != null)
						(field.GetValue(o) as IMultiCastable).type = typeAtt.type;
				}
			}
		}

		//Used when the BBVariable is set to read/write from the non-local blackboard
		private Blackboard overrideBB{
			get
			{
				if (string.IsNullOrEmpty(dataName) || !dataName.Contains("/"))
					return null;
					
				string prefix = dataName.Substring(0, dataName.IndexOf("/"));
				if (Blackboard.GetGlobalBlackboards().ContainsKey(prefix))
					return Blackboard.GetGlobalBlackboards()[prefix];
				return null;
			}
		}

		//The VariableData object reference if any.One is set after a Read or Write
		private VariableData dataRef{
			get {return _dataRef;}
			set {_dataRef = value;}
		}

		///The blackboard to read/write from.
		public Blackboard bb{
			get {return _bb;}
			set
			{
				if (_bb != value){
					_bb = value;
					dataRef = null;
				}
			}
		}

		//Is the BBVar set to read/write dynamicaly
		public bool isDynamic{
			get {return _isDynamic;}
			set {_isDynamic = value;}
		}

		///The name of the VariableData to read/write from
		public string dataName{
			get
			{
				if (dataRef != null){
					if (!string.IsNullOrEmpty(_dataName) && _dataName.Contains("/"))
						return _dataName.Substring(0, _dataName.IndexOf("/")+1) + dataRef.name;
					return dataRef.name;
				}
				return _dataName;
			}
			set
			{
				if (_dataName != value){
					_dataName = value;
					if (!string.IsNullOrEmpty(value)){
						useBlackboard = true;
						if (overrideBB){
							dataRef = overrideBB.GetData(_dataName.Substring( _dataName.LastIndexOf("/") + 1), varType);
						} else if (bb){
							dataRef = bb.GetData(_dataName, varType);
						}
					} else {
						dataRef = null;
					}
				}
			}
		}

		///Should read/write be allowed only from the blackboard?
		public bool blackboardOnly{
			get { return _blackboardOnly;}
			set { _blackboardOnly = value; if (value == true) useBlackboard = true;}
		}

		///Are we currently reading/writing from the blackboard or the direct assigned value?
		public bool useBlackboard{
			get { return _useBlackboard;}
			set { _useBlackboard = value; if (value == false) dataName = null; }
		}

		///Has the user selected |NONE| in the dropdown?
		public bool isNone{
			get {return useBlackboard && string.IsNullOrEmpty(dataName);}
		}

		///The raw object value
		abstract public object objectValue{get;set;}
		
		///The type of the value that this object holds.
		abstract public Type varType{get;}

		///Is the final value null?
		virtual public bool isNull{
			get {return objectValue == null || objectValue.Equals(null);}
		}

		public override string ToString(){
			UnityEngine.Object uObject = typeof(UnityEngine.Object).NCIsAssignableFrom(varType)? (UnityEngine.Object)objectValue : null;
			return "'<b>" + ( useBlackboard? "$" + dataName : (isNull? "NULL" : (uObject? uObject.name : objectValue.ToString() ) ) ) + "</b>'";
		}

		///Read the specified type from the blackboard
		protected T Read<T>(){

			if (dataRef != null){
				try {return ((VariableData<T>)dataRef).GetValue();}
				catch
				{
					try {return (T)dataRef.objectValue;}
					catch {return default(T);}				
				}
			}

			if (isNone)
				return default(T);

			if (overrideBB != null){
				dataRef = overrideBB.GetData(dataName.Substring( dataName.LastIndexOf("/") + 1), typeof(T));
				if (dataRef) return (T)dataRef.objectValue;
			}

			if (bb != null){
				dataRef = bb.GetData(dataName, typeof(T));
				if (dataRef) return (T)dataRef.objectValue;
			}
			return default(T);
		}


		///Write the specified object to the blackboard
		protected void Write<T>(T o){

			if (dataRef != null){
				try {((VariableData<T>)dataRef).SetValue(o);}
				catch{dataRef.objectValue = o;}
				return;
			}

			if (isNone)
				return;

			if (overrideBB != null){
				dataRef = overrideBB.SetDataValue(dataName.Substring( dataName.LastIndexOf("/") + 1) , o);
				return;
			}

			if (bb != null){
				dataRef = bb.SetDataValue(dataName, o);
				return;
			}

			Debug.LogError("BBVariable has neither linked VariableData, nor blackboard");
		}
	}


	///The actual type to derive from to create almost any custom variable.
	[Serializable]
	abstract public class BBVariable<T> : BBVariable{
		
		[SerializeField]
		protected T _value;
		public T value{
			get {return useBlackboard? Read<T>() : _value;}
			set {if (useBlackboard) Write<T>(value); else _value = value;}
		}
		public override object objectValue{
			get {return value;}
			set {this.value = (T)value;}
		}
		public override Type varType{
			get {return typeof(T);}
		}
	}

	///Derive this for List<T> types. Not mandatory, but helpful
	[Serializable]
	abstract public class BBListVariable<T> : BBVariable<List<T>>{
		public override string ToString(){
			return useBlackboard? base.ToString() : string.Format("<b>List</b>({0})", value != null? value.Count.ToString() : "0");
		}
	}


	[Serializable]
	public class BBBool : BBVariable<bool>{}
	[Serializable]
	public class BBFloat : BBVariable<float>{}
	[Serializable]
	public class BBInt : BBVariable<int>{}
	[Serializable]
	public class BBVector : BBVariable<Vector3>{}
	[Serializable]
	public class BBVector2 : BBVariable<Vector2>{}
	[Serializable]
	public class BBColor : BBVariable<Color>{}
	[Serializable]
	public class BBAnimationCurve : BBVariable<AnimationCurve>{}
	[Serializable]
	public class BBQuaternion : BBVariable<Quaternion>{}
	[Serializable]
	public class BBString : BBVariable<string>{
		public override bool isNull{ get {return string.IsNullOrEmpty(value); }}
		public override string ToString(){
			return useBlackboard? base.ToString() : string.Format("\"{0}\"", value);
		}
	}

	////UNITY OBJECTS
	[Serializable]
	public class BBGameObject : BBVariable<GameObject>{}
	[Serializable]
	public class BBTransform : BBVariable<Transform>{}
	[Serializable]
	public class BBRigidbody : BBVariable<Rigidbody>{}
	[Serializable]
	public class BBCollider : BBVariable<Collider>{}
	[Serializable]
	public class BBTexture2D : BBVariable<Texture2D>{}
	[Serializable]
	public class BBAudioClip : BBVariable<AudioClip>{}
	[Serializable]
	public class BBAnimationClip : BBVariable<AnimationClip>{}
	[Serializable]
	public class BBMaterial : BBVariable<Material>{}
	[Serializable]
	public class BBSprite : BBVariable<Sprite>{}
	/////

	////SPECIFIC LISTS
	[Serializable]
	public class BBGameObjectList : BBListVariable<GameObject>{}
	[Serializable]
	public class BBComponentList : BBListVariable<Component>{}
	[Serializable]
	public class BBUnityObjectList : BBListVariable<UnityEngine.Object>{}
	[Serializable]
	public class BBStringList : BBListVariable<string>{}
	[Serializable]
	public class BBVectorList : BBListVariable<Vector3>{}
	[Serializable]
	public class BBFloatList : BBListVariable<float>{}
	////

	//Kept for backwards compatibility
	[Serializable]
	public class BBComponent : BBVariable<Component>, IMultiCastable{
		[SerializeField]
		private string _typeName = typeof(Component).AssemblyQualifiedName;
		public override Type varType{
			get {return type;}
		}
		public Type baseType{
			get {return typeof(Component);}
		}
		public Type type{
			get {return Type.GetType(_typeName);}
			set {_typeName = value.AssemblyQualifiedName;}
		}
	}
	///

	///Can be set to any derived type
	[Serializable]
	public class BBObject : BBVariable<UnityEngine.Object>, IMultiCastable{
		[SerializeField]
		private string _typeName = typeof(UnityEngine.Object).AssemblyQualifiedName;
		public override Type varType{
			get {return type;}
		}
		public Type baseType{
			get {return typeof(UnityEngine.Object);}
		}
		public Type type{
			get {return Type.GetType(_typeName);}
			set {_typeName = value.AssemblyQualifiedName;}
		}
	}

	///Can be set to any Enum type
	[Serializable]
	public class BBEnum : BBVariable, IMultiCastable{
		
		[SerializeField]
		private string _value = string.Empty;
		[SerializeField]
		private string _typeName = typeof(Enum).AssemblyQualifiedName;
		public Enum value{
			get
			{
				if (useBlackboard) return Read<Enum>();
				try {return (Enum)Enum.Parse(type, _value);}
				catch{return null;}
			}
			set {if (useBlackboard) Write<Enum>(value); else _value = Enum.GetName(type, value) ;}
		}
		public override object objectValue{
			get {return value;}
			set {this.value = (Enum)value;}
		}
		public override Type varType{
			get {return type;}
		}
		public Type baseType{
			get {return typeof(Enum);}
		}
		public Type type{
			get {return Type.GetType(_typeName);}
			set
			{
				if (_typeName != value.AssemblyQualifiedName){
					_typeName = value.AssemblyQualifiedName;
					if (value != (typeof(Enum))) //dont if ts the base Enum type
						_value = Enum.GetNames(value)[0];
				}
			}
		}
		public override string ToString(){
			return useBlackboard? base.ToString() : string.Format("\'{0}.{1}\'", type.Name, value);
		}
	}

	///Use to show a selection of any type of variable
	[Serializable]
	public class BBVar : BBVariable, IMultiCastable{
		public BBVar(){ blackboardOnly = true; }
		[SerializeField]
		private string _typeName = typeof(object).AssemblyQualifiedName;
		public override Type varType{
			get {return type;}
		}
		public override object objectValue{
			get {return value;}
			set {this.value = value;}
		}
		public Type baseType{
			get {return typeof(object);}
		}
		public Type type{
			get {return Type.GetType(_typeName);}
			set {_typeName = value.AssemblyQualifiedName;}
		}
		public object value{
			get {return Read<object>();}
			set {Write<object>(value);}
		}
	}
}
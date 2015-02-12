using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	///Variables are stored in Blackboards. Custom variables should best derive VariableData<T>
	abstract public class VariableData : MonoBehaviour{

		[SerializeField]
		private string dataName;
		public event System.Action<string, object> onValueChanged;

		///The name of the variable
		new public string name{
			get {return dataName;}
			set	{dataName = value;}
		}

		///The path to the property this data is binded to. Null if none
		abstract public string propertyPath{get;set;}
		///The System.Object value of the contained variable
		abstract public object objectValue{get;set;}
		///The Type this Variable holds
		abstract public System.Type varType{get;set;}
		///Returns whether or not the variable is property binded
		abstract public bool isBinded{get;}
		///Used when saving to get the object information
		abstract public object GetSerialized();
		///Used when loading to set the object information
		abstract public void SetSerialized(object obj);
		///Called when the variable is created
		abstract public void OnCreate();
		///Used to bind variable to a property
		abstract protected void BindProperty(PropertyInfo prop);
		///Used to un-bind variable from a property
		abstract protected void UnBindProperty();
		///Called from Blackboard in Awake to Initialize the binding on specified game object
		abstract public void InitBinding(GameObject go);

		///Should be called when a variable's value changed
		protected void OnValueChanged(object newValue){
			if (onValueChanged != null)
				onValueChanged(dataName, newValue);
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		abstract public void ShowDataGUI(GUILayoutOption[] layoutOptions);
		#endif
	}

	///Derive this to create custom variable data
	abstract public class VariableData<T> : VariableData{
		
		public T value;
		[SerializeField]
		private string _propertyPath;
		private System.Func<T> getter;
		private System.Action<T> setter;

		sealed public override string propertyPath{
			get {return _propertyPath;}
			set {_propertyPath = value;}
		}

		sealed public override bool isBinded{
			get {return !string.IsNullOrEmpty(_propertyPath);}
		}

		sealed public override object objectValue{
			get {return GetValue();}
			set {SetValue((T)value);}
		}

		public override System.Type varType{
			get {return typeof(T);}
			set { /*some variables can change derived type of T*/ }
		}

		virtual public T GetValue(){
			if (getter != null)
				return getter();
			return value;
		}
		
		virtual public void SetValue(T o){
			if (!object.Equals(GetValue(), o)){
				if (setter != null) setter(o);
				else this.value = o;
				OnValueChanged(o);
			}
		}
		
		
		public override object GetSerialized(){	return GetValue(); }
		public override void SetSerialized(object o){ SetValue((T)o); }

		sealed protected override void BindProperty(PropertyInfo prop){
			_propertyPath = string.Format("{0}.{1}", prop.DeclaringType.Name, prop.Name);
		}

		sealed protected override void UnBindProperty(){
			_propertyPath = null;
		}

		sealed public override void InitBinding(GameObject go){
			if (isBinded && Application.isPlaying){
				var arr = _propertyPath.Split('.');
				var comp = go.GetComponent( arr[0] );
				if (comp != null){
					var type = comp.GetType();
					var prop = type.NCGetProperty(arr[1]);
					if (prop.CanRead)
						getter = prop.GetGetMethod().NCCreateDelegate<System.Func<T>>(comp);
					if (prop.CanWrite)
						setter = prop.GetSetMethod().NCCreateDelegate<System.Action<T>>(comp);
				}
			}
		}

		public override void OnCreate(){}

		//////////////////////////
		///////EDITOR/////////////
		//////////////////////////
		#if UNITY_EDITOR

		public override void ShowDataGUI(GUILayoutOption[] layoutOptions){

			if (isBinded){
				var arr = _propertyPath.Split('.');
				GUI.color = new Color(0.8f,0.8f,1);
				GUILayout.Label(string.Format("-> {0}.{1}", arr[0], arr[1]), layoutOptions);
				GUI.color = Color.white;
			} else {
				//GUI.backgroundColor = NCPrefs.GetTypeColor(varType);
				objectValue = EditorField(objectValue, varType, layoutOptions);
				GUI.backgroundColor = Color.white;
			}

			OnVariableGUI();

			//'B' to bind data to property
			if (GUILayout.Button("B", GUILayout.Width(8), GUILayout.Height(16))){
				System.Action<System.Reflection.PropertyInfo> Selected = delegate(System.Reflection.PropertyInfo prop){
					BindProperty(prop);
				};

				var menu = new UnityEditor.GenericMenu();
				foreach (Component comp in gameObject.GetComponents(typeof(Component))){
					if (!comp || comp.hideFlags == HideFlags.HideInInspector)
						continue;
					menu = EditorUtils.GetPropertySelectionMenu(comp.GetType(), new List<System.Type>{varType}, Selected, false, false, menu, "Bind Property");
				}

				menu.AddSeparator("/");
				if (isBinded){
					menu.AddItem(new GUIContent("UnBind Property"), false, delegate{UnBindProperty();});
				} else {
					menu.AddDisabledItem(new GUIContent("UnBind Property"));
				}
				
				menu.ShowAsContext();
				Event.current.Use();
			}
		}

		virtual public void OnVariableGUI(){

		}

		object EditorField(object o, System.Type t, GUILayoutOption[] layoutOptions){

			var actualType = o != null? o.GetType() : t;
			if (actualType.IsAbstract){
				GUILayout.Label( string.Format("({0})", EditorUtils.TypeName(actualType)), layoutOptions );
				return o;
			}

			if (t == typeof(bool))
				return UnityEditor.EditorGUILayout.Toggle((bool)o, layoutOptions);
			if (t == typeof(Color))
				return UnityEditor.EditorGUILayout.ColorField((Color)o, layoutOptions);
			if (t == typeof(AnimationCurve))
				return UnityEditor.EditorGUILayout.CurveField((AnimationCurve)o, layoutOptions);
			if (t.IsSubclassOf(typeof(System.Enum) ))
				return UnityEditor.EditorGUILayout.EnumPopup((System.Enum)o, layoutOptions);
			if (t == typeof(float)){
				GUI.backgroundColor = NCPrefs.GetTypeColor(t);
				return UnityEditor.EditorGUILayout.FloatField((float)o, layoutOptions);
			}
			if (t == typeof(int)){
				GUI.backgroundColor = NCPrefs.GetTypeColor(t);
				return UnityEditor.EditorGUILayout.IntField((int)o, layoutOptions);
			}
			if (t == typeof(string)){
				GUI.backgroundColor = NCPrefs.GetTypeColor(t);
				return UnityEditor.EditorGUILayout.TextField((string)o, layoutOptions);
			}
			if (t == typeof(Vector2))
				return UnityEditor.EditorGUILayout.Vector2Field("",(Vector2)o, new GUILayoutOption[]{GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true), GUILayout.MaxHeight(18)});
			if (t == typeof(Vector3))
				return UnityEditor.EditorGUILayout.Vector3Field("",(Vector3)o, new GUILayoutOption[]{GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true), GUILayout.MaxHeight(18)});
			if (typeof(Object).IsAssignableFrom(t))
				return UnityEditor.EditorGUILayout.ObjectField((Object)o, t, true, layoutOptions);
			if (t == typeof(Quaternion)){
				var quat = (Quaternion)o;
				var vec4 = new Vector4(quat.x, quat.y, quat.z, quat.w);
				vec4 = UnityEditor.EditorGUILayout.Vector4Field("", vec4, layoutOptions);
				return new Quaternion(vec4.x, vec4.y, vec4.z, vec4.w);
			}

			if ( GUILayout.Button( string.Format("({0}) {1}", EditorUtils.TypeName(actualType), (objectValue is IList)? ((IList)objectValue).Count.ToString() : "" ), layoutOptions))
				NodeCanvasEditor.PopObjectEditor.Show(objectValue, actualType);

			return o;
		}

		#endif
	}
}
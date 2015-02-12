using UnityEngine;
using System.Collections.Generic;
using NodeCanvas.Variables;
using System.Reflection;

namespace NodeCanvas{

	[AddComponentMenu("NodeCanvas/Blackboard Property Binder")]
	public class BlackboardPropertyBinder : MonoBehaviour {

		[System.Serializable]
		public class Binder{
			
			public enum BindingType{
				SetProperty,
				GetProperty
			}

			public BindingType bindingType = BindingType.SetProperty;

			public string variableName;
			public string componentName;
			public string propertyName;

			private Component component;
			private System.Func<object, object> getter;
			private System.Action<object, object> setter;

			private VariableData data;

			[SerializeField] [HideInInspector]
			private string _typeName = typeof(object).AssemblyQualifiedName;

			public System.Type type{
				get {return System.Type.GetType(_typeName);}
				set {_typeName = value.AssemblyQualifiedName;}
			}

			public Binder(VariableData data){
				this.variableName = data.name;
				this.type = data.varType;
				this.data = data;
			}

			public void Init(Blackboard bb, GameObject go){

				component = go.GetComponent(componentName);
				if (component == null){
					Debug.LogWarning(string.Format("<b>Property Binder:</b> GameObject doesn't have '{0}' component type", componentName), go);
					return;
				}

				data = bb.GetData(variableName, type);
				if (data == null){
					Debug.LogWarning(string.Format("<b>Property Binder:</b> Blackboard doesn't have variable with name '{0}' and type '{1}'", variableName, type.Name), bb);
					return;
				}

				if (bindingType == BindingType.SetProperty){
					var setterMethod = component.GetType().NCGetMethod("set_" + propertyName);
					if (setterMethod == null){
						Debug.LogWarning(string.Format("<b>Property Binder:</b> Component '{0}' doesn't have '{1}' setter property", componentName, propertyName), go);
						return;
					}
					setter = NCReflection.BuildDelegate<System.Action<object, object>>(setterMethod);
					data.onValueChanged += OnValueChanged;
					OnValueChanged(variableName, data.objectValue);
				}
				else
				if (bindingType == BindingType.GetProperty){
					var getterMethod = component.GetType().NCGetMethod("get_" + propertyName);
					if (getterMethod == null){
						Debug.LogWarning(string.Format("<b>Property Binder:</b> Component '{0}' doesn't have '{1}' getter property", componentName, propertyName), go);
						return;
					}
					getter = NCReflection.BuildDelegate<System.Func<object, object>>(getterMethod);
				}

				Debug.Log(string.Format("Binded blackboard variable '{0}' with '{1}.{2}' property", variableName, componentName, propertyName), go );
			}

			void OnValueChanged(string name, object value){
				setter(component, value);
			}

			object lastValue;
			object currentValue;
			public void Update(){

				if (bindingType != BindingType.GetProperty)
					return;

				currentValue = getter(component);
				if (lastValue != currentValue){
					data.objectValue = currentValue;
					lastValue = currentValue;
				}
			}
		}

		public Blackboard blackboard;
		new public GameObject gameObject;
		public List<Binder> binders = new List<Binder>();

		private bool binded;

		void Reset(){
			blackboard = GetComponent<Blackboard>();
			gameObject = this.transform.gameObject;
		}

		void OnEnable(){
			
			if (binded)
				return;

			binded = true;

			if (!blackboard)
				blackboard = GetComponent<Blackboard>();
			
			if (!gameObject)
				gameObject = this.transform.gameObject;
			
			if (!blackboard || !gameObject)
				return;

			foreach (Binder binder in binders)
				binder.Init(blackboard, gameObject);
		}

		void LateUpdate(){
			foreach (Binder binder in binders)
				binder.Update();
		}
	}
}
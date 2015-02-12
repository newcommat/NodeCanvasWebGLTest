using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("✫ Script Control")]
	[Description("Calls a function that has signature of 'public Status NAME()' or 'public Status NAME(object)'. Return Status.Success, Failure or Running within that function")]
	public class ImplementedAction : ActionTask {

		public BBVariableSet paramValue1 = new BBVariableSet();

		[SerializeField]
		private string scriptName = typeof(Component).AssemblyQualifiedName;
		[SerializeField]
		private string methodName;
		private Status actionStatus = Status.Resting;

		#if UNITY_IPHONE
		private MethodInfo method;
		#else
		private System.Func<object, object, object> method;
		#endif

		public override System.Type agentType{
			get {return System.Type.GetType(scriptName);}
		}

		protected override string info{
			get {return string.Format("[ {0}.{1}{2} ]", agentInfo, methodName, paramValue1.selectedType != null? string.Format("({0})", paramValue1.ToString()) : "" ); }
		}

		protected override string OnInit(){

			var paramTypes = new List<System.Type>();
			if (paramValue1.selectedType != null){
				paramTypes.Add(paramValue1.selectedType);
			}

			var methodInfo = agent.GetType().NCGetMethod(methodName, paramTypes.ToArray());
			if (methodInfo == null)
				return "Method not found";

			#if UNITY_IPHONE
			method = methodInfo;
			#else
			method = NCReflection.BuildDelegate<System.Func<object, object, object>>(methodInfo);
			#endif

			return null;
		}

		protected override void OnExecute(){ Forward(); }
		protected override void OnUpdate(){	Forward(); }

		void Forward(){

			#if UNITY_IPHONE
			var args = new List<object>();
			if (paramValue1.selectedType != null)
				args.Add(paramValue1.objectValue);
			actionStatus = (Status)method.Invoke(agent, args.ToArray());
			#else
			actionStatus = (Status)method(agent, paramValue1.objectValue);
			#endif

			if (actionStatus == Status.Success){
				EndAction(true);
				return;
			}

			if (actionStatus == Status.Failure){
				EndAction(false);
				return;
			}
		}

		protected override void OnStop(){
			actionStatus = Status.Resting;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		[SerializeField]
		private List<string> paramNames = new List<string>{"Param1"};

		/////UPDATING
		protected override void OnEditorValidate(){
			if (agentType == null)
				scriptName = EditorUtils.GetType(scriptName, typeof(Component)).AssemblyQualifiedName;
		}
		///////	
		
		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && agent == null && GUILayout.Button("Alter Type")){
				System.Action<System.Type> TypeSelected = delegate(System.Type t){
					var newName = t.AssemblyQualifiedName;
					if (scriptName != newName){
						scriptName = newName;
						methodName = null;
					}
				};

				EditorUtils.ShowConfiguredTypeSelectionMenu(typeof(Component), TypeSelected);
			}

			if (!Application.isPlaying && GUILayout.Button("Select Action Method")){

				System.Action<MethodInfo> MethodSelected = delegate(MethodInfo method){
					scriptName = method.DeclaringType.AssemblyQualifiedName;
					methodName = method.Name;
					var parameters = method.GetParameters();
					paramNames = parameters.Select(p => EditorUtils.SplitCamelCase(p.Name) ).ToList();
					paramValue1.selectedType = parameters.Length == 1? parameters[0].ParameterType : null;
				};

				if (agent != null){
					EditorUtils.ShowGameObjectMethodSelectionMenu(agent.gameObject, new List<System.Type>{typeof(Status)}, paramValue1.availableTypes, MethodSelected, 1, false);
				} else {
					var menu = EditorUtils.GetMethodSelectionMenu(agentType, new List<System.Type>{typeof(Status)}, null, MethodSelected, 0, false);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}

			if (!string.IsNullOrEmpty(methodName)){
				UnityEditor.EditorGUILayout.LabelField("Selected Action Method:", methodName);
				if (paramValue1.selectedType != null)
					EditorUtils.BBVariableField(paramNames[0], paramValue1.selectedBBVariable);
			}
		}
		
		#endif
	}
}
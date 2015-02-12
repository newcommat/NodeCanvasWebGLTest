using UnityEngine;
using System;
using System.Reflection;

namespace NodeCanvas.Conditions{

	[Category("✫ Script Control")]
	[Description("Will subscribe to an event of EventHandler type or custom handler with zero parameters and return type of void")]
	[AgentType(typeof(Transform))]
	public class CheckCSharpEvent : ConditionTask {

		[SerializeField]
		private string scriptName = typeof(Component).AssemblyQualifiedName;
		[SerializeField]
		private string eventName;

		public override Type agentType{
			get {return Type.GetType(scriptName);}
		}
		
		protected override string info{
			get {return string.Format("'{0}' Raised", eventName);}
		}

		protected override string OnInit(){

			var eventInfo = agent.GetType().NCGetEvent(eventName);
			if (eventInfo == null)
				return "Event was not found";
				
			MethodInfo m;
			Delegate pointer;
			if (eventInfo.EventHandlerType == typeof(EventHandler)){
				m = this.GetType().NCGetMethod("DefaultRaised");
                pointer = m.NCCreateDelegate<Action<object, EventArgs>>(this);
			} else {
				m = this.GetType().NCGetMethod("Raised");
                pointer = m.NCCreateDelegate(eventInfo.EventHandlerType, this);
			}
			eventInfo.AddEventHandler( agent, pointer );
			return null;
		}

		public void DefaultRaised(object sender, EventArgs e){
			Raised();
		}

		public void Raised(){
			YieldReturn(true);
		}

		protected override bool OnCheck(){
			return false;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnTaskInspectorGUI(){

			if (!Application.isPlaying && agent == null && GUILayout.Button("Change Type")){
				EditorUtils.ShowConfiguredTypeSelectionMenu(typeof(Component), (t)=> { scriptName = t.AssemblyQualifiedName; });
			}


			if (!Application.isPlaying && GUILayout.Button("Select Event")){
				Action<EventInfo> Selected = delegate(EventInfo e){
					scriptName = e.DeclaringType.AssemblyQualifiedName;
					eventName = e.Name;
				};

				if (agent != null){
					EditorUtils.ShowGameObjectEventSelectionMenu(agent.gameObject, Selected);
				} else {
					var menu = EditorUtils.GetEventSelectionMenu(agentType, Selected);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}


			if (!string.IsNullOrEmpty(eventName)){
				GUILayout.BeginVertical("box");
				UnityEditor.EditorGUILayout.LabelField("Selected Type", agentType.Name);
				UnityEditor.EditorGUILayout.LabelField("Selected Event", eventName);
				GUILayout.EndVertical();
			}
		}
		
		#endif
	}
}
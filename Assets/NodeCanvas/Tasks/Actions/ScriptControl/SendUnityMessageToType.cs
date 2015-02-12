using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("✫ Script Control")]
	[Description("Send a Unity message to all game objects with at least one of the specified type components\nNotice: This is slow")]
	public class SendUnityMessageToType : ActionTask {

		public BBString message;
		[SerializeField]
		private NCTypeInfo type;

		protected override string info{
			get {return string.Format("Message {0} to all {1}", message, type.Get() != null? type.Get().Name : "NULL");}
		}

		protected override void OnExecute(){
			
			Component[] objects = FindObjectsOfType(type.Get()) as Component[];
			if (objects.Length == 0){
				EndAction(false);
				return;
			}

			foreach (var o in objects)
				o.gameObject.SendMessage(message.value, SendMessageOptions.DontRequireReceiver);
			
			EndAction();
		}


		/////
		/////
		/////
		#if UNITY_EDITOR

		protected override void OnTaskInspectorGUI(){
			
			DrawDefaultInspector();
			if (GUILayout.Button("Select Type")){
				System.Action<System.Type> TypeSelected = delegate(System.Type t){
					type = new NCTypeInfo(t);
				};

				EditorUtils.ShowConfiguredTypeSelectionMenu(typeof(Component), TypeSelected);
			}			
		}

		#endif
	}
}
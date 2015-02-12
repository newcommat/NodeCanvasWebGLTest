using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class GameObjectData : VariableData<GameObject>{

		public override object GetSerialized(){

			var go = GetValue();
			if (go == null)
				return null;

			string path = "/" + go.name;
			while (go.transform.parent != null){
				go = go.transform.parent.gameObject;
				path = "/" + go.name + path;
			}
			
			return path;
		}

		public override void SetSerialized(object obj){
			var go = GameObject.Find((string)obj);
			SetValue( go );
			if (go == null && !string.IsNullOrEmpty((string)obj))
				Debug.LogWarning("GameObjectData Failed to load. GameObject is not in scene. Path '" + (obj as string) + "'");
		}
	}
}
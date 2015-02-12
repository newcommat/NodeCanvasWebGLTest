using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class GameObjectListData : VariableData<List<GameObject>>{

		public override void OnCreate(){
			value = new List<GameObject>();
		}

		public override object GetSerialized(){

			var goPaths = new List<string>();
			foreach (GameObject go in GetValue()){

				var obj = go;
				if (obj == null){
					goPaths.Add(null);
					continue;
				}

				string path= "/" + obj.name;

				while (obj.transform.parent != null){
					obj = obj.transform.parent.gameObject;
					path = "/" + obj.name + path;
				}
				
				goPaths.Add(path);
			}

			return goPaths;
		}

		public override void SetSerialized(object obj){
			var goPaths = new List<string>(obj as List<string>);
			var newList = new List<GameObject>();
			foreach (string goPath in goPaths){
				var go = GameObject.Find(goPath);
				newList.Add(go);
				if (go == null && !string.IsNullOrEmpty(goPath))
					Debug.LogWarning("GameObjectListData Failed to load a GameObject in the list. GameObject was not found in scene. Path '" + goPath + "'");
			}
			SetValue(newList);
		}
	}
}
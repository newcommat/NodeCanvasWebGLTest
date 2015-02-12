
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;

namespace NodeCanvasEditor{

	///A generic popup editor for all types
	public class PopObjectEditor : EditorWindow{

		private object targetObject;
		private System.Type targetType;
		private Vector2 scrollPos;

		void OnEnable(){
			title = "NC Object Editor";
			GUI.skin.label.richText = true;
			//EditorApplication.playmodeStateChanged += Close;
		}

		void OnGUI(){

			if (EditorApplication.isCompiling || targetType == null){
				Close();
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(string.Format("<size=14><b>{0}</b></size>", NodeCanvas.EditorUtils.TypeName(targetType) ) );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			NodeCanvas.EditorUtils.GenericField(NodeCanvas.EditorUtils.TypeName(targetType), targetObject, targetType);
			GUILayout.EndScrollView();
			Repaint();
		}

		public static void Show(object o, System.Type t){

			var window = ScriptableObject.CreateInstance(typeof(PopObjectEditor)) as PopObjectEditor;
			window.targetObject = o;
			window.targetType = t;
			window.ShowUtility();
		}
	}
}

#endif
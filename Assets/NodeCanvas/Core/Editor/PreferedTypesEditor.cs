using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NodeCanvas;

namespace NodeCanvasEditor{

	public class PreferedTypesEditor : EditorWindow {

		private List<System.Type> typeList;
		private Vector2 scrollPos;

		void OnEnable(){
			title = "Prefered Types";
			typeList = NCPrefs.GetPreferedTypesList(typeof(object));
		}

		void OnGUI(){
			
			EditorGUILayout.HelpBox("Here you can specify frequently used types for your game for easier access wherever you need to select a type\nFor example when setting the type of an Object variable as well as when setting the agent type in any 'Script Control' Task", MessageType.Info);

			if (GUILayout.Button("Add New Type")){
				GenericMenu.MenuFunction2 Selected = delegate(object t){
					if (!typeList.Contains( (System.Type)t)){
						typeList.Add( (System.Type)t);
						Save();
					} else {
						ShowNotification(new GUIContent("Type already in list") );
					}
				};	

				var menu = new UnityEditor.GenericMenu();
				foreach(System.Type t in EditorUtils.GetAssemblyTypes(typeof(object))){
					var friendlyName = (string.IsNullOrEmpty(t.Namespace)? "No Namespace/" : t.Namespace.Replace(".", "/") + "/") + EditorUtils.TypeName(t);
					var category = "Classes/";
					if (t.IsInterface) category = "Interfaces/";
					if (t.IsEnum) category = "Enumerations/";
					menu.AddItem(new GUIContent( category + friendlyName), false, Selected, t);
				}
				menu.ShowAsContext();
				Event.current.Use();
			}

			if (GUILayout.Button("RESET DEFAULTS")){
				NCPrefs.ResetTypeConfiguration();
				typeList = NCPrefs.GetPreferedTypesList(typeof(object));
				Save();
			}

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			EditorUtils.ReorderableList(typeList, delegate(int i){
				GUILayout.BeginHorizontal("box");
				EditorGUILayout.LabelField(typeList[i].Name, typeList[i].Namespace);
				if (GUILayout.Button("X", GUILayout.Width(18))){
					typeList.RemoveAt(i);
					Save();
				}
				GUILayout.EndHorizontal();
			});

			GUILayout.EndScrollView();

			Repaint();
		}

		void Save(){
			NCPrefs.SetPreferedTypesList(typeList);
			ShowNotification(new GUIContent("Configuration Saved!"));
		}


		[MenuItem("Window/NodeCanvas/Set Prefered Types")]
		public static void ShowWindow(){
			var window = GetWindow<PreferedTypesEditor>();
			window.Show();
		}
	}
}
﻿using UnityEditor;
using UnityEngine;
using NodeCanvas;

namespace NodeCanvasEditor{

	public class ExternalInspector : EditorWindow {

		private object currentSelection;
		private Vector2 scrollPos;

		void OnEnable(){
	        title = "NC Inspector";
	        Graph.useExternalInspector = true;
		}

		void OnDestroy(){
			Graph.useExternalInspector = false;
		}

		void Update(){
			if (currentSelection != Graph.currentSelection)
				Repaint();
		}

		void OnGUI(){

			if (GraphEditor.currentGraph == null){
				GUILayout.Label("No current NodeCanvas Graph open");				
				return;
			}
				
			if (EditorApplication.isCompiling){
				ShowNotification(new GUIContent("Compiling Please Wait..."));
				return;			
			}

			currentSelection = Graph.currentSelection;

			if (currentSelection == null){
				GUILayout.Label("No Node Selected in Canvas");
				return;
			}

			scrollPos = GUILayout.BeginScrollView(scrollPos);

			if (typeof(Node).IsAssignableFrom(currentSelection.GetType()) ){
				var node = currentSelection as Node;
				Title(node.name );
				node.ShowNodeInspectorGUI();
			}
			
			if (typeof(Connection).IsAssignableFrom(currentSelection.GetType() )){
				Title("Connection");
				(currentSelection as Connection).ShowConnectionInspectorGUI();
			}

			GUILayout.EndScrollView();
		}

		void Title(string text){

			GUILayout.Space(5);
			GUILayout.BeginHorizontal("box", GUILayout.Height(28));
			GUILayout.FlexibleSpace();
			GUILayout.Label("<b><size=16>" + text + "</size></b>");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			//EditorUtils.BoldSeparator();
		}

	    [MenuItem("Window/NodeCanvas/External Inspector")]
	    public static void OpenWindow() {

	        var window = GetWindow(typeof(ExternalInspector)) as ExternalInspector;
	        window.Show();
	    }
	}
}
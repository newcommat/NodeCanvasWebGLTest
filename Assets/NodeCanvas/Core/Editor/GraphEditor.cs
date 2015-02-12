using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using NodeCanvas;

namespace NodeCanvasEditor{

	public class GraphEditor : EditorWindow{

		//the current graph loaded for editing. Can be a nested graph of the root graph
		public static Graph currentGraph;

		//the root graph that was first opened in the editor
		private Graph _rootGraph;
		private int rootGraphID;

		//the GrapOwner if any, that was used to open the editor and from which to read the rootGraph
		private GraphOwner _targetOwner;
		private int targetOwnerID;

		private Rect totalCanvas  = new Rect();
		private Vector2 scrollPos = Vector2.zero;
		private float topMargin   = 20;
		private GUISkin guiSkin;
		private int repaintCounter;
		private Vector2 selectionStartPos;
		private bool isMultiSelecting;

		private Graph rootGraph{
			get
			{
				if (_rootGraph == null)
					_rootGraph = EditorUtility.InstanceIDToObject(rootGraphID) as Graph;
				return _rootGraph;
			}
			set
			{
				_rootGraph = value;
				rootGraphID = value != null? value.GetInstanceID() : 0;
			}
		}

		private GraphOwner targetOwner{
			get
			{
				if (_targetOwner == null)
					_targetOwner = EditorUtility.InstanceIDToObject(targetOwnerID) as GraphOwner;
				return _targetOwner;
			}
			set
			{
				_targetOwner = value;
				targetOwnerID = value != null? value.GetInstanceID() : 0;
			}
		}

		private Rect viewCanvas{
			get { return new Rect(5, topMargin, position.width - 10, position.height - topMargin - 5);	}
		}

		private Rect canvasLimits{
			get
			{
				float minX = 0;
				float minY = 0;
				float maxX = 0;
				float maxY = 0;
				
				for (int i = 0; i < currentGraph.allNodes.Count; i++){
					var node = currentGraph.allNodes[i];
					minX = Mathf.Min(minX, node.nodeRect.x-20);
					minY = Mathf.Min(minY, node.nodeRect.y-20);
					maxX = Mathf.Max(maxX, node.nodeRect.xMax+20);
					maxY = Mathf.Max(maxY, node.nodeRect.yMax+20);
				}

				return new Rect(minX, minY, maxX, maxY);				
			}
		}

		void OnEnable(){
			title = "NodeCanvas";
			guiSkin = EditorGUIUtility.isProSkin? (GUISkin)Resources.Load("NodeCanvasSkin") : (GUISkin)Resources.Load("NodeCanvasSkinLight");
			wantsMouseMove = true;
			Repaint();
		}

		void OnInspectorUpdate(){
			Repaint();
		}

		void OnGUI(){

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;

			if (EditorApplication.isCompiling){
				ShowNotification(new GUIContent("Compiling Please Wait..."));
				return;			
			}

			var e = Event.current;
			GUI.skin = guiSkin;

			//get the graph from the GraphOwner if one is set
			if (targetOwner != null)
				rootGraph = targetOwner.behaviour;

			if (rootGraph == null){
				ShowNotification(new GUIContent("Please select a GameObject with a Graph Owner or a Graph itself"));
				return;
			}

			//set the currently viewing graph by getting the child graph from the root graph recursively
			currentGraph = GetCurrentGraph(rootGraph);

			///UPGRADE NODES PRIOR TO VERSION 1.6
			if (!currentGraph.hasUpgraded_1_6){
				if (EditorUtility.DisplayDialog("Updating Graph", "This graph needs to be updated", "OK")){
					currentGraph.hasUpgraded_1_6 = true;
					var primeID = 1;
					var oldNodes = new List<Node>(currentGraph.allNodes);
					var dupNodes = Node.DuplicateNodes(oldNodes);
					for (int i = 0; i < oldNodes.Count; i++){
						if (currentGraph.primeNode == oldNodes[i])
							primeID = i;
						currentGraph.allNodes.Remove(oldNodes[i]);
						DestroyImmediate(oldNodes[i].gameObject, true);
					}
					
					currentGraph.primeNode = dupNodes[primeID];
				}
			}
			/////


			//disconnect prefab if graph is a prefab instance
			if (e.type == EventType.MouseDown || e.type == EventType.KeyDown){
				if (PrefabUtility.GetPrefabType(currentGraph) == PrefabType.PrefabInstance){
					ShowNotification(new GUIContent("Prefab Disconnected. Apply when done."));
					PrefabUtility.DisconnectPrefabInstance(currentGraph);
				}
			}

			//set repating counter if need be
			if (mouseOverWindow == this && (e.isMouse || e.isKey) )
				repaintCounter += 2;

			//hande undo/redo keyboard commands
			if (e.type == EventType.ValidateCommand && e.commandName == "UndoRedoPerformed"){
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                e.Use();
				return;
			}

			//snap all nodes
			if (e.type == EventType.MouseUp || e.type == EventType.KeyUp)
				SnapNodes();


			//Set canvas limits from the nodes
			totalCanvas.width = canvasLimits.width;
			totalCanvas.height = canvasLimits.height;

			//GUI flavor
			GUI.Box(viewCanvas, string.Format("{0}\n{1}", currentGraph.GetType().Name, "NodeCanvas v1.6.1"), "canvasBG");
			DrawGrid();

			//Begin windows and ScrollView for the nodes
			scrollPos = GUI.BeginScrollView (viewCanvas, scrollPos, totalCanvas);
			BeginWindows();
			currentGraph.ShowNodesGUI(new Rect(scrollPos.x, scrollPos.y, viewCanvas.width, viewCanvas.height));
			EndWindows();
			GUI.EndScrollView();
			//

			//Breadcrumb navigation
			GUILayout.BeginArea(new Rect(20, topMargin + 5, Screen.width, Screen.height));
			ShowBreadCrumbNavigation(rootGraph);
			GUILayout.EndArea();
			//

			//Graph controls (after windows to show on top)
			currentGraph.ShowGraphControls(scrollPos);

			//handle multi selection
			DoMultiSelection();

			//repaint
			if (repaintCounter > 0 || Application.isPlaying){
				repaintCounter = Mathf.Max (repaintCounter -1, 0);
				Repaint();
			}

			//set nodes size to minimum. They rescale to fit automaticaly since they use GUILayout.Window
			if (GUI.changed){
				foreach (Node node in currentGraph.allNodes){
					node.nodeRect.width = Node.minSize.x;
					node.nodeRect.height = Node.minSize.y;
				}
				Repaint();
			}

			//post
			GUI.Box(viewCanvas,"", "canvasBorders");
			GUI.skin = null;
			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;
		}


		//...
		void DoMultiSelection(){
			var e = Event.current;
			if (Graph.allowClick && e.button == 0 && e.type == EventType.MouseDown && !e.alt && !e.shift){
				Graph.multiSelection.Clear();
				selectionStartPos = e.mousePosition;
				isMultiSelecting = true;
			}

			if (isMultiSelecting){
				var rect = GetSelectionRect(e.mousePosition);
				if (rect.width > 5 && rect.height > 5){
					GUI.color = new Color(0.5f,0.5f,1,0.3f);
					GUI.Box(rect, "");
					rect.center += scrollPos;
					foreach (Node node in currentGraph.allNodes){
						if (RectContainsRect(rect, node.nodeRect) && !node.isHidden){
							var highlightRect = node.nodeRect;
							highlightRect.center += new Vector2(5 ,topMargin) - scrollPos;
							GUI.Box(highlightRect, "", "windowHighlight");
						}
					}
				}
			}

			if (isMultiSelecting && e.button == 0 && e.type == EventType.MouseUp){
				var overNodes = new List<Object>();
				var rect = GetSelectionRect(e.mousePosition);
				rect.center += scrollPos;
				foreach (Node node in currentGraph.allNodes){
					if (RectContainsRect(rect, node.nodeRect) && !node.isHidden)
						overNodes.Add(node);
				}
				Graph.multiSelection = overNodes;
				isMultiSelecting = false;
			}
		}

		//rect b marginaly contained inside rect a?
		bool RectContainsRect(Rect a, Rect b){
			return b.xMin <= a.xMax && b.xMax >= a.xMin && b.yMin <= a.yMax && b.yMax >= a.yMin;
		}

		//Draw a simple grid
		void DrawGrid(){

			Handles.color = new Color(0,0,0,0.1f);
			for (int i = 0; i < Screen.width; i++){
				if (i % 60 == 0)
					Handles.DrawLine(new Vector3(i,0,0), new Vector3(i,Screen.height,0));
				if (i % 15 == 0)
					Handles.DrawLine(new Vector3(i,0,0), new Vector3(i,Screen.height,0));
			}
			
			var yOffset = -7 - scrollPos.y;
			for (int i = 0; i < Screen.height + scrollPos.y; i++){
				if (i % 60 == 0)
					Handles.DrawLine(new Vector3(0, i + yOffset, 0), new Vector3(Screen.width, i + yOffset, 0));
				if (i % 15 == 0)
					Handles.DrawLine(new Vector3(0, i + yOffset, 0), new Vector3(Screen.width, i + yOffset, 0));
			}
			Handles.color = Color.white;
		}

		//Recursively get the currenlty showing nested graph starting from the root
		Graph GetCurrentGraph(Graph root){
			if (root.nestedGraphView == null)
				return root;
			return GetCurrentGraph(root.nestedGraphView);
		}

		//This is the hierarchy shown at top left. Recusrsively show the nested path
		void ShowBreadCrumbNavigation(Graph root){

			if (root == null)
				return;

			if (Graph.currentSelection != null && !Graph.useExternalInspector)
				return;

			var agentInfo = root.agent != null? root.agent.gameObject.name : "No Agent";
			var bbInfo = root.blackboard? root.blackboard.name : "No Blackboard";
			var prefabInfo = PrefabUtility.GetPrefabType(root) == PrefabType.Prefab? "<color=#ff4d4d>(PREFAB Asset)</color>" : "";
			prefabInfo = PrefabUtility.GetPrefabType(currentGraph) == PrefabType.PrefabInstance? "<color=#ff4d4d>(PREFAB Instance)</color>" : prefabInfo;

			GUI.color = new Color(1f,1f,1f,0.5f);
			GUILayout.BeginVertical();
			if (root.nestedGraphView == null){

				if (root.agent == null && root.blackboard == null){
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>", root.name, prefabInfo));	
				} else {
					GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>\n<size=10>{2} | {3}</size>", root.name, prefabInfo, agentInfo, bbInfo));
				}

			} else {

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("^ " + root.name, (GUIStyle)"button")){
					root.nestedGraphView = null;
					Event.current.Use();
				}

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				ShowBreadCrumbNavigation(root.nestedGraphView);
			}

			GUILayout.EndVertical();
			GUI.color = Color.white;
		}

		//Snap all nodes (this is not what is done while dragging a node)
		void SnapNodes(){

			if (!NCPrefs.doSnap)
				return;

			foreach(Node node in currentGraph.allNodes){
				var snapedPos = new Vector2(node.nodeRect.xMin, node.nodeRect.yMin);
				snapedPos.y = Mathf.Round(snapedPos.y / 15) * 15;
				snapedPos.x = Mathf.Round(snapedPos.x / 15) * 15;
				node.nodeRect = new Rect(snapedPos.x, snapedPos.y, node.nodeRect.width, node.nodeRect.height);
			}
		}


		//Change viewing graph
		void OnSelectionChange(){
			
			if (Selection.activeGameObject != null){
				var foundOwner = Selection.activeGameObject.GetComponent<GraphOwner>();
				if (!NCPrefs.isLocked && foundOwner != null && foundOwner.behaviour != null){
					var lastEditor = EditorWindow.focusedWindow;
					OpenWindow(foundOwner);
					if (lastEditor) lastEditor.Focus();
				}
			}
		}

		Rect GetSelectionRect(Vector2 endPos){
			var num1 = (selectionStartPos.x < endPos.x)? selectionStartPos.x : endPos.x;
			var num2 = (selectionStartPos.x > endPos.x)? selectionStartPos.x : endPos.x;
			var num3 = (selectionStartPos.y < endPos.y)? selectionStartPos.y : endPos.y;
			var num4 = (selectionStartPos.y > endPos.y)? selectionStartPos.y : endPos.y;
			return new Rect(num1, num3, num2 - num1, num4 - num3);		
		}


	    //Opening the window for a graph owner
	    public static GraphEditor OpenWindow(GraphOwner owner){
	    	var window = OpenWindow(owner.behaviour, owner, owner.blackboard);
	    	window.targetOwner = owner;
	    	return window;
	    }

	    //For opening the window from gui button in the nodegraph's Inspector.
	    public static GraphEditor OpenWindow(Graph newGraph){
	    	return OpenWindow(newGraph, newGraph.agent, newGraph.blackboard);
	    }

	    //...
	    public static GraphEditor OpenWindow(Graph newGraph, Component agent, Blackboard blackboard) {

	        var window = GetWindow(typeof(GraphEditor)) as GraphEditor;
	        newGraph.agent = agent;
	        newGraph.blackboard = blackboard;
	        newGraph.nestedGraphView = null;
	        newGraph.SendTaskOwnerDefaults();
	        newGraph.UpdateNodeIDsInGraph();

	        window.rootGraph = newGraph;
	        window.targetOwner = null;
	        Graph.currentSelection = null;
	        return window;
	    }
	}
}
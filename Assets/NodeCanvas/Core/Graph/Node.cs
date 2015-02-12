#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Variables;

namespace NodeCanvas{

	public enum Status {
		Failure  = 0,
		Success  = 1,
		Running  = 2,
		Resting  = 3,
		Error    = 4,
	}

	///The base class for all nodes that can live in NodeCanvas' graph systems
	abstract public partial class Node : MonoBehaviour{

		[SerializeField]
		private List<Connection> _inConnections = new List<Connection>();
		[SerializeField]
		private List<Connection> _outConnections = new List<Connection>();
		[SerializeField]
		private Graph _graph;
		[SerializeField]
		private string _customName;
		[SerializeField]
		private string _tagName;

		private Status _status = Status.Resting;
		private string _nodeName;

		/////

		private string customName{
			get {return _customName;}
			set {_customName = value;}
		}

		///The title name of the node shown in the window if editor is not in Icon Mode. This is a property so title name may change instance wise
		new virtual public string name{
			get
			{
				if (!string.IsNullOrEmpty(customName))
					return customName;

				if (string.IsNullOrEmpty(_nodeName) ){
					var nameAtt = this.GetType().NCGetAttribute<NameAttribute>(false);
					_nodeName = nameAtt != null? nameAtt.name : GetType().Name;
				}
				return _nodeName;
			}
			set {customName = value;}
		}

		///The node tag. Useful for finding nodes through code
		new public string tag{
			get {return _tagName;}
			set {_tagName = value;}
		}

		///The numer of possible inputs. -1 for infinite
		virtual public int maxInConnections{
			get {return -1;}
		}

		///The numer of possible outputs. -1 for infinite
		virtual public int maxOutConnections{
			get {return -1;}
		}

		///The output connection Type this node has
		virtual public System.Type outConnectionType{
			get {return typeof(Connection);}
		}

		///Can this node be set as prime (Start)?
		virtual public bool allowAsPrime{
			get {return true;}
		}

		///All incomming connections to this node
		public List<Connection> inConnections{
			get {return _inConnections;}
			protected set {_inConnections = value;}
		}

		///All outgoing connections from this node
		public List<Connection> outConnections{
			get {return _outConnections;}
			protected set {_outConnections = value;}
		}

		///The graph this node belongs to
		public Graph graph{
			get {return _graph;}
			private set {_graph = value;}
		}

		///The current status of the node
		public Status status{
			get {return _status;}
			protected set {_status = value;}
		}

		///The node's ID in the graph
		public int ID{get; private set;}

		///The current agent. Taken from the graph this node belongs to
		protected Component graphAgent{
			get {return graph != null? graph.agent : null;}
		}

		///The current blackboard. Taken from the graph this node belongs to
		protected Blackboard graphBlackboard{
			get {return graph != null? graph.blackboard : null;}
		}

		//Used to check recursion
		private bool isChecked{get;set;}

		/////////////////////
		/////////////////////
		/////////////////////

		//protect for future case
		protected void Awake(){
			OnAwake();
		}
		
		virtual protected void OnAwake(){}

		///Create a new Node
		public static Node Create(Graph ownerGraph, System.Type nodeType){
			var newNode = ownerGraph.nodesRoot.gameObject.AddComponent(nodeType) as Node;
			newNode.graph = ownerGraph;
			newNode.UpdateNodeBBFields(ownerGraph.blackboard);
			newNode.OnCreate();
			return newNode;
		}

		///Called when the node is created
		virtual protected void OnCreate(){}

		///Returns if a new connection should be allowed with the source node.
		public bool IsNewConnectionAllowed(Node sourceNode){
			
			if (this == sourceNode){
				Debug.LogWarning("Node can't connect to itself");
				return false;
			}

			if (sourceNode.outConnections.Count >= sourceNode.maxOutConnections && sourceNode.maxOutConnections != -1){
				Debug.LogWarning("Source node can have no more out connections.");
				return false;
			}

			if (this == graph.primeNode && maxInConnections == 1){
				Debug.LogWarning("Target node can have no more connections");
				return false;
			}

			if (maxInConnections <= inConnections.Count && maxInConnections != -1){
				Debug.LogWarning("Target node can have no more connections");
				return false;
			}
/*
			foreach (Connection c in sourceNode.outConnections){
				if (c.targetNode == this){
					Debug.LogWarning("Nodes are already connected");
					return false;
				}
			}
*/
			return true;
		}

		public Status Execute(){
			return Execute(graphAgent, graphBlackboard);
		}

		public Status Execute(Component agent){
			return Execute(agent, graphBlackboard);
		}

		
		///The main execution function of the node. Execute the node for the agent and blackboard provided. Default = graphAgent and graphBlackboard
		public Status Execute(Component agent, Blackboard blackboard){

			if (isChecked)
				return Error("Infinite Loop Detected");

			isChecked = true;
			status = OnExecute(agent, blackboard);
			isChecked = false;

			return status;
		}

		///A little helper function to log errors easier
		protected Status Error(string log){
			Debug.LogError("<b>Graph Error:</b> '" + log + "' On node '" + name + "' ID " + ID + " | On graph '" + graph.name + "'", graph.gameObject);
			return Status.Error;
		}

		///Override this to specify what the node does.
		virtual protected Status OnExecute(Component agent, Blackboard blackboard){
			return OnExecute(agent);
		}

		virtual protected Status OnExecute(Component agent){
			return OnExecute();
		}

		virtual protected Status OnExecute(){
			return status;
		}

		public void ResetNode(){ ResetNode(true);}
		///Recursively reset the node and child nodes if it's not Resting already
		public void ResetNode(bool recursively){

			if (status == Status.Resting || isChecked)
				return;

			OnReset();
			status = Status.Resting;

			isChecked = true;
			for (int i = 0; i < outConnections.Count; i++)
				outConnections[i].ResetConnection(recursively);
			isChecked = false;
		}

		///Called when the node gets reseted. e.g. OnGraphStart, after a tree traversal, when interrupted, OnGraphEnd...
		virtual protected void OnReset(){}

		///Sends an event to the graph
		public void SendEvent(string name){
			graph.SendEvent(name);
		}

		//Nodes can use coroutine as normal through MonoManager.
		new protected Coroutine StartCoroutine(IEnumerator routine){
			return MonoManager.current.StartCoroutine(routine);
		}

		//Set the target blackboard for all BBVariables found on node. Done when creating node, OnValidate as well as when graphBlackboard set to a new value.
		public void UpdateNodeBBFields(Blackboard bb){
			BBVariable.SetBBFields(bb, this);
		}

		//Updates the node ID in it's current graph. This is called in the editor GUI for convenience, as well as whenever a change is made in the node graph and from the node graph.
		public int AssignIDToGraph(int lastID){

			if (isChecked)
				return lastID;
			isChecked = true;

			lastID++;
			ID = lastID;

			for (int i = 0; i < outConnections.Count; i++)
				lastID = outConnections[i].targetNode.AssignIDToGraph(lastID);

			return lastID;
		}

		public void ResetRecursion(){

			if (!isChecked)
				return;
			isChecked = false;
			for (int i = 0; i < outConnections.Count; i++)
				outConnections[i].targetNode.ResetRecursion();
		}


		///Returns all parent nodes in case node can have many parents like in FSM and Dialogue Trees
		public List<Node> GetParentNodes(){
			if (inConnections.Count != 0)
				return inConnections.Select(c => c.sourceNode).ToList();
			return new List<Node>();
		}

		///Get all childs of this node, on the first depth level
		public List<Node> GetChildNodes(){
			if (outConnections.Count != 0)
				return outConnections.Select(c => c.targetNode).ToList();
			return new List<Node>();
		}

		///Called when an input connection is connected
		virtual public void OnParentConnected(int connectionIndex){}

		///Called when an input connection is disconnected but before it actually does
		virtual public void OnParentDisconnected(int connectionIndex){}

		///Called when an output connection is connected
		virtual public void OnChildConnected(int connectionIndex){}

		///Called when an output connection is disconnected but before it actually does
		virtual public void OnChildDisconnected(int connectionIndex){}

		///Called when the parent graph is started (not continued from pause). Use to init values or otherwise.
		virtual public void OnGraphStarted(){}

		///Called when the parent graph is stopped.
		virtual public void OnGraphStoped(){}

		///Called when the parent graph is paused.
		virtual public void OnGraphPaused(){}

		sealed public override string ToString(){
			var assignable = this as ITaskAssignable;
			return string.Format("{0} ({1})", name, assignable != null && assignable.task != null? assignable.task.ToString() : "" );
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		//Class for the nodeports GUI
		class GUIPort{

			public int portIndex;
			public Node parent;
			public Vector2 pos;

			public GUIPort(int index, Node parent, Vector2 pos){
				this.portIndex = index;
				this.parent = parent;
				this.pos = pos;
			}
		}

		[HideInInspector]
		public Rect nodeRect            = new Rect(0,0,minSize.x, minSize.y);
		[SerializeField]
		private string nodeComment      = string.Empty;
		[SerializeField]
		private bool _childrenCollapsed = false;

		private Texture2D _icon;
		private string _nodeDescription;
		private bool nodeIsPressed{get;set;}
		private static GUIPort clickedPort{get;set;}
		private static GUIStyle _centerLabel = null;
		readonly public static Color successColor = new Color(0.4f, 0.7f, 0.2f);
		readonly public static Color failureColor = new Color(1.0f, 0.3f, 0.3f);
		readonly public static Color runningColor = Color.yellow;
		readonly public static Color restingColor = new Color(0.7f, 0.7f, 1f, 0.8f);
		readonly public static Vector2 minSize    = new Vector2(100, 20);

		private static GUIStyle centerLabel{
			get
			{
				if (_centerLabel == null){
					_centerLabel = new GUIStyle("label");
					_centerLabel.alignment = TextAnchor.UpperCenter;
					_centerLabel.richText = true;
				}
				return _centerLabel;
			}
		}

		//The help info of the node
		private string nodeDescription{
			get
			{
				if (string.IsNullOrEmpty(_nodeDescription)){
					var descAtt = this.GetType().NCGetAttribute<DescriptionAttribute>(false);
					_nodeDescription = descAtt != null? descAtt.description : "No Description";
				}
				return _nodeDescription;
			}
		}

		///Editor! Active is relevant to the input connections
		public bool isActive{
			get
			{
				for (int i = 0; i < inConnections.Count; i++)
					if (inConnections[i].isActive)
						return true;
				return inConnections.Count == 0;
			}
		}

		///Editor! Are children collapsed?
		public bool childrenCollapsed{
			get {return _childrenCollapsed;}
			set {_childrenCollapsed = value;}
		}

		///EDITOR! is the node hidden due to parent has children collapsed or is hidden itself?
		public bool isHidden {
			get
			{
				if (graph.autoSort){
					foreach (Node parent in inConnections.Select(c => c.sourceNode)){
						if (parent.ID > this.ID)
							continue;
						if (parent.childrenCollapsed || parent.isHidden)
							return true;
					}
				}
				return false;
			}
		}

		private bool isSelected{
			get {return Graph.currentSelection == this || Graph.multiSelection.Contains(this);}
		}

		//The icon of the node
		private Texture2D icon{
			get
			{
				if (this is ITaskAssignable){
					var assignable = this as ITaskAssignable;
					if (assignable != null && assignable.task != null && assignable.task.icon != null)
						_icon = assignable.task.icon;
				}
				if (_icon == null){
					var iconAtt = this.GetType().NCGetAttribute<IconAttribute>(true);
					if (iconAtt != null) _icon = (Texture2D)Resources.Load(iconAtt.iconName);					
				}
				return _icon;			
			}
		}

		//Is NC in icon mode && node has an icon?
		protected bool inIconMode{
			get {return NCPrefs.iconMode && icon != null;}
		}


		////////////////

		//protect them
		protected void Reset(){
			OnEditorReset();
		}
		protected void OnValidate(){
			UpdateNodeBBFields(graphBlackboard);
			OnEditorValidate();
		}

		virtual protected void OnEditorReset(){}
		virtual protected void OnEditorValidate(){}

		///Get connection information node wise, to show on top of the connection
		virtual public string GetConnectionInfo(Connection c){
			return null;
		}


		//The main function for drawing a node's gui.Fires off others.
		public void ShowNodeGUI(){

			if (isHidden)
				return;

			DrawNodeWindow();
			DrawNodeTag();
			DrawNodeComments();
		}

		//Draw the window
		void DrawNodeWindow(){

			///un-colapse children ui
			if (childrenCollapsed){
				var r = new Rect(nodeRect.x, nodeRect.yMax + 10, nodeRect.width, 20);
				EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
				if (GUI.Button(r, "COLLAPSED", "box"))
					childrenCollapsed = false;
			}

			GUI.color = isActive? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.8f);
			GUI.color = Graph.currentSelection == this? new Color(0.9f, 0.9f, 1) : GUI.color;
			nodeRect = GUILayout.Window (ID, nodeRect, NodeWindowGUI, string.Empty, "window");

			GUI.Box(nodeRect, "", "windowShadow");
			GUI.color = new Color(1,1,1,0.5f);
			GUI.Box(new Rect(nodeRect.x+6, nodeRect.y+6, nodeRect.width, nodeRect.height), "", "windowShadow");

			if (Application.isPlaying && status != Status.Resting){

				if (status == Status.Success)
					GUI.color = successColor;
				else if (status == Status.Running)
					GUI.color = runningColor;
				else if (status == Status.Failure)
					GUI.color = failureColor;
				else if (status == Status.Error)
					GUI.color = Color.red;

				GUI.Box(nodeRect, "", "windowHighlight");
				
			} else {
				
				if (isSelected){
					GUI.color = restingColor;
					GUI.Box(nodeRect, "", "windowHighlight");
				}
			}

			//delete shortcut
			if (isSelected && GUIUtility.keyboardControl == 0 && Event.current.keyCode == KeyCode.Delete && Event.current.type == EventType.KeyUp)
				Graph.PostGUI += delegate{ graph.RemoveNode(this); };

			//Duplicate
			if (isSelected && Event.current.isKey && Event.current.control && Event.current.keyCode == KeyCode.D){
				Graph.PostGUI += delegate{ Graph.currentSelection = Duplicate(); };
				Event.current.Use();
			}

			GUI.color = Color.white;
			EditorGUIUtility.AddCursorRect(nodeRect, MouseCursor.Link);
		}

		//removes the text color that some nodes add with html tags
		string StripNameColor(string name){
			if (string.IsNullOrEmpty(name))
				return name;
			if (name.StartsWith("<") && name.EndsWith(">")){
				name = name.Replace( name.Substring (0, name.IndexOf(">")+1), "" );
				name = name.Replace( name.Substring (name.IndexOf("<"), name.LastIndexOf(">")+1 - name.IndexOf("<")), "" );
			}
			return name;
		}

		//This is the callback function of the GUILayout.window. Everything here is INSIDE the node Window.
		void NodeWindowGUI(int ID){

			////TITLE///
			if (inIconMode){
				GUI.color = EditorGUIUtility.isProSkin? Color.white : new Color(0f,0f,0f, 0.7f);
				GUI.backgroundColor = new Color(0,0,0,0);
				GUILayout.Box(icon);
				GUI.backgroundColor = Color.white;
				GUI.color = Color.white;
			} else {
				var stripedTitle = StripNameColor(name);
				if (stripedTitle != null){
					var title = name;
					var defaultColor = "<color=#eed9a7>";
					if (!EditorGUIUtility.isProSkin){
						title = StripNameColor(title);
						defaultColor = "<color=#222222>";
					}
					GUILayout.Label("<b><size=12>" + defaultColor + title + "</color></size></b>", centerLabel);
				}
			}
			///


			var e = Event.current;

			//Node click
			if (Graph.allowClick && e.button != 2 && e.type == EventType.MouseDown){

				Graph.currentSelection = this;
				nodeIsPressed = true;

				//Double click
				if (e.button == 0 && e.clickCount == 2){
		    		if (this is INestedNode && (this as INestedNode).nestedGraph != null ){
	    				graph.nestedGraphView = (this as INestedNode).nestedGraph;
	    				nodeIsPressed = false;
		    		} else {
			    		AssetDatabase.OpenAsset(MonoScript.FromMonoBehaviour(this));
		    		}
		    		e.Use();
		    	}

		    	//+ control?
		    	if (e.control){
		    		Graph.PostGUI += delegate {graph.primeNode = this; };
		    		e.Use();
		    	}

		    	OnNodePicked();
			}

	    	//Mouse up
	    	if (e.type == EventType.MouseUp){
	    		nodeIsPressed = false;
	    		if (graph.autoSort)
	    			Graph.PostGUI += delegate { SortConnectionsByPositionX(); };
	    		OnNodeReleased();
	    	}

	    	///

	        ////STATUS MARK ICONS////
	        if (Application.isPlaying && status != Status.Resting){
		        var markRect = new Rect(5, 5, 15, 15);
		        if (status == Status.Success){
		        	GUI.color = successColor;
		        	GUI.Box(markRect, "", new GUIStyle("checkMark"));

		        } else if (status == Status.Running){
		        	GUI.Box(markRect, "", new GUIStyle("clockMark"));

		        } else if (status == Status.Failure){
		        	GUI.color = failureColor;
		        	GUI.Box(markRect, "", new GUIStyle("xMark"));
		        }
		    }
	        ///

	        ////NODE GUI////
	        GUI.color = Color.white;
	        GUI.skin = null;
	        GUI.skin.label.richText = true;
	        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUILayout.BeginVertical();

			OnNodeGUI();

			if (this is ITaskAssignable){
				var assignable = this as ITaskAssignable;
				var missing = MissingTask(assignable.serializedTask);
				if (missing != null){
					GUILayout.Label(missing);
				} else {
					var task = (this as ITaskAssignable).task;
					if (task != null){
						GUILayout.Label(NCPrefs.showTaskSummary? task.summaryInfo : string.Format("<b>{0}</b>", task.name));
					} else {
						GUILayout.Label("No Task");
					}
				}
			}

			GUILayout.EndVertical();
			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			////


	    	////CONTEXT MENU////
		    if (Graph.allowClick && e.button == 1 && e.type == EventType.MouseUp){

		    	//Multiselection menu
		    	if (Graph.multiSelection.Count > 0){
		            var menu = new GenericMenu();
		            menu.AddItem (new GUIContent ("Duplicate Selected Nodes"), false, delegate{ DuplicateNodes(Graph.multiSelection.Cast<Node>().ToList()); });
		            menu.AddSeparator("/");
		            menu.AddItem (new GUIContent ("Delete Selected Nodes"), false, delegate{ foreach (Node node in Graph.multiSelection) graph.RemoveNode(node); });
			        menu.ShowAsContext();
			        e.Use();
			        return;

		    	//Single node menu
		    	} else {

		            var menu = new GenericMenu();
		            if (graph.primeNode != this && allowAsPrime)
			            menu.AddItem (new GUIContent ("Set Start (CTRL+Click)"), false, delegate{graph.primeNode = this;});

			        if (this is INestedNode)
			        	menu.AddItem (new GUIContent ("Edit Nested (Double Click)"), false, delegate{graph.nestedGraphView = (this as INestedNode).nestedGraph; } );

					menu.AddItem (new GUIContent ("Duplicate (CTRL+D)"), false, delegate{Duplicate();});

		            if (inConnections.Count > 0)
			            menu.AddItem (new GUIContent (isActive? "Disable" : "Enable"), false, delegate{SetActive(!isActive);});

					if (graph.autoSort && outConnections.Count > 0)
						menu.AddItem (new GUIContent (childrenCollapsed? "Expand Children" : "Collapse Children"), false, delegate{ childrenCollapsed = !childrenCollapsed; });

					if (this is ITaskAssignable){
						var assignable = this as ITaskAssignable;
						if (assignable.task != null){
							menu.AddItem (new GUIContent("Copy Assigned Task"), false, delegate{ Task.copiedTask = assignable.task; });
						} else {
							menu.AddDisabledItem(new GUIContent("Copy Assigned Task"));
						}

						if (Task.copiedTask != null) {
							menu.AddItem (new GUIContent("Paste Assigned Task"), false, delegate{
								
								if (assignable.task == Task.copiedTask)
									return;

								try
								{
									var current = assignable.task;
									assignable.task = Task.copiedTask;
									assignable.task = current;
								}
								catch
								{
									Debug.LogWarning(string.Format("Copied Task '{0}'' is incompatible type for target node '{1}'", Task.copiedTask.name, this.name));
									return;
								}

								if (assignable.task != null){
									if (EditorUtility.DisplayDialog("Paste Task", string.Format("Node already has a Task assigned '{0}'. Replace assigned task with pasted task '{1}'?", assignable.task.name, Task.copiedTask.name), "YES", "NO")){
										Undo.DestroyObjectImmediate(assignable.task);
									} else {
										return;
									}
								}

								assignable.task = Task.copiedTask.CopyTo(graph.nodesRoot.gameObject);
							});

						} else {
							menu.AddDisabledItem(new GUIContent("Paste Assigned Task"));
						}
					}
					
		            OnContextMenu(menu);

					menu.AddSeparator("/");
		            menu.AddItem (new GUIContent ("Delete (DEL)"), false, delegate{graph.RemoveNode(this);});
		            menu.ShowAsContext();
		            e.Use();
		    	}
		    }
		    ///


		    if (Graph.allowClick && e.button != 2){

	    		//drag all selected nodes
	    		if (e.type == EventType.MouseDrag){
	    			for (int i = 0; i < Graph.multiSelection.Count; i++){
	    				var node = (Node)Graph.multiSelection[i];
	    				Undo.RecordObject(node, "Move");
	    				node.nodeRect.center += e.delta;
	    			}
	    		}

	    		Undo.RecordObject(this, "Move");

	    		//snap to grid
		    	if (!NCPrefs.hierarchicalMove && NCPrefs.doSnap && !e.shift && Graph.multiSelection.Count == 0 && nodeIsPressed){
					nodeRect.x = Mathf.Round(nodeRect.x / 15) * 15;
					nodeRect.y = Mathf.Round(nodeRect.y / 15) * 15;
				}		

		    	//recursive drag
		    	if (graph.autoSort && (NCPrefs.hierarchicalMove || e.shift || childrenCollapsed ) && e.type == EventType.MouseDrag && nodeIsPressed)
		    		RecursivePanNode(e.delta);

		    	//single drag
		    	GUI.DragWindow();
		    }
		}

		//The comments of the node sitting next or bottom of it
		void DrawNodeComments(){

			if (NCPrefs.showComments && !string.IsNullOrEmpty(nodeComment)){

				var commentsRect = new Rect();
				var size = new GUIStyle("textArea").CalcSize(new GUIContent(nodeComment));

				if (outConnections.Count == 0){
					size.y = new GUIStyle("textArea").CalcHeight(new GUIContent(nodeComment), nodeRect.width);
					commentsRect = new Rect(nodeRect.x, nodeRect.yMax + 5, nodeRect.width, size.y);
				} else {
					commentsRect = new Rect(nodeRect.xMax + 5, nodeRect.yMin, Mathf.Min(size.x, nodeRect.width), nodeRect.height);
				}

				GUI.color = new Color(1,1,1,0.6f);
				GUI.backgroundColor = new Color(1f,1f,1f,0.2f);
				GUI.Box(commentsRect, nodeComment, "textArea");
				GUI.backgroundColor = Color.white;
				GUI.color = Color.white;
			}
		}

		//Shows the tag label on the left of the node if it is tagged
		void DrawNodeTag(){

			if (!string.IsNullOrEmpty(tag)){
				var size = new GUIStyle("label").CalcSize(new GUIContent(tag));
				var tagRect = new Rect(nodeRect.x - size.x -10, nodeRect.y, size.x, size.y);
				GUI.Label(tagRect, tag);
				tagRect.width = 12;
				tagRect.height = 12;
				tagRect.y += tagRect.height + 5;
				tagRect.x = nodeRect.x - 22;
				GUI.DrawTexture(tagRect, EditorUtils.tagIcon);
			}
		}

		//Function to pan the node with children recursively
		void RecursivePanNode(Vector2 delta){

			nodeRect.center += delta;

			for (int i= 0; i < outConnections.Count; i++){
				var node = outConnections[i].targetNode;
				if (node.ID > this.ID)
					node.RecursivePanNode(delta);
			}
		}

		//The inspector of the node shown in the editor panel or else.
		public void ShowNodeInspectorGUI(){

			Undo.RecordObject(this, "Node Inspector");
			if (NCPrefs.showNodeInfo){
				GUI.backgroundColor = new Color(0.8f,0.8f,1);
				EditorGUILayout.HelpBox(nodeDescription, MessageType.None);
				GUI.backgroundColor = Color.white;
			}

			GUILayout.BeginHorizontal();
			if (!inIconMode && allowAsPrime){
				customName = EditorGUILayout.TextField(customName);
				EditorUtils.TextFieldComment(customName, "Name...");
			}

			tag = EditorGUILayout.TextField(tag );
			EditorUtils.TextFieldComment(tag, "Tag...");
			GUILayout.EndHorizontal();

			nodeComment = EditorGUILayout.TextField(nodeComment);
			EditorUtils.TextFieldComment(nodeComment);

			EditorUtils.Separator();
			OnNodeInspectorGUI();
			TaskAssignableGUI();

			if (GUI.changed)
				EditorUtility.SetDirty(this);
		}


		void TaskAssignableGUI(){

			if (this is ITaskAssignable){

				System.Type taskType = null;
				foreach(System.Type iType in this.GetType().GetInterfaces()) {
				    if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(ITaskAssignable<>)){
				        taskType = iType.GetGenericArguments()[0];
				        break;
				    }
				}

				if (taskType != null){
					var assignable = this as ITaskAssignable;
					string missing = MissingTask(assignable.serializedTask);
					if ( missing != null ){
						GUILayout.Label("Missing: " + missing );
						if (GUILayout.Button("Remove Missing")){
							DestroyImmediate(assignable.serializedTask, true);
							assignable.task = null;
						}
						return;
					}

					if (assignable.task == null){

						EditorUtils.TaskSelectionButton(graph.nodesRoot.gameObject, taskType, delegate(Task t){assignable.task = t;});

					} else {

						assignable.task.ShowInspectorGUI();
					}	
				}
			}
		}

		string MissingTask(Object o){
			if (!Equals(o, null) && o.GetType() == typeof(Object) && o.ToString() != "null"){
				var s = o.ToString();
				s = s.Replace(gameObject.name + " ", "");
				return string.Format("<color=#ff6457>* {0} *</color>", s);
			}
			return null;
		}


		//Duplicate node alone
		public Node Duplicate(Graph targetGraph = null){

			if (targetGraph == null)
				targetGraph = this.graph;

			var newNode = targetGraph.nodesRoot.gameObject.AddComponent(this.GetType()) as Node;
			Undo.RegisterCreatedObjectUndo(newNode, "Duplicate");
			EditorUtility.CopySerialized(this, newNode);

			Undo.RecordObject(targetGraph, "Duplicate");
			targetGraph.allNodes.Add(newNode);

			Undo.RecordObject(newNode, "Duplicate");
			newNode.inConnections.Clear();
			newNode.outConnections.Clear();
			newNode.nodeRect.center += new Vector2(50,50);
			
			var assignable = this as ITaskAssignable;
			if (assignable != null && assignable.task != null)
				(newNode as ITaskAssignable).task = assignable.task.CopyTo(targetGraph.nodesRoot.gameObject);

			newNode.OnEditorValidate();
			return newNode;
		}

		//Duplicate a node along with all hierarchy
		public Node DuplicateHierarchy(Graph targetGraph = null){
			
			if (targetGraph == null)
				targetGraph = this.graph;

			var newNode = Duplicate(targetGraph);
			var dupConnections = new List<Connection>();
			foreach (Connection connection in outConnections)
				dupConnections.Add( connection.Duplicate(newNode, connection.targetNode.DuplicateHierarchy(targetGraph), targetGraph ));
			Undo.RecordObject(newNode, "Duplicate");
			newNode.outConnections = dupConnections;
			return newNode;
		}


		//Duplicate all nodes provided. Connections will duplicate only if target node is contained in the provided list
		public static List<Node> DuplicateNodes(List<Node> nodes){
			var dupNodes = new List<Node>();
			var dupLinks = new Dictionary<Connection, int>();
			foreach (Node node in nodes){
				var dupNode = node.Duplicate();
				dupNodes.Add(dupNode);
				foreach (Connection c in node.outConnections){
					if (nodes.Contains(c.targetNode) )
						dupLinks.Add( c.Duplicate(dupNode, null), nodes.IndexOf(c.targetNode) );
				}
			}
			
			for (int i = 0; i < dupNodes.Count; i++){
				var dupNode = dupNodes[i];
				foreach (Connection c in dupNode.outConnections){
					var targetIndex = dupLinks[c];
					c.RelinkTarget( dupNodes[targetIndex] );
				}
			}

			return dupNodes;
		}


		//Activates/Deactivates all inComming connections
		void SetActive(bool active){

			if (isChecked)
				return;
			
			isChecked = true;

			//just for visual feedback
			if (!active)
				Graph.currentSelection = null;

			//disable all incomming
			foreach (Connection cIn in inConnections){
				Undo.RecordObject(cIn, "SetActive");
				cIn.isActive = active;
			}
			
			//disable all outgoing
			foreach (Connection cOut in outConnections){
				Undo.RecordObject(cOut, "SetActive");
				cOut.isActive = active;
			}

			//if child is still considered active(= at least 1 incomming is active), continue else SetActive child as well
			foreach (Node child in outConnections.Select(c => c.targetNode)){
				
				if (child.isActive == !active)
					continue;

				child.SetActive(active);
			}

			isChecked = false;
		}


		//Sorts the connections based on the child nodes and this node X position. Possible only when not in play mode
		void SortConnectionsByPositionX(){
			
			if (!Application.isPlaying){

				if (isChecked)
					return;

				isChecked = true;

				Undo.RecordObject(this, "Re-Sort");
				outConnections = outConnections.OrderBy(c => c.targetNode.nodeRect.center.x ).ToList();
				foreach(Connection connection in inConnections)
					connection.sourceNode.SortConnectionsByPositionX();

				isChecked = false;
			}
		}


		///Draw an automatic editor inspector for this node.
		protected void DrawDefaultInspector(){
			EditorUtils.ShowAutoEditorGUI(this);
		}
		
		//Editor. When the node is picked
		virtual protected void OnNodePicked(){}
		//Editor. When the node is released (mouse up)
		virtual protected void OnNodeReleased(){}
		///Editor. Override to show controls within the node window
		virtual protected void OnNodeGUI(){}
		//Editor. Override to show controls within the inline inspector or leave it to show an automatic editor
		virtual protected void OnNodeInspectorGUI(){ DrawDefaultInspector(); }
		//Editor. Override to add more entries to the right click context menu of the node
		virtual protected void OnContextMenu(GenericMenu menu){ }

		//Draw the connections line from this node, to all of its children. This is the default hierarchical style. Override in each system's base node class.
		virtual public void DrawNodeConnections(){

			if (isHidden)
				return;

			var e = Event.current;

			//Receive connections first
			if (clickedPort != null && e.type == EventType.MouseUp){

				if (nodeRect.Contains(e.mousePosition)){
					
					if (graph.ConnectNode(clickedPort.parent, this, clickedPort.portIndex) != null){
						clickedPort = null;
						e.Use();
					}

				} else {

					if (ID == graph.allNodes.Count){

						var source = clickedPort.parent;
						var index = clickedPort.portIndex;
						var pos = e.mousePosition;						
						clickedPort = null;
						
						System.Action<System.Type> Selected = delegate(System.Type type){
							var newNode = graph.AddNode(type);
							newNode.nodeRect.center = pos;
							graph.ConnectNode(source, newNode, index);
							newNode.SortConnectionsByPositionX();
						};

						EditorUtils.GetTypeSelectionMenu(graph.baseNodeType, Selected).ShowAsContext();
						e.Use();
					}
				}
			}

			if (maxOutConnections == 0)
				return;

			var nodeOutputBox = new Rect(nodeRect.x, nodeRect.yMax - 4, nodeRect.width, 12);
			GUI.Box(nodeOutputBox, "", new GUIStyle("nodePortContainer"));
			
			if (outConnections.Count < maxOutConnections || maxOutConnections == -1){

				for (int i = 0; i < outConnections.Count + 1; i++){

					var portRect = new Rect(0, 0, 10, 10);
					portRect.center = new Vector2(((nodeRect.width / (outConnections.Count + 1)) * (i + 0.5f)) + nodeRect.xMin, nodeRect.yMax + 6);
					GUI.Box(portRect, "", "nodePortEmpty");

					if (childrenCollapsed)
						continue;

					EditorGUIUtility.AddCursorRect(portRect, MouseCursor.ArrowPlus);
					if (e.button == 0 && e.type == EventType.MouseDown && portRect.Contains(e.mousePosition)){
						clickedPort = new GUIPort(i, this, portRect.center);
						e.Use();
					}
				}
			}

			//draw the new connection line if in link mode
			if (clickedPort != null && clickedPort.parent == this)
				Handles.DrawBezier(clickedPort.pos, e.mousePosition, clickedPort.pos, e.mousePosition, restingColor, null, 2);

			//draw all connected lines
			for (int connectionIndex = 0; connectionIndex < outConnections.Count; connectionIndex++){
				
				var connection = outConnections[connectionIndex];
				if (connection != null){

					var sourcePos = new Vector2(((nodeRect.width / (outConnections.Count + 1)) * (connectionIndex + 1) ) + nodeRect.xMin, nodeRect.yMax + 6);
					var targetPos = new Vector2(connection.targetNode.nodeRect.center.x, connection.targetNode.nodeRect.y);

					var connectedPortRect = new Rect(0,0,12,12);
					connectedPortRect.center = sourcePos;
					GUI.Box(connectedPortRect, "", "nodePortConnected");
			
					if (childrenCollapsed || connection.targetNode.isHidden)
						continue;

					connection.DrawConnectionGUI(sourcePos, targetPos);

					//On right click disconnect connection from the source.
					if (e.button == 1 && e.type == EventType.MouseDown && connectedPortRect.Contains(e.mousePosition)){
						graph.RemoveConnection(connection);
						e.Use();
						return;
					}
				}
			}
		}

		#endif
	}
}
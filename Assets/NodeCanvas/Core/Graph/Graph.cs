#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeCanvas.Variables;

namespace NodeCanvas{

	///This is the base and main class of NodeCanvas and graphs. All graph Systems are deriving from this.
	abstract public partial class Graph : MonoBehaviour, ITaskSystem {

		[SerializeField]
		private string _graphName = string.Empty;
		[SerializeField]
		private Node _primeNode;
		[SerializeField]
		private List<Node> _allNodes = new List<Node>();
		[SerializeField]
		private Component _agent;
		[SerializeField]
		private Blackboard _blackboard;
		[HideInInspector]
		public Transform _nodesRoot;

		private static List<Graph> runningGraphs = new List<Graph>();
		private float timeStarted;
		private System.Action FinishCallback;
		/////
		/////

		new public string name{
			get{return _graphName;}
			set
			{
				if (string.IsNullOrEmpty(_graphName))
					_graphName = gameObject.name;
				_graphName = value;
			}
		}

		public float elapsedTime{
			get {return isRunning || isPaused? Time.time - timeStarted : 0;}
		}

		///The base type of all nodes that can live in this system
		virtual public System.Type baseNodeType{
			get {return typeof(Node);}
		}

		///Is this system allowed to start with a null agent?
		virtual protected bool allowNullAgent{
			get {return false;}
		}

		///Should the the nodes be auto sorted on position x?
		virtual public bool autoSort{
			get {return false;}
		}

		///The node to execute first. aka 'START'
		public Node primeNode{
			get {return _primeNode;}
			set
			{
				if (_primeNode != value){
					if (value != null && value.allowAsPrime == false)
						return;
					
					if (isRunning){
						if (_primeNode != null)	_primeNode.ResetNode();
						if (value != null) value.ResetNode();
					}

					#if UNITY_EDITOR //To save me some sanity
					Undo.RecordObject(this, "Set Start");
					#endif
					
					_primeNode = value;
					UpdateNodeIDsInGraph();
					ReorderNodeList();
				}
			}
		}

		///All nodes assigned to this system
		public List<Node> allNodes{
			get {return _allNodes;}
			private set {_allNodes = value;}
		}

		///The agent currently assigned to the graph
		public Component agent{
			get {return _agent;}
			set
			{
				if (_agent != value){
					_agent = value;
					SendTaskOwnerDefaults();
				}
			}
		}

		///The blackboard currently assigned to the graph
		public Blackboard blackboard{
			get {return _blackboard;}
			set
			{
				if (_blackboard != value){
					_blackboard = value;
					UpdateAllNodeBBFields();
					SendTaskOwnerDefaults();
				}
			}
		}

		///Is the graph running?
		public bool isRunning {get; private set;}

		///Is the graph paused?
		public bool isPaused {get; private set;}

		public Transform nodesRoot{
			get
			{
				if (_nodesRoot == null){
					_nodesRoot = SafeCreateParentedGameObject("__ALLNODES__", this.transform).transform;
					_nodesRoot.gameObject.AddComponent<NodesRootUtility>().parentGraph = this;
				}

				_nodesRoot.gameObject.hideFlags = doHide? HideFlags.HideInHierarchy : 0;
				return _nodesRoot;			
			}
		}

		//Debug purposes
		public static bool doHide{get{return true;}}

		///////
		///////

		//A utility for the root to be able to be created even in a prefab
		static GameObject SafeCreateParentedGameObject(string name, Transform parent){

			var newGO = new GameObject(name);
			#if UNITY_EDITOR
			if (PrefabUtility.GetPrefabType(parent.gameObject) == PrefabType.Prefab){
				var clone = PrefabUtility.InstantiatePrefab(parent.gameObject) as GameObject;
				var root = PrefabUtility.FindPrefabRoot(clone);
				newGO.transform.parent = clone.transform;
				newGO.transform.localPosition = Vector3.zero;
				var index = newGO.transform.GetSiblingIndex();
				PrefabUtility.ReplacePrefab(root, PrefabUtility.GetPrefabParent(root), ReplacePrefabOptions.ConnectToPrefab);
				Object.DestroyImmediate(root, true);
				return parent.GetChild(index).gameObject;
			}
			#endif
			
			newGO.transform.parent = parent;
			newGO.transform.localPosition = Vector3.zero;
			return newGO;
		}

		//Create monomanager and order nodes by IDs
		protected void Awake(){
			MonoManager.Create();
			UpdateNodeIDsInGraph();
			ReorderNodeList();
		}

		protected void OnDestroy(){
			runningGraphs.Remove(this);
			MonoManager.current.RemoveMethod(OnGraphUpdate);
		}

		//This is not really nescessary
		public void ReorderNodeList(){
			allNodes = allNodes.OrderBy(node => node.ID).ToList();
		}

		//Sets all graph Tasks' owner (which is this)
		public void SendTaskOwnerDefaults(){
			foreach (Task task in GetAllTasksOfType<Task>(true))
				task.SetOwnerSystem(this);
		}

		//Update all graph node's BBFields
		private void UpdateAllNodeBBFields(){
			foreach (Node node in allNodes)
				node.UpdateNodeBBFields(blackboard);
		}

		///Sends a OnCustomEvent message to the tasks that needs them. Tasks subscribe to events using [EventListener] attribute
		public void SendEvent(string eventName){
			if (!string.IsNullOrEmpty(eventName) && isRunning && agent != null)
				agent.gameObject.SendMessage("OnCustomEvent", eventName, SendMessageOptions.DontRequireReceiver);
		}

		///Sends an event to all graphs
		public static void SendGlobalEvent(string eventName){
			foreach(Graph graph in runningGraphs)
				graph.SendEvent(eventName);
		}

		public void SendTaskMessage(string name){
			SendTaskMessage(name, null);
		}

		///Send a message to all tasks in this graph and nested graphs.
		public void SendTaskMessage(string name, object argument){
			foreach (Task task in GetAllTasksOfType<Task>(true)){
				var method = task.GetType().NCGetMethod(name);
				if (method != null){
					var args = method.GetParameters().Length == 0? null : new object[]{argument};
					method.Invoke(task, args);
				}
			}
		}

		public void StartGraph(){
			StartGraph(this.agent, this.blackboard, null);
		}

		///Start the graph with the already assigned agent and blackboard
		///optionaly providing a callback for when it is finished
		public void StartGraph(System.Action callback){
			StartGraph(this.agent, this.blackboard, callback);
		}

		public void StartGraph(Component agent){
			StartGraph(agent, this.blackboard, null);
		}

		public void StartGraph(Component agent, System.Action callback){
			StartGraph(agent, this.blackboard, callback);
		}

		public void StartGraph(Component agent, Blackboard blackboard){
			StartGraph(agent, blackboard, null);
		}

		///Start the graph for the agent and blackboard provided.
		///Optionally provide a callback for when the graph stops or ends
		public void StartGraph(Component agent, Blackboard blackboard, System.Action callback){

			if (isRunning){
				if (callback != null)
					FinishCallback += callback;
				Debug.LogWarning("Graph allready Active");
				return;
			}

			if (agent == null && allowNullAgent == false){
				Debug.LogWarning("You've tried to start a graph with null Agent.");
				return;
			}
			
			if (blackboard == null && agent != null){
				Debug.Log("Graph started without blackboard. Looking for blackboard on agent '" + agent.gameObject + "'...", agent.gameObject);
				blackboard = agent.GetComponent<Blackboard>();
				if (blackboard != null)
					Debug.Log("Blackboard found");
			}

			UpdateNodeIDsInGraph();
			ReorderNodeList();
			
			this.blackboard = blackboard;
			this.agent = agent;
			if (callback != null)
				this.FinishCallback = callback;

			isRunning = true;
			if (!isPaused){
				timeStarted = Time.time;
				foreach (Node node in allNodes)
					node.OnGraphStarted();
			}

			isPaused = false;
			runningGraphs.Add(this);
			MonoManager.current.AddMethod(OnGraphUpdate);
			OnGraphStarted();
		}

		///Stops the graph completely and resets all nodes.
		public void StopGraph(){

			if (!isRunning && !isPaused)
				return;

			runningGraphs.Remove(this);
			MonoManager.current.RemoveMethod(OnGraphUpdate);
			isRunning = false;
			isPaused = false;

			OnGraphStoped();
			foreach(Node node in allNodes){
				node.ResetNode(false);
				node.OnGraphStoped();
			}

			if (FinishCallback != null){
				FinishCallback();
				FinishCallback = null;
			}
		}

		///Pauses the graph from updating as well as notifying all nodes and tasks.
		public void PauseGraph(){

			if (!isRunning)
				return;

			runningGraphs.Remove(this);
			MonoManager.current.RemoveMethod(OnGraphUpdate);
			isRunning = false;
			isPaused = true;

			OnGraphPaused();
			foreach (Node node in allNodes)
				node.OnGraphPaused();
		}

		///Override for graph specific stuff to run when the graph is started or resumed
		virtual protected void OnGraphStarted(){}

		///Override for graph specific per frame logic. Called every frame if the graph is running
		virtual protected void OnGraphUpdate(){}

		///Override for graph specific stuff to run when the graph is stoped
		virtual protected void OnGraphStoped(){}

		///Override this for when the graph is paused
		virtual protected void OnGraphPaused(){}


		///Get a node by it's ID, null if not found
		public Node GetNodeWithID(int searchID){
			if (searchID <= allNodes.Count && searchID >= 0){
				foreach (Node node in allNodes){
					if (node.ID == searchID)
						return node;
				}
			}

			return null;
		}

		///Get all nodes of a specific type
		public List<T> GetAllNodesOfType<T>() where T:Node{
			return allNodes.OfType<T>().ToList();
		}

		///Get a node by it's tag name
		public T GetNodeWithTag<T>(string name) where T:Node{
			foreach (T node in allNodes.OfType<T>()){
				if (node.tag == name)
					return node;
			}
			return default(T);
		}

		///Get all nodes taged with such tag name
		public List<T> GetNodesWithTag<T>(string name) where T:Node{
			var nodes = new List<T>();
			foreach (T node in allNodes.OfType<T>()){
				if (node.tag == name)
					nodes.Add(node);
			}
			return nodes;
		}

		///Get all taged nodes regardless tag name
		public List<T> GetAllTagedNodes<T>() where T:Node{
			var nodes = new List<T>();
			foreach (T node in allNodes.OfType<T>()){
				if (!string.IsNullOrEmpty(node.tag))
					nodes.Add(node);
			}
			return nodes;
		}

		///Get a node by it's name
		public T GetNodeWithName<T>(string name) where T:Node{
			foreach(T node in allNodes.OfType<T>()){
				if (StripNameColor(node.name).ToLower() == name.ToLower())
					return node;
			}
			return default(T);
		}

		//removes the text color that some nodes add with html tags
		string StripNameColor(string name){
			if (name.StartsWith("<") && name.EndsWith(">")){
				name = name.Replace( name.Substring (0, name.IndexOf(">")+1), "" );
				name = name.Replace( name.Substring (name.IndexOf("<"), name.LastIndexOf(">")+1 - name.IndexOf("<")), "" );
			}
			return name;
		}

		///Get all nodes of the graph that have no incomming connections
		public List<Node> GetRootNodes(){
			var rootNodes = new List<Node>();
			foreach(Node node in allNodes){
				if (node.inConnections.Count == 0)
					rootNodes.Add(node);
			}

			return rootNodes;
		}

		///Get all assigned node Tasks in the graph
		public List<T> GetAllTasksOfType<T>(bool includeNested) where T:Task{
			return (includeNested? this.transform : nodesRoot ).GetComponentsInChildren<T>(true).ToList();
		}

		///Get all Nested graphs of this graph
		public List<T> GetAllNestedGraphs<T>(bool recursive) where T:Graph{

			var graphs = new List<T>();
			foreach (INestedNode node in allNodes.OfType<INestedNode>()){

				if (node.nestedGraph != null && !graphs.Contains((T)node.nestedGraph) ){

					if (node.nestedGraph is T)
						graphs.Add((T)node.nestedGraph);

					if (recursive)
						graphs.AddRange( node.nestedGraph.GetAllNestedGraphs<T>(recursive) );
				}
			}

			return graphs;
		}


		///Update the IDs of the nodes in the graph. Is automatically called whenever a change happens in the graph by the adding removing connecting etc.
		public void UpdateNodeIDsInGraph(){

			var lastID = 0;

			//start with the prime node
			if (primeNode != null)
				lastID = primeNode.AssignIDToGraph(lastID);

			//then set remaining nodes that are not connected
			for (int i = 0; i < allNodes.Count; i++){
				if (allNodes[i].inConnections.Count == 0)
					lastID = allNodes[i].AssignIDToGraph(lastID);
			}

			//reset the check
			for (int i = 0; i < allNodes.Count; i++)
				allNodes[i].ResetRecursion();
		}


		///Add a new node to this graph
		public T AddNode<T>() where T : Node{
			return (T)AddNode(typeof(T));
		}

		public T AddNode<T>(Vector2 pos) where T : Node{
			return (T)AddNode(typeof(T), pos);
		}

		public Node AddNode(System.Type nodeType){
			return AddNode(nodeType, new Vector2(50,50));
		}

		///Add a new node to this graph
		public Node AddNode(System.Type nodeType, Vector2 pos){

			if ( !nodeType.IsSubclassOf(baseNodeType) ){
				Debug.Log(nodeType + " can't be added to " + this.GetType() + " graph");
				return null;
			}

			var newNode = Node.Create(this, nodeType);

			#if UNITY_EDITOR
			newNode.nodeRect.center = pos;
			Undo.RegisterCreatedObjectUndo(newNode, "New Node");
			Undo.RecordObject(this, "New Node");
			#endif

			allNodes.Add(newNode);

			if (primeNode == null)
				primeNode = newNode;

			#if UNITY_EDITOR
			Undo.RecordObject(this, "New Node");
			#endif

			UpdateNodeIDsInGraph();
			return newNode;
		}

		///Disconnects and then removes a node from this graph by ID
		public void RemoveNode(int id){
			RemoveNode(GetNodeWithID(id));
		}

		///Disconnects and then removes a node from this graph
		public void RemoveNode(Node node){
 
			if (!allNodes.Contains(node)){
				Debug.LogWarning("Node is not part of this graph", gameObject);
				return;
			}

			#if UNITY_EDITOR
			//auto reconnect parent & child of deleted node
			if (autoSort && node.inConnections.Count == 1 && node.outConnections.Count == 1){
				var relinkNode = node.outConnections[0].targetNode;
				RemoveConnection(node.outConnections[0]);
				node.inConnections[0].RelinkTarget(relinkNode);
			}
			#endif

			//disconnect children
			foreach (Connection outConnection in node.outConnections.ToArray())
				RemoveConnection(outConnection);

			//disconnect parents
			foreach (Connection inConnection in node.inConnections.ToArray())
				RemoveConnection(inConnection);

			#if UNITY_EDITOR
			Undo.RecordObject(this, "Delete Node");
			#endif
			allNodes.Remove(node);
			UpdateNodeIDsInGraph();
			
			#if UNITY_EDITOR
			Undo.DestroyObjectImmediate(node);
			#else
			DestroyImmediate(node, true);
			#endif

			if (node == primeNode)
				primeNode = GetNodeWithID(1);

			#if UNITY_EDITOR
			//handle nested graphs
			var nestNode = node as INestedNode;
			if (nestNode != null && nestNode.nestedGraph != null){
				var isPrefab = PrefabUtility.GetPrefabType(nestNode.nestedGraph) == PrefabType.Prefab;
				if (!isPrefab && EditorUtility.DisplayDialog("Deleting Nested Node", "Delete assign nested graph '" + nestNode.nestedGraph.name + "' as well?", "Yes", "No"))
					Undo.DestroyObjectImmediate(nestNode.nestedGraph.gameObject);
			}

			//handle assigned tasks
			var assignable = node as ITaskAssignable;
			if (assignable != null && assignable.task != null)
				Undo.DestroyObjectImmediate(assignable.task);
			#endif
		}
		
		///Connect two nodes together to the next available port of the source node
		public Connection ConnectNode(Node sourceNode, Node targetNode){
			return ConnectNode(sourceNode, targetNode, sourceNode.outConnections.Count);
		}

		///Connect two nodes together to a specific port index of the source node
		public Connection ConnectNode(Node sourceNode, Node targetNode, int indexToInsert){

			if (targetNode.IsNewConnectionAllowed(sourceNode) == false)
				return null;

			#if UNITY_EDITOR
			Undo.RecordObject(sourceNode, "New Connection");
			Undo.RecordObject(targetNode, "New Connection");
			#endif

			var newConnection = Connection.Create(sourceNode, targetNode, indexToInsert);
			
			#if UNITY_EDITOR
			Undo.RegisterCreatedObjectUndo(newConnection, "New Connection");
			Undo.RecordObject(sourceNode, "New Connection");
			Undo.RecordObject(targetNode, "New Connection");
			#endif

			sourceNode.OnChildConnected(indexToInsert);
			targetNode.OnParentConnected(targetNode.inConnections.IndexOf(newConnection));

			#if UNITY_EDITOR
			Undo.RecordObject(this, "New Connection");
			#endif

			UpdateNodeIDsInGraph();
			return newConnection;
		}

		///Removes a connection
		public void RemoveConnection(Connection connection){

			//for live editing convenience
			if (Application.isPlaying)
				connection.ResetConnection();

			#if UNITY_EDITOR
			Undo.RecordObject(connection.sourceNode, "Delete Connection");
			Undo.RecordObject(connection.targetNode, "Delete Connection");
			#endif

			//callbacks
			connection.sourceNode.OnChildDisconnected(connection.sourceNode.outConnections.IndexOf(connection));
			connection.targetNode.OnParentDisconnected(connection.targetNode.inConnections.IndexOf(connection));
			connection.sourceNode.outConnections.Remove(connection);
			connection.targetNode.inConnections.Remove(connection);

			
			#if UNITY_EDITOR
			Undo.DestroyObjectImmediate(connection);
			Undo.RecordObject(this, "Delete Connection");
			#else
			DestroyImmediate(connection, true);
			#endif

			#if UNITY_EDITOR
			//handle connection assigned tasks
			var assignable = connection as ITaskAssignable;
			if (assignable != null && assignable.task != null)
				Undo.DestroyObjectImmediate(assignable.task);
			#endif

			UpdateNodeIDsInGraph();
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public string graphComments = string.Empty;
		private Rect blackboardRect = new Rect(15, 55, 0, 0);
		private Rect inspectorRect  = new Rect(15, 55, 0, 0);
		private Vector2 inspectorScrollPos;
		private Graph _nestedGraphView;

		[SerializeField]
		public bool hasUpgraded_1_6 = false;

		private static Object _currentSelection;
		private static List<Object> _multiSelection = new List<Object>();

		public static System.Action PostGUI{get;set;}
		public static bool allowClick{get;set;}
		public static bool useExternalInspector{get;set;}

		//responsible for the breacrumb navigation
		public Graph nestedGraphView{
			get {return _nestedGraphView;}
			set
			{
				Undo.RecordObject(this, "Change View");
				if (value != null){
					value.nestedGraphView = null;
					value.agent = this.agent;
					value.blackboard = this.blackboard;
				}
				_nestedGraphView = value;
				currentSelection = null;
			}
		}

		//Selected Node or Connection
		public static Object currentSelection{
			get
			{
				if (multiSelection.Count > 1)
					return null;
				if (multiSelection.Count == 1)
					return multiSelection[0];
				if (_currentSelection as Object == null)
					return null;
				return _currentSelection;
			}
			set
			{
				if (!multiSelection.Contains(value))
					multiSelection.Clear();
				GUIUtility.keyboardControl = 0;
				_currentSelection = value;
				SceneView.RepaintAll(); //for gizmos
			}
		}

		public static List<Object> multiSelection{
			get {return _multiSelection;}
			set
			{
				if (value.Count == 1){
					currentSelection = value[0];
					value.Clear();
				}
				_multiSelection = value;
			}
		}

		private static Node focusedNode{
			get {return currentSelection as Node;}
		}

		private static Connection focusedConnection{
			get	{return currentSelection as Connection;}
		}

		///

		[ContextMenu("Reset")]
		protected void Reset(){
			hasUpgraded_1_6 = true;
			_nodesRoot = nodesRoot;
		}
		[ContextMenu("Copy Component")]
		protected void CopyComponent(){ Debug.Log("Unsupported"); }
		[ContextMenu("Paste Component Values")]
		protected void PasteComponentValues(){ Debug.Log("Unsupported"); }
		protected void OnValidate(){}


		///Get all BBVariables along with the object they live on. Defined means that the BBVariable is set to read or write to/from a Blackboard
		public Dictionary<object, BBVariable> GetDefinedBBVariables(){

			var bbVars = new Dictionary<object, BBVariable>();
			var objects = new List<object>();
			objects.AddRange(GetAllTasksOfType<Task>(false).Cast<object>());
			objects.AddRange(GetAllNodesOfType<Node>().Cast<object>());

			foreach (object o in objects){

				if (typeof(Task).NCIsAssignableFrom(o.GetType())){
					var task = (Task)o;
					if (task.agentIsOverride && !string.IsNullOrEmpty(task.overrideAgentParameterName) )
						bbVars[o] = typeof(Task).GetField("taskAgent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(task) as BBVariable;
				}

				foreach (FieldInfo field in o.GetType().NCGetFields()){

					if (typeof(BBVariable).NCIsAssignableFrom(field.FieldType)){
						var bbVar = field.GetValue(o) as BBVariable;
						if (bbVar != null && bbVar.useBlackboard && !bbVar.isNone)
							bbVars[o] = bbVar;
					}

					if (field.FieldType == typeof(BBVariableSet)){
						var varSet = field.GetValue(o) as BBVariableSet;
						if (varSet != null){
							var bbVar = varSet.selectedBBVariable;
							if (bbVar != null && bbVar.useBlackboard && !bbVar.isNone)
								bbVars[o] = bbVar;
						}
					}
				}
			}

			return bbVars;
		}



		///Create a new nested graph for the provided INestedNode parent.
		public static Graph CreateNested(INestedNode parent, System.Type type, string name){

			if (PrefabUtility.GetPrefabType(parent as Node) == PrefabType.Prefab){
				Debug.LogWarning("Automaticaly creating new nested graph when editing a prefab asset is disabled for safety\nPlease create and assign a graph manualy");
				return null;
			}

			Graph newGraph = null;
			if (parent != null){
				Undo.RecordObject(parent as Node, "New Graph");
				newGraph = SafeCreateParentedGameObject(name, (parent as Node).graph.transform).AddComponent(type) as Graph;
			} else {
				newGraph = new GameObject(name).AddComponent(type) as Graph;
			}

			Undo.RegisterCreatedObjectUndo(newGraph.gameObject, "New Graph");
			newGraph.name = name;
			newGraph.transform.localPosition = Vector3.zero;
			parent.nestedGraph = newGraph;
			return newGraph;
		}

		///Clears the whole graph
		public void ClearGraph(){

			foreach(INestedNode node in allNodes.OfType<INestedNode>() ){
				if (node.nestedGraph && node.nestedGraph.transform.parent == this.transform){
					Undo.RecordObject(node as Node, "Delete Nested");
					Undo.DestroyObjectImmediate(node.nestedGraph.gameObject);
				}
			}

			foreach (Node node in allNodes.ToList())
				RemoveNode(node);
		}

		//This is called while within Begin/End windows and ScrollArea from the GraphEditor. This is the main function that calls others
		public void ShowNodesGUI(Rect drawCanvas){

			var e = Event.current;

			GUI.color = Color.white;
			GUI.backgroundColor = Color.white;

			UpdateNodeIDsInGraph();
			ReorderNodeList();
		
			foreach (Node node in allNodes){

				//Panning nodes
				if ( (e.button == 2 && e.type == EventType.MouseDrag) || (e.button == 0 && e.type == EventType.MouseDrag && e.shift && e.isMouse) )	{
					Undo.RecordObject(node, "Move");
					node.nodeRect.center += e.delta;
				}

				if (RectContainsRect(drawCanvas, node.nodeRect))
					node.ShowNodeGUI();
			}

			//This better be done in seperate pass
			foreach (Node node in allNodes)
				node.DrawNodeConnections();

			if (primeNode != null)
				GUI.Box(new Rect(primeNode.nodeRect.x, primeNode.nodeRect.y - 20, primeNode.nodeRect.width, 20), "<b>START</b>");
		}

		//Is rect B marginaly contained inside rect A?
		bool RectContainsRect(Rect a, Rect b){
			return b.xMin <= a.xMax && b.xMax >= a.xMin && b.yMin <= a.yMax && b.yMax >= a.yMin;
		}

		//This is called outside of windows
		public void ShowGraphControls(Vector2 scrollOffset){

			ShowToolbar();
			ShowInspectorGUI();
			ShowBlackboardGUI();
			ShowGraphCommentsGUI();
			DoGraphControls(scrollOffset);
			AcceptDrops(scrollOffset);

			if (PostGUI != null){
				PostGUI();
				PostGUI = null;
			}
		}

		void AcceptDrops(Vector2 scrollOffset){
			var e = Event.current;
			if (e.type == EventType.DragUpdated && DragAndDrop.objectReferences.Length == 1){
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}

			if (e.type == EventType.DragPerform){
				if (DragAndDrop.objectReferences.Length != 1)
					return;
				DragAndDrop.AcceptDrag();
				OnDropAccepted(DragAndDrop.objectReferences[0], e.mousePosition + scrollOffset);
			}
		}

		///Handles drag and drop objects in the graph
		virtual protected void OnDropAccepted(Object o, Vector2 mousePos){
			Debug.Log("This Graph doesn't handle drops of type '" + (o != null? o.GetType().Name : "NULL") + "'");
		}

		//This is called outside Begin/End Windows from GraphEditor.
		void ShowToolbar(){

			var e = Event.current;
		
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUI.backgroundColor = new Color(1f,1f,1f,0.5f);

			if (agent != null && GUILayout.Button("Select Owner", EditorStyles.toolbarButton, GUILayout.Width(80)))
				Selection.activeObject = agent;

			if (GUILayout.Button("Select Graph", EditorStyles.toolbarButton, GUILayout.Width(80)))
				Selection.activeObject = this;

			GUILayout.Space(10);

			if (GUILayout.Button("Preferences", new GUIStyle(EditorStyles.toolbarDropDown))){
				var menu = new GenericMenu();
				menu.AddItem (new GUIContent ("Icon Mode"), NCPrefs.iconMode, delegate{NCPrefs.iconMode = !NCPrefs.iconMode;});
				menu.AddItem (new GUIContent ("Show Task Summary Info"), NCPrefs.showTaskSummary, delegate{NCPrefs.showTaskSummary = !NCPrefs.showTaskSummary;});
				menu.AddItem (new GUIContent ("Node Help"), NCPrefs.showNodeInfo, delegate{NCPrefs.showNodeInfo = !NCPrefs.showNodeInfo;});
				menu.AddItem (new GUIContent ("Grid Snap"), NCPrefs.doSnap, delegate{NCPrefs.doSnap = !NCPrefs.doSnap;});
				menu.AddItem (new GUIContent ("Show Comments"), NCPrefs.showComments, delegate{NCPrefs.showComments = !NCPrefs.showComments;});
				if (autoSort)
					menu.AddItem (new GUIContent ("Automatic Hierarchical Move"), NCPrefs.hierarchicalMove, delegate{NCPrefs.hierarchicalMove = !NCPrefs.hierarchicalMove;});
				menu.AddItem (new GUIContent ("Curve Mode/Smooth"), NCPrefs.curveMode == 0, delegate{NCPrefs.curveMode = 0;});
				menu.AddItem (new GUIContent ("Curve Mode/Stepped"), NCPrefs.curveMode == 1, delegate{NCPrefs.curveMode = 1;});
				menu.ShowAsContext();
			}

			GUILayout.Space(10);
			GUILayout.FlexibleSpace();

			NCPrefs.showBlackboard = GUILayout.Toggle(NCPrefs.showBlackboard, "Blackboard", EditorStyles.toolbarButton);
			GUILayout.Space(1);
			NCPrefs.isLocked = GUILayout.Toggle(NCPrefs.isLocked, "Lock", EditorStyles.toolbarButton);
			GUILayout.Space(1);

			GUI.backgroundColor = new Color(1, 0.8f, 0.8f, 1);
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50))){
				if (EditorUtility.DisplayDialog("Clear Canvas", "This will delete all nodes of the currently viewing graph!\nAre you sure?", "YES", "NO!")){
					ClearGraph();
					e.Use();
					return;
				}
			}

			GUILayout.EndHorizontal();
			GUI.backgroundColor = Color.white;
		}

		void DoGraphControls(Vector2 scrollOffset){

			var e = Event.current;
			//variable is set as well, so that  nodes know if they can be clicked
			allowClick = !inspectorRect.Contains(e.mousePosition) && !blackboardRect.Contains(e.mousePosition);
			if (allowClick){
	
				//canvas click to deselect all
				if (e.button == 0 && e.isMouse && e.type == EventType.MouseDown){
					currentSelection = null;
					return;
				}

				//right click canvas context menu. Basicaly for adding new nodes
				if (e.button == 1 && e.type == EventType.MouseDown){
					var pos = e.mousePosition + scrollOffset;
					System.Action<System.Type> Selected = delegate(System.Type type){
						currentSelection = AddNode(type, pos);
					};

					var menu = EditorUtils.GetTypeSelectionMenu(baseNodeType, Selected);
					menu = OnCanvasContextMenu(menu, e.mousePosition + scrollOffset);
					menu.ShowAsContext();
					e.Use();
				}
			}
		}

		///Override to add extra context sensitive options in the right click canvas context menu
		virtual protected GenericMenu OnCanvasContextMenu(GenericMenu menu, Vector2 mousePos){
			return menu;
		}

		//Show the comments window
		void ShowGraphCommentsGUI(){

			if (NCPrefs.showComments && !string.IsNullOrEmpty(graphComments)){
				GUI.backgroundColor = new Color(1f,1f,1f,0.3f);
				GUI.Box(new Rect(10, Screen.height - 100, 330, 60), graphComments, (GUIStyle)"textArea");
				GUI.backgroundColor = Color.white;
			}
		}

		//This is the window shown at the top left with a GUI for extra editing opions of the selected node.
		void ShowInspectorGUI(){
			
			if ( (!focusedNode && !focusedConnection) || useExternalInspector){
				inspectorRect.height = 0;
				return;
			}

			inspectorRect.width = 330;
			inspectorRect.x = 10;
			inspectorRect.y = 30;
			GUISkin lastSkin = GUI.skin;
			GUI.Box(inspectorRect, "", "windowShadow");

			var viewRect = new Rect(inspectorRect.x + 1, inspectorRect.y, inspectorRect.width + 18, Screen.height - inspectorRect.y - 30);
			inspectorScrollPos = GUI.BeginScrollView(viewRect, inspectorScrollPos, inspectorRect);

			GUILayout.BeginArea(inspectorRect, (focusedNode? focusedNode.name : "Connection"), (GUIStyle)"editorPanel");
			GUILayout.Space(5);
			GUI.skin = null;

			if (focusedNode)
				focusedNode.ShowNodeInspectorGUI();
			else
			if (focusedConnection)
				focusedConnection.ShowConnectionInspectorGUI();

			GUILayout.Box("", GUILayout.Height(5), GUILayout.Width(inspectorRect.width - 10));
			GUI.skin = lastSkin;
			if (Event.current.type == EventType.Repaint)
				inspectorRect.height = GUILayoutUtility.GetLastRect().yMax + 5;

			GUILayout.EndArea();
			GUI.EndScrollView();

			if (GUI.changed && currentSelection != null)
				EditorUtility.SetDirty(currentSelection);
		}


		//Show the target blackboard window
		void ShowBlackboardGUI(){

			if (!NCPrefs.showBlackboard || blackboard == null){
				blackboardRect.height = 0;
				return;
			}

			blackboardRect.width = 330;
			blackboardRect.x = Screen.width - 350;
			blackboardRect.y = 30;
			GUISkin lastSkin = GUI.skin;
			GUI.Box(blackboardRect, "", "windowShadow" );

			GUILayout.BeginArea(blackboardRect, "Variables", (GUIStyle)"editorPanel");
			GUILayout.Space(5);
			GUI.skin = null;

			blackboard.ShowVariablesGUI();

			GUILayout.Box("", GUILayout.Height(5), GUILayout.Width(blackboardRect.width - 10));
			GUI.skin = lastSkin;
			if (Event.current.type == EventType.Repaint)
				blackboardRect.height = GUILayoutUtility.GetLastRect().yMax + 5;
			GUILayout.EndArea();		
		}

        class BBVarInfo{
            public System.Type type;
            public int count;
            public List<object> objects = new List<object>();
            public bool dynamic;
        }

        public void ShowDefinedBBVariablesGUI(){

            var varInfo = new Dictionary<string, BBVarInfo>();
            foreach (KeyValuePair<object, BBVariable> pair in GetDefinedBBVariables()){
                if (!varInfo.ContainsKey(pair.Value.dataName))
                    varInfo[pair.Value.dataName] = new BBVarInfo();
                varInfo[pair.Value.dataName].type = pair.Value.varType;
                varInfo[pair.Value.dataName].dynamic = pair.Value.isDynamic;
                varInfo[pair.Value.dataName].count ++;
                varInfo[pair.Value.dataName].objects.Add(pair.Key);
            }

            EditorUtils.BoldSeparator();
            EditorUtils.CoolLabel("Defined Parameters");
            EditorUtils.Separator();
            
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true));
            GUI.color = Color.yellow;
            GUILayout.Label("Name");
            GUI.color = Color.white;
            foreach (KeyValuePair<string, BBVarInfo> pair in varInfo)
                GUILayout.Label(string.Format(pair.Value.dynamic? "<i>{0}</i>" : "{0}", pair.Key));
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true));
            GUI.color = Color.yellow;            
            GUILayout.Label("Type");
            GUI.color = Color.white;
            foreach (KeyValuePair<string, BBVarInfo> pair in varInfo)
                GUILayout.Label(EditorUtils.TypeName(pair.Value.type) );
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true));
            GUI.color = Color.yellow;
            GUILayout.Label("Occurencies");
            GUI.color = Color.white;
            foreach (KeyValuePair<string, BBVarInfo> pair in varInfo)
                GUILayout.Label(pair.Value.count.ToString());
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }


		void OnDrawGizmos(){

			foreach (Task task in GetAllTasksOfType<Task>(true))
				task.DrawGizmos();

			if (focusedNode && focusedNode is ITaskAssignable && ((ITaskAssignable)focusedNode).task != null )
				(focusedNode as ITaskAssignable).task.DrawGizmosSelected();
		}

		#endif
	}
}
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;
using System.Linq;

namespace NodeCanvas {

	///Base class for connections. This can be used as an identity connection
	[AddComponentMenu("")]
	public partial class Connection : MonoBehaviour{

		[SerializeField]
		private Node _sourceNode;
		[SerializeField]
		private Node _targetNode;
		[SerializeField]
		private bool _isDisabled;
		private Status _connectionStatus = Status.Resting;

		///Create a new Connection
		public static Connection Create(Node source, Node target, int sourceIndex){
			var newConnection = source.graph.nodesRoot.gameObject.AddComponent(source.outConnectionType) as Connection;
			newConnection.sourceNode = source;
			newConnection.targetNode = target;
			newConnection.sourceNode.outConnections.Insert(sourceIndex, newConnection);
			newConnection.targetNode.inConnections.Add(newConnection);
			newConnection.OnCreate(sourceIndex, target.inConnections.IndexOf(newConnection));
			return newConnection;
		}

		///Called when connection is created
		virtual protected void OnCreate(int sourceIndex, int targetIndex){}

		
		///The source node of the connection
		public Node sourceNode{
			get {return _sourceNode; }
			private set {_sourceNode = value;}
		}

		///The target node of the connection
		public Node targetNode{
			get {return _targetNode; }
			private set {_targetNode = value;}
		}

		///The connection status
		public Status connectionStatus{
			get {return _connectionStatus;}
			set {_connectionStatus = value;}
		}

		///Is the connection active?
		public bool isActive{
			get	{return !_isDisabled;}
			set
			{
				if (!_isDisabled && value == false)
					ResetConnection();
				_isDisabled = !value;
			}
		}

		///The graph this connection belongs to taken from the source node.
		protected Graph graph{
			get {return sourceNode.graph;}
		}

		///The current agent of the graph taken from the source node.
		protected Component graphAgent{
			get {return graph != null? graph.agent : null;}
		}

		///The current blackboard of the graph taken from the source node.
		protected Blackboard graphBlackboard{
			get {return graph != null? graph.blackboard : null;}
		}

		///////////
		///////////

		public Status Execute(){
			return Execute(graphAgent, graphBlackboard);
		}

		public Status Execute(Component agent){
			return Execute(agent, graphBlackboard);
		}

		///Execute the conneciton for the specified agent and blackboard.
		public Status Execute(Component agent, Blackboard blackboard){

			if (!isActive)
				return Status.Resting;

			connectionStatus = OnExecute(agent, blackboard);
			return connectionStatus;
		}

		public void ResetConnection(){
			ResetConnection(true);
		}

		///Resets the connection and its targetNode, optionaly recursively
		public void ResetConnection(bool recursively){

			if (connectionStatus == Status.Resting)
				return;

			connectionStatus = Status.Resting;
			OnReset();

			if (recursively)
				targetNode.ResetNode(recursively);
		}

		///Overide to derived connection types. By default returns whatever targetNode.Execute returns
		virtual protected Status OnExecute(Component agent, Blackboard blackboard){
			return targetNode.Execute(agent, blackboard);
		}

		///Called when the connection is reset
		virtual protected void OnReset(){}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		private Rect areaRect           = new Rect(0,0,50,10);
		private Status lastStatus       = Status.Resting;
		private Color connectionColor   = Node.restingColor;
		private float lineSize          = 3;
		private bool nowSwitchingColors = false;
		private Vector3 lineFromTangent = Vector3.zero;
		private Vector3 lineToTangent   = Vector3.zero;
		private bool isRelinking        = false;
		[SerializeField]
		private bool _connectionInfoExpanded = true;

		readonly private static float defaultLineSize = 3;

		public enum TipConnectionStyle{
			None,
			Circle,
			Arrow
		}

		virtual protected TipConnectionStyle tipConnectionStyle{
			get {return TipConnectionStyle.Circle;}
		}

		public Connection Duplicate(Node newSource, Node newTarget, Graph targetGraph = null){
			
			if (targetGraph == null)
				targetGraph = this.graph;

			var newConnection = targetGraph.nodesRoot.gameObject.AddComponent(this.GetType()) as Connection;
			Undo.RegisterCreatedObjectUndo(newConnection, "Duplicate");
			EditorUtility.CopySerialized(this, newConnection);

			Undo.RecordObject(newConnection, "Duplicate");
			var assignable = this as ITaskAssignable;
			if (assignable != null && assignable.task != null)
				(newConnection as ITaskAssignable).task = assignable.task.CopyTo(targetGraph.nodesRoot.gameObject);

			newConnection.Relink(newSource, newTarget);
			return newConnection;
		}

		//Relinks the source node of the connection
		public void RelinkSource(Node newSource){
			Undo.RecordObject(sourceNode, "Relink");
			sourceNode.outConnections.Remove(this);
			if (newSource != null){
				Undo.RecordObject(newSource, "Relink");
				newSource.outConnections.Add(this);
			}
			Undo.RecordObject(this, "Relink");
			sourceNode = newSource;			
		}

		//Relinks the target node of the connection
		public void RelinkTarget(Node newTarget){
			if (targetNode != null){
				Undo.RecordObject(targetNode, "Relink");
				targetNode.inConnections.Remove(this);
			}
			if (newTarget != null){
				Undo.RecordObject(newTarget, "Relink");
				newTarget.inConnections.Add(this);
			}
			Undo.RecordObject(this, "Relink");
			targetNode = newTarget;
		}

		//Relink both source and target node of the connection
		public void Relink(Node newSource, Node newTarget){
			RelinkSource(newSource);
			RelinkTarget(newTarget);
		}


		//Draw connection from-to
		public void DrawConnectionGUI(Vector3 lineFrom, Vector3 lineTo){
			
			//curveMode 0 is smooth
			var mlt = NCPrefs.curveMode == 0? 0.8f : 1f;
			var tangentX = Mathf.Abs(lineFrom.x - lineTo.x) * mlt;
			var tangentY = Mathf.Abs(lineFrom.y - lineTo.y) * mlt;

			GUI.color = connectionColor;
			var arrowRect = new Rect(0,0,15,15);
			arrowRect.center = lineTo;

			var hor = 0;

			if (lineFrom.x <= sourceNode.nodeRect.x){
				lineFromTangent = new Vector3(-tangentX, 0, 0);
				hor--;
			}

			if (lineFrom.x >= sourceNode.nodeRect.xMax){
				lineFromTangent = new Vector3(tangentX, 0, 0);
				hor++;
			}

			if (lineFrom.y <= sourceNode.nodeRect.y)
				lineFromTangent = new Vector3(0, -tangentY, 0);

			if (lineFrom.y >= sourceNode.nodeRect.yMax)
				lineFromTangent = new Vector3(0, tangentY, 0);


			if (lineTo.x <= targetNode.nodeRect.x){
				lineToTangent = new Vector3(-tangentX, 0, 0);
				hor--;
				if (tipConnectionStyle == TipConnectionStyle.Circle)
					GUI.Box(arrowRect, "", "circle");
				else
				if (tipConnectionStyle == TipConnectionStyle.Arrow)
					GUI.Box(arrowRect, "", "arrowRight");
			}

			if (lineTo.x >= targetNode.nodeRect.xMax){
				lineToTangent = new Vector3(tangentX, 0, 0);
				hor++;
				if (tipConnectionStyle == TipConnectionStyle.Circle)
					GUI.Box(arrowRect, "", "circle");
				else
				if (tipConnectionStyle == TipConnectionStyle.Arrow)
					GUI.Box(arrowRect, "", "arrowLeft");
			}

			if (lineTo.y <= targetNode.nodeRect.y){
				lineToTangent = new Vector3(0, -tangentY, 0);
				if (tipConnectionStyle == TipConnectionStyle.Circle)
					GUI.Box(arrowRect, "", "circle");
				else
				if (tipConnectionStyle == TipConnectionStyle.Arrow)
					GUI.Box(arrowRect, "", "arrowBottom");
			}

			if (lineTo.y >= targetNode.nodeRect.yMax){
				lineToTangent = new Vector3(0, tangentY, 0);
				if (tipConnectionStyle == TipConnectionStyle.Circle)
					GUI.Box(arrowRect, "", "circle");
				else
				if (tipConnectionStyle == TipConnectionStyle.Arrow)
					GUI.Box(arrowRect, "", "arrowTop");
			}

			GUI.color = Color.white;

			///

			var e = Event.current;

			var outPortRect = new Rect(0,0,12,12);
			outPortRect.center = lineFrom;

			if (!Application.isPlaying){
				connectionColor = Node.restingColor;
				lineSize = Graph.currentSelection == this? 5 : defaultLineSize;
			}

			//On click select this connection
			if ( (Graph.allowClick && e.type == EventType.MouseDown && e.button == 0) && (areaRect.Contains(e.mousePosition) || outPortRect.Contains(e.mousePosition) )){
				if (!outPortRect.Contains(e.mousePosition))
					isRelinking = true;
				Graph.currentSelection = this;
				e.Use();
				return;
			}

			if (isRelinking){
				Handles.DrawBezier(areaRect.center, e.mousePosition, areaRect.center, e.mousePosition, new Color(1,1,1,0.5f), null, 2);
				if (e.type == EventType.MouseUp){
					foreach(Node node in graph.allNodes){
						if (node.nodeRect.Contains(e.mousePosition) && node.IsNewConnectionAllowed(sourceNode) ){
							RelinkTarget(node);
							break;
						}
					}
					isRelinking = false;
					e.Use();
				} 
			}

			//with delete key, remove connection
			if (Graph.currentSelection == this && e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && GUIUtility.keyboardControl == 0){
				Graph.PostGUI += delegate { graph.RemoveConnection(this); };
				e.Use();
				return;
			}

			connectionColor = isActive? connectionColor : new Color(0.3f, 0.3f, 0.3f);

			//check this != null for when in playmode user removes a running connection
			if (Application.isPlaying && this != null && connectionStatus != lastStatus && !nowSwitchingColors && isActive){
				MonoManager.current.StartCoroutine(ChangeLineColorAndSize());
				lastStatus = connectionStatus;
			}

			Handles.color = connectionColor;
			if (NCPrefs.curveMode == 0){
				var shadow = new Vector3(3.5f,3.5f,0);
				Handles.DrawBezier(lineFrom, lineTo+shadow, lineFrom+shadow + lineFromTangent+shadow, lineTo+shadow + lineToTangent, new Color(0,0,0,0.1f), null, lineSize+10f);
				Handles.DrawBezier(lineFrom, lineTo, lineFrom + lineFromTangent, lineTo + lineToTangent, connectionColor, null, lineSize);
			} else {
				var shadow = new Vector3(1,1,0);
				Handles.DrawPolyLine(lineFrom, lineFrom + lineFromTangent * (hor == 0? 0.5f :1), lineTo + lineToTangent* (hor == 0? 0.5f :1), lineTo);
				Handles.DrawPolyLine(lineFrom+shadow, (lineFrom + lineFromTangent * (hor == 0? 0.5f:1))+shadow , (lineTo + lineToTangent* (hor == 0? 0.5f:1))+shadow, lineTo+shadow);
			}

			Handles.color = Color.white;

			//Find the mid position of the connection to draw GUI stuff there
			Vector2 midPosition;

			var t = 0.5f;
			var B1 = t*t*t;
			var B2 = 3*t*t*(1-t);
			var B3 = 3*t*(1-t)*(1-t);
			var B4 = (1-t)*(1-t)*(1-t);
			var C1 = lineFrom;
			var C2 = lineFrom - (lineFromTangent/2);
			var C3 = lineTo - (lineToTangent/2);
			var C4 = lineTo;
			var posX = C1.x * B1 + C2.x * B2 + C3.x * B3 + C4.x * B4;
			var posY = C1.y * B1 + C2.y * B2 + C3.y * B3 + C4.y * B4;

			midPosition = new Vector2(posX, posY);
			midPosition += (Vector2)(lineFromTangent + lineToTangent) /2;
			areaRect.center = midPosition;

			//////////////////////////////////////
			/////Information showing in the middle
			var alpha = (_connectionInfoExpanded || Graph.currentSelection == this || Graph.currentSelection == sourceNode)? 0.8f : 0.1f;
			var info = GetConnectionInfo(_connectionInfoExpanded, ref alpha);
			var extraInfo = sourceNode.GetConnectionInfo(this);
			if (!string.IsNullOrEmpty(info) || !string.IsNullOrEmpty(extraInfo)){
				
				if (!string.IsNullOrEmpty(extraInfo) && !string.IsNullOrEmpty(info))
					extraInfo = "\n" + extraInfo;

				var textToShow = string.Format("<size=9>{0}{1}</size>", info, extraInfo);
				if (!_connectionInfoExpanded)
					textToShow = "<size=9>-||-</size>";
				var finalSize= new GUIStyle("Box").CalcSize(new GUIContent(textToShow));

				areaRect.width = finalSize.x;
				areaRect.height = finalSize.y;

				if (e.button == 1 && e.type == EventType.MouseDown && areaRect.Contains(e.mousePosition)){
					_connectionInfoExpanded = !_connectionInfoExpanded;
					e.Use();
				}

				GUI.color = new Color(1f,1f,1f,alpha);
				GUI.Box(areaRect, textToShow);
				GUI.color = Color.white;

			} else {
				areaRect.width = 0;
				areaRect.height = 0;
			}
			////////////////////////////////////////
			////////////////////////////////////////

		}

		//The information to show in the middle area of the connection
		virtual protected string GetConnectionInfo(bool isExpanded, ref float alpha){
			return null;
		}

		
		//The connection's inspector
		public void ShowConnectionInspectorGUI(){

			GUILayout.BeginHorizontal();
			GUI.color = new Color(1,1,1,0.5f);

			if (GUILayout.Button("<", GUILayout.Height(14), GUILayout.Width(20)))
				Graph.currentSelection = sourceNode;

			if (GUILayout.Button(">", GUILayout.Height(14), GUILayout.Width(20)))
				Graph.currentSelection = targetNode;

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("X", GUILayout.Height(14), GUILayout.Width(20))){
				Graph.PostGUI += delegate { graph.RemoveConnection(this); };
				return;
			}

			GUI.color = Color.white;
			GUILayout.EndHorizontal();

			Undo.RecordObject(this, "Connection Value Change");
			isActive = EditorGUILayout.ToggleLeft("ACTIVE", isActive, GUILayout.Width(150));

			EditorUtils.BoldSeparator();
			OnConnectionInspectorGUI();

			if (GUI.changed)
				EditorUtility.SetDirty(this);
		}

		//Editor.Override to show controls in the editor panel when connection is selected
		virtual protected void OnConnectionInspectorGUI(){}

		//Simple tween to enhance the GUI line for debugging.
		private IEnumerator ChangeLineColorAndSize(){

			float effectLength = 0.2f;
			float timer = 0;

			//no tween when its going to become resting
			if (connectionStatus == Status.Resting){
				connectionColor = Node.restingColor;
				yield break;
			}

			if (connectionStatus == Status.Success)
				connectionColor = Node.successColor;

			if (connectionStatus == Status.Failure)
				connectionColor = Node.failureColor;

			if (connectionStatus == Status.Running)
				connectionColor = Node.runningColor;

			nowSwitchingColors = true;
				
			while(timer < effectLength){

				timer += Time.deltaTime;
				lineSize = Mathf.Lerp(5, defaultLineSize, timer/effectLength);
				yield return null;
			}

			if (connectionStatus == Status.Resting)
				connectionColor = Node.restingColor;

			if (connectionStatus == Status.Success)
				connectionColor = Node.successColor;

			if (connectionStatus == Status.Failure)
				connectionColor = Node.failureColor;

			if (connectionStatus == Status.Running)
				connectionColor = Node.runningColor;
			
			nowSwitchingColors = false;
		}

		#endif
	}
}
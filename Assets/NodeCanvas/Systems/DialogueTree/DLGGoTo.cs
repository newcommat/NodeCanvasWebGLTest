using UnityEngine;
using System.Collections;

namespace NodeCanvas.DialogueTrees{

	[AddComponentMenu("")]
	[Name("GO TO")]
	[Description("Jump to another Dialogue node")]
	public class DLGGoTo : DLGNodeBase {

		[SerializeField]
		private DLGNodeBase _targetNode;

		public override int maxOutConnections{
			get {return 0;}
		}

		public override string name{
			get{ return "<color=#00b9e8><GO TO></color>";}
		}

		protected override Status OnExecute(){

			if (_targetNode == null){
				DLGTree.StopGraph();
				return Error("Target node of GOTO node is null");
			}

			_targetNode.Execute();
			return Status.Success;
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnNodeGUI(){
			GUILayout.Label(string.Format("<b> {0} </b>", (_targetNode? _targetNode.name : "NONE")) );
		}

		protected override void OnNodeInspectorGUI(){
			if (GUILayout.Button("Set GO TO Target")){
				
				UnityEditor.GenericMenu.MenuFunction2 Selected = delegate(object selection){
					_targetNode = (DLGNodeBase)selection;
				};

				var menu = new UnityEditor.GenericMenu();
				foreach (DLGNodeBase node in graph.allNodes){
					if (node != this)
						menu.AddItem( new GUIContent(node.name), false, Selected, node );
				}
				menu.ShowAsContext();
				Event.current.Use();
			}

			if (_targetNode != null && GUILayout.Button("Select Target")){
				Graph.currentSelection = _targetNode;
			}
		}

		#endif
	}
}
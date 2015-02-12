using UnityEngine;
using UnityEditor;
using NodeCanvas;

namespace NodeCanvasEditor{
	//On purpose hide inspection
	[CustomEditor(typeof(Node), true)]
	public class NodeInspector : Editor {
		public override void OnInspectorGUI(){}
	}
}

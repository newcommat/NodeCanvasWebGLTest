using UnityEngine;
using System.Collections;
using NodeCanvas.Variables;

namespace NodeCanvas.BehaviourTrees{

	[Name("Switch")]
	[Category("Composites")]
	[Description("Executes ONE child based on the provided index or enum and return it's status. If 'case' change while a child is running, that child will be interrupted before the new child is executed")]
	[Icon("IndexSwitcher")]
	public class BTIndexSwitcher : BTComposite {

		public enum SelectionMode
		{
			IndexBased,
			EnumBased
		}

		public enum OutOfRangeMode
		{
			ReturnFailure,
			LoopIndex
		}

		[BlackboardOnly] [RequiredField]
		public BBEnum enumIndex;
		public BBInt index;
		public OutOfRangeMode outOfRangeMode;
		public SelectionMode selectionMode = SelectionMode.EnumBased;

		private int current;
		private int runningIndex;

		public override string name{
			get{return string.Format("<color=#b3ff7f>{0}</color>", base.name.ToUpper());}
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (outConnections.Count == 0)
				return Status.Failure;

			if (selectionMode == SelectionMode.IndexBased){

				current = index.value;
				if (outOfRangeMode == OutOfRangeMode.LoopIndex)
					current = Mathf.Abs(current) % outConnections.Count;

			} else {

				current = (int)System.Enum.Parse(enumIndex.value.GetType(), enumIndex.value.ToString());
			}

			if (runningIndex != current)
				outConnections[runningIndex].ResetConnection();

			if (current < 0 || current >= outConnections.Count)
				return Status.Failure;

			status = outConnections[current].Execute(agent, blackboard);

			if (status == Status.Running)
				runningIndex = current;

			return status;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public override string GetConnectionInfo(Connection connection){

			var i = outConnections.IndexOf(connection);
			if (selectionMode == SelectionMode.EnumBased){
				if (enumIndex.value == null)
					return "*Null Enum*";
				var enumNames = System.Enum.GetNames(enumIndex.value.GetType());
				if (i >= enumNames.Length)
					return "*Never*";
				return enumNames[i];
			}
			return i.ToString();
		}
		
		protected override void OnNodeGUI(){
			GUILayout.Label( selectionMode == SelectionMode.IndexBased? ("Current = " + index.ToString()) : enumIndex.ToString() );
		}

		protected override void OnNodeInspectorGUI(){
			selectionMode = (SelectionMode)UnityEditor.EditorGUILayout.EnumPopup("Selection Mode", selectionMode);
			if (selectionMode == SelectionMode.IndexBased)
			{
				index = (BBInt)EditorUtils.BBVariableField("Index", index);
				outOfRangeMode = (OutOfRangeMode)UnityEditor.EditorGUILayout.EnumPopup("When Out Of Range", outOfRangeMode);
			}
			else
			{
				enumIndex = (BBEnum)EditorUtils.BBVariableField("Enum", enumIndex);
				if (enumIndex.value != null){
					GUILayout.BeginVertical("box");
					foreach (string s in System.Enum.GetNames(enumIndex.value.GetType()) )
						GUILayout.Label(s);
					GUILayout.EndVertical();
				}
			}
		}
		
		#endif
	}
}
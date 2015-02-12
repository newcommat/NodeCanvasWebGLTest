using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("✫ Utility")]
	public class Wait : ActionTask {

		public BBFloat waitTime = new BBFloat{value = 1};
		public Status finishStatus = Status.Success;

		protected override string info{
			get {return "Wait " + waitTime + " sec.";}
		}

		protected override void OnUpdate(){
			if (elapsedTime >= waitTime.value)
				EndAction(finishStatus == Status.Success? true : false);
		}
	}
}
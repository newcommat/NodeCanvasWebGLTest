using UnityEngine;
using NodeCanvas.Variables;
using System.Linq;

namespace NodeCanvas.Conditions{

	[Name("Check Line Of Sight 2D")]
	[Category("GameObject")]
	[AgentType(typeof(Transform))]
	[Description("Check of agent is in line of sight with target by doing a linecast and optionaly save the distance")]
	public class CheckLOS2D : ConditionTask{

		[RequiredField]
		public BBGameObject LosTarget;
		[BlackboardOnly]
		public BBFloat saveDistanceAs;

		private RaycastHit2D[] hits;
		[GetFromAgent]
		private Collider2D agentCollider;

		protected override string info{
			get {return "LOS with " + LosTarget.ToString();}
		}

		protected override bool OnCheck(){

			hits = Physics2D.LinecastAll(agent.transform.position, LosTarget.value.transform.position);
			foreach (Collider2D collider in hits.Select(h => h.collider)){
				if (collider != agentCollider && collider != LosTarget.value.GetComponent<Collider2D>())
					return false;
			}
			return true;
		}

		protected override void OnGizmosSelected(){
			if (agent && LosTarget.value)
				Gizmos.DrawLine(agent.transform.position, LosTarget.value.transform.position);
		}
	}
}
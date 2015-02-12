using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class CurveData : VariableData<AnimationCurve>{

		public override void OnCreate(){
			value = AnimationCurve.EaseInOut(0,0,1,1);
		}
		
		public override object GetSerialized(){
			var serKeys = new List<SerializedKey>();
			foreach (Keyframe key in GetValue().keys){
				var newKey = new SerializedKey();
				newKey.time = key.time;
				newKey.value = key.value;
				newKey.inTangent = key.inTangent;
				newKey.outTangent = key.outTangent;
				serKeys.Add(newKey);
			}
			return serKeys;
		}

		public override void SetSerialized(object obj){
			var keyframes = new List<Keyframe>();
			foreach (SerializedKey serKey in (List<SerializedKey>)obj )
				keyframes.Add(new Keyframe(serKey.time, serKey.value, serKey.inTangent, serKey.outTangent));
			(objectValue as AnimationCurve).keys = keyframes.ToArray();
			SetValue( new AnimationCurve(keyframes.ToArray()) );
		}

		[System.Serializable]
		class SerializedKey{

			public float time;
			public float value;
			public float inTangent;
			public float outTangent;
		}
	}
}
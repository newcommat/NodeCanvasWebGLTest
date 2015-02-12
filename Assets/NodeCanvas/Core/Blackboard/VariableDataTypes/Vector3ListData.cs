using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	public class Vector3ListData : VariableData<List<Vector3>> {

		public override void OnCreate(){
			value = new List<Vector3>();
		}

		public override object GetSerialized(){
			var vectors = new List<float[]>();
			foreach (Vector3 vec in GetValue())
				vectors.Add( new float[] { vec.x, vec.y, vec.z } );
			return vectors;
		}

		public override void SetSerialized(object obj){
			var vectors = (List<float[]>)obj;
			var newList = new List<Vector3>();
			foreach (float[] vec in vectors)
				newList.Add( new Vector3( vec[0], vec[1], vec[2] ) );
			SetValue(newList);
		}
	}
}
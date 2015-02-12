#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NodeCanvas{

	///Holds NC preferences
	public static class NCPrefs {

		static bool loaded = false;
		static bool _showNodeInfo;
		static bool _isLocked;
		static bool _iconMode;
		static int _curveMode;
		static bool _doSnap;
		static bool _showTaskSummary;
		static bool _showBlackboard;
		static bool _showComments;
		static bool _hierarchicalMove;

		static List<System.Type> _preferedTypes;


		public static bool showNodeInfo{
			get {if (!loaded) Load(); return _showNodeInfo;}
			set {_showNodeInfo = value; Save();}
		}

		public static bool isLocked{
			get {if (!loaded) Load(); return _isLocked;}
			set {_isLocked = value; Save();}
		}

		public static bool iconMode{
			get {if (!loaded) Load(); return _iconMode;}
			set {_iconMode = value; Save();}
		}

		public static int curveMode{
			get {if (!loaded) Load(); return _curveMode;}
			set {_curveMode = value; Save();}
		}
		
		public static bool doSnap{
			get {if (!loaded) Load(); return _doSnap;}
			set {_doSnap = value; Save();}
		}

		public static bool showTaskSummary{
			get {if (!loaded) Load(); return _showTaskSummary;}
			set {_showTaskSummary = value; Save();}
		}

		public static bool showBlackboard{
			get {if (!loaded) Load(); return _showBlackboard;}
			set {_showBlackboard = value; Save();}
		}

		public static bool showComments{
			get {if (!loaded) Load(); return _showComments;}
			set {_showComments = value; Save();}			
		}

		public static bool hierarchicalMove{
			get {if (!loaded) Load(); return _hierarchicalMove;}
			set {_hierarchicalMove = value; Save();}			
		}

		//Load the prefs
		static void Load(){
			_showNodeInfo    = EditorPrefs.GetBool("NC.NodeInfo", true);
			_isLocked        = EditorPrefs.GetBool("NC.IsLocked", false);
			_iconMode        = EditorPrefs.GetBool("NC.IconMode", true);
			_curveMode       = EditorPrefs.GetInt("NC.CurveMode", 0);
			_doSnap          = EditorPrefs.GetBool("NC.DoSnap", true);
			_showTaskSummary = EditorPrefs.GetBool("NC.TaskSummary", true);
			_showBlackboard  = EditorPrefs.GetBool("NC.ShowBlackboard", true);
			_showComments    = EditorPrefs.GetBool("NC.ShowComments", true);
			_hierarchicalMove= EditorPrefs.GetBool("NC.HierarchicalMove", false);
			loaded           = true;
		}

		//Save the prefs
		static void Save(){
			EditorPrefs.SetBool("NC.NodeInfo", _showNodeInfo);
			EditorPrefs.SetBool("NC.IsLocked", _isLocked);
			EditorPrefs.SetBool("NC.IconMode", _iconMode);
			EditorPrefs.SetInt("NC.CurveMode", _curveMode);
			EditorPrefs.SetBool("NC.DoSnap", _doSnap);
			EditorPrefs.SetBool("NC.TaskSummary", _showTaskSummary);
			EditorPrefs.SetBool("NC.ShowBlackboard", _showBlackboard);
			EditorPrefs.SetBool("NC.ShowComments", _showComments);
			EditorPrefs.SetBool("NC.HierarchicalMove", _hierarchicalMove);
		}

		//The default prefered types list to be shown wherever a type is important
		private static string defaultPreferedTypesList{
			get
			{
				var typeList = new List<System.Type>{

					//Static
					typeof(Debug),
					typeof(Application),
					typeof(Mathf),
					typeof(Physics),
					typeof(Physics2D),
					typeof(Input),
					typeof(NavMesh),
					typeof(NavMeshHit),
					typeof(RaycastHit),
					typeof(RaycastHit2D),
					typeof(PlayerPrefs),
					typeof(Random),
					typeof(Time),

					//Structs
					typeof(Vector2),
					typeof(Vector3),
					typeof(Vector4),
					typeof(Quaternion),
					typeof(Bounds),
					typeof(Rect),
					typeof(Color),

					//Objects
					typeof(Object),
					typeof(GameObject),
					typeof(Component),
					typeof(Transform),
					typeof(Animation),
					typeof(Animator),
					typeof(Rigidbody),
					typeof(Rigidbody2D),
					typeof(Collider),
					typeof(Collider2D),
					typeof(NavMeshAgent),
					typeof(CharacterController),
					typeof(AudioSource),
					typeof(Camera),
					typeof(Light),
					typeof(Renderer),
					typeof(SpriteRenderer),

					//Asset Objects
					typeof(Material),
					typeof(Texture2D),
					typeof(Sprite),
					typeof(AudioClip),
					typeof(AnimationClip),

					//UGUI
					#if UNITY_4_6
					typeof(RectTransform),
					typeof(UnityEngine.UI.Image),
					typeof(UnityEngine.UI.Button),
					typeof(UnityEngine.UI.Text),
					typeof(UnityEngine.UI.Slider),
					typeof(Canvas),
					#endif

					//NodeCanvas
					typeof(Graph),
					typeof(GraphOwner),
					typeof(Blackboard),
					typeof(Status)
				};

				var joined = "";
				foreach (System.Type t in typeList)
					joined += t.AssemblyQualifiedName + "|";
				return joined;
			}
		}

		///Get the prefered types set by the user
		public static List<System.Type> GetPreferedTypesList(System.Type baseType){

			if (_preferedTypes == null){
				_preferedTypes = new List<System.Type>();
				foreach(string s in EditorPrefs.GetString("NC.CommonTypes", defaultPreferedTypesList).Split('|')){
					try { _preferedTypes.Add(System.Type.GetType(s)); }
					catch {Debug.Log(string.Format("Type '{0}' not found. It will be excluded", s));}
				}
			}

			return _preferedTypes.Where(t => baseType.IsAssignableFrom(t)).ToList();
		}

		///Set the prefered types list for the user
		public static void SetPreferedTypesList(List<System.Type> types){
			_preferedTypes = types;
			var joined = "";
			foreach (System.Type t in types)
				joined += t.AssemblyQualifiedName + "|";
			EditorPrefs.SetString("NC.CommonTypes", joined);
		}

		///Reset the prefered types to the default ones
		public static void ResetTypeConfiguration(){
			EditorPrefs.SetString("NC.CommonTypes", defaultPreferedTypesList);
			_preferedTypes = null;
		}

		//A Type to color lookup table
		private static Dictionary<System.Type, Color> typeColors = new Dictionary<System.Type, Color>()
		{
			{typeof(bool),               new Color(1,0.6f,0.6f)},
			{typeof(float),              new Color(0.6f,0.6f,1)},
			{typeof(int),                new Color(0.6f,1,0.6f)},
			{typeof(string),             new Color(0.5f,0.5f,0.5f)},
			{typeof(Vector2),            new Color(1,0.6f,1)},
			{typeof(Vector3),            new Color(1,0.6f,1)},
			{typeof(UnityEngine.Object), Color.grey}
		};

		///Get the color preference for a type
		public static Color GetTypeColor(System.Type type){
			if (!EditorGUIUtility.isProSkin)
				return Color.white;
			if (typeColors.ContainsKey(type))
				return typeColors[type];

			foreach (var pair in typeColors){
				if (pair.Key.NCIsAssignableFrom(type))
					return typeColors[type] = pair.Value;
				if (typeof(System.Collections.IList).IsAssignableFrom(type)){
					if (type.IsArray){
						return typeColors[type] = GetTypeColor( type.GetElementType() );
					} else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)){
						return typeColors[type] = GetTypeColor( type.GetGenericArguments()[0] );
					}
				}
			}
			return typeColors[type] = new Color(1,1,1,0.8f);
		}

		///Get the hex color preference for a type
		public static string GetTypeHexColor(System.Type type){
			return ColorToHex(GetTypeColor(type));
		}

		static string ColorToHex(Color32 color){
			return ("#" + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2")).ToLower();
		}
		 
		static Color HexToColor(string hex){
			byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
			byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
			byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
			return new Color32(r,g,b, 255);
		}
	}
}

#endif
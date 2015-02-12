#if UNITY_EDITOR
using UnityEditor;
#endif

#if !NETFX_CORE && !UNITY_WP8
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using NodeCanvas.Variables;

namespace NodeCanvas{

	[ExecuteInEditMode]
	[AddComponentMenu("NodeCanvas/Blackboard")]
	///Blackboard holds data and is able to save and load itself, but if so the name must be unique. It's usefull for interop
	///communication within the program
	public partial class Blackboard : MonoBehaviour, ISavable{

		[SerializeField]
		private string _blackboardName = string.Empty;
		[SerializeField]
		private bool _isGlobal;
		[SerializeField]
		private List<VariableData> variables = new List<VariableData>();
		private Dictionary<string, VariableData> _lookUpVars;

		private static Dictionary<Type, Type> _typeDataRelation;
		private static List<Blackboard> _globalBlackboards;

		//The VariableType to their contained value type relation
		private static Dictionary<Type, Type> typeDataRelation{
			get
			{
				if (_typeDataRelation == null)
					_typeDataRelation = GetTypeDataRelation();
				return _typeDataRelation;
			}
		}

		//A list of the global blackboards in the scene
		private static List<Blackboard> globalBlackboards{
			get
			{
				if (_globalBlackboards == null){
					_globalBlackboards = new List<Blackboard>();
					foreach (Blackboard bb in FindObjectsOfType<Blackboard>()){
						if (bb.isGlobal)
							_globalBlackboards.Add(bb);
					}
				}
				return _globalBlackboards;
			}
		}

		//Dictionary to be used in runtime for quick variables lookup based on their name
		private Dictionary<string, VariableData> lookUpVars{
			get
			{
				if (_lookUpVars == null){
					_lookUpVars = new Dictionary<string, VariableData>();
					foreach (var variable in variables){
						if (_lookUpVars.ContainsKey(variable.name)){
							Debug.LogError(string.Format("A variable with name '{0}' already exists on blackboard '{1}'", variable.name, this.name), this);
							continue;
						}
						_lookUpVars[variable.name] = variable;
					}
				}
				return _lookUpVars;
			}
		}

		new public string name{
			get {return _blackboardName;}
			set
			{
				if (string.IsNullOrEmpty(value))
					value = gameObject.name + "_BB";
				_blackboardName = value;
			}
		}

		public bool isGlobal{
			get {return _isGlobal;}
			set
			{
				if (_isGlobal == true && value == false)
					globalBlackboards.Remove(this);
				
				if (_isGlobal == false && value == true){
					if (!globalBlackboards.Contains(this))
						globalBlackboards.Add(this);
				}
				_isGlobal = value;
			}
		}

		//Get the Type to Type relation of VariableData type and their contained value type.
		private static Dictionary<Type, Type> GetTypeDataRelation(){
			var pairs = new Dictionary<Type, Type>();
			foreach (Type t in NCReflection.GetAssemblyTypes()){
				if (typeof(VariableData).NCIsAssignableFrom(t) && !t.NCIsAbstract() && t.NCGetAttribute<ObsoleteAttribute>(true) == null){
					var valueField = t.NCGetField("value");
					if (valueField != null)
						pairs[t] = valueField.FieldType;
				}
			}
			return pairs;
		}

		///Get all data of the blackboard
		public IEnumerable<VariableData> GetAllData(){
			return variables;
		}

		public static Dictionary<string, Blackboard> GetGlobalBlackboards(){
			try {return globalBlackboards.ToDictionary( b => b.name, b => b );}
			catch {Debug.LogError("Duplicate Global Blackboard names found"); return new Dictionary<string, Blackboard>(); }
		}

		private static bool ValidateGlobalBlackboards(){
			try {globalBlackboards.ToDictionary( b => b.name, b => b ); return true;}
			catch {return false;}
		}


		void Awake(){

			if (Application.isPlaying){
				foreach (VariableData data in variables)
					data.InitBinding(this.gameObject);
			}

			if (isGlobal && !globalBlackboards.Contains(this)){
				globalBlackboards.Add(this);
				if (!ValidateGlobalBlackboards())
					Debug.LogError("Duplicate Global Blackboard Names found!");
			}

			if (_typeDataRelation == null)
				_typeDataRelation = GetTypeDataRelation();
		}

		void OnDestroy(){

			globalBlackboards.Remove(this);

			#if UNITY_EDITOR
			UnityEditor.EditorApplication.delayCall += ()=> {
				foreach(VariableData data in variables)
					if (data) DestroyImmediate(data, true);
			};
			#endif
		}


		///Add a new VariableData in the blackboard
		public VariableData AddData(string dataName, object value){
			
			if (value == null){
				Debug.LogWarning("You can't use AddData with a null value. Use AddData(string, Type) to add the new data first");
				return null;
			}
			
			var newData = AddData(dataName, value.GetType());
			if (newData != null)
				newData.objectValue = value;

			return newData;
		}

		///Add a new VariableData in the blackboard defining name and type instead of value
		public VariableData AddData(string dataName, Type type){

			if (Application.isPlaying && GetData(dataName) != null){
				Debug.LogWarning(string.Format("Variable with name '{0}' already exists on blackboard '{1}'", dataName, this.name));
				return null;
			}

			VariableData newData = null;

			foreach (KeyValuePair<Type, Type> pair in typeDataRelation){
				
				//Exclude system and unity object types so that we find explicit defined variables first
				if (pair.Value == typeof(object) || pair.Value == typeof(UnityEngine.Object))
					continue;

				if (pair.Value.NCIsAssignableFrom(type) ){
					newData = (VariableData)gameObject.AddComponent(pair.Key);
					break;
				}
			}

			if (newData == null){
				if (typeof(UnityEngine.Object).NCIsAssignableFrom(type)){
					//First check if type is a Unity object type and use the implicit variable
					newData = gameObject.AddComponent<UnityObjectData>();
				} else {
					//Else use the implicit object type variable
					newData = gameObject.AddComponent<SystemObjectData>();
				}
			}

			newData.name = dataName;
			newData.varType = type;
			newData.hideFlags = HideFlags.HideInInspector;
			variables.Add(newData);
			lookUpVars[dataName] = newData;
			newData.OnCreate();
			return newData;
		}

		///Deletes the VariableData of name provided regardless of type
		public void DeleteData(string dataName){
			var data = GetData(dataName);
			if (data != null){
				variables.Remove(data);
				lookUpVars.Remove(data.name);
				DestroyImmediate(data,true);
			}
		}

		///Generic version 
		public bool HasData<T>(string dataName){
			return HasData(dataName, typeof(T));
		}

		///Does the blackboard has the data of type and name?
		public bool HasData(string dataName, Type type){
			return GetData(dataName, type) != null;
		}

		///Generic way of getting data. Reccomended
		public T GetDataValue<T>(string dataName){
			try {return (T)GetData(dataName, typeof(T)).objectValue;}
			catch {return default(T);}
		}

		///Non generic method of geting a variable value
		public object GetDataValue(string dataName, Type type){
			var data = GetData(dataName, type);
			return data != null? data.objectValue : null;
		}

		///Set the value of the VariableData variable defined by its name. If a data by that name and type doesnt exist, a new data is added by that name
		public VariableData SetDataValue(string dataName, object value){

			var data = GetData(dataName, (value != null? value.GetType() : null), true);

			if (data != null){

				data.objectValue = value;
			
			} else if (value != null){

				Debug.Log("No VariableData of name '" + dataName + "' and type '" + value.GetType().Name + "' exists. Adding new instead...");
				return AddData(dataName, value);
			}

			return data;			
		}


		public VariableData<T> GetData<T>(string dataName, bool castableTo = false){
			return GetData(dataName, typeof(T), castableTo) as VariableData<T>; 
		}

		///Get the VariableData object of a certain name and optional specified type
		public VariableData GetData(string dataName, Type ofType = null, bool castableTo = false){

			if (string.IsNullOrEmpty(dataName))
				return null;
			
			VariableData data = null;

			#if UNITY_EDITOR
			if (!Application.isPlaying){
				for (int i = 0; i < variables.Count; i++){
					data = variables[i];
					if (data.name == dataName){
						if ( ofType == null || ofType.NCIsAssignableFrom(data.varType) )
							return data;
						if (castableTo && data.varType.NCIsAssignableFrom(ofType))
							return data;
					}
				}

				return null;
			}
			#endif

			if (!lookUpVars.ContainsKey(dataName))
				return null;
			data = lookUpVars[dataName];
			if (ofType == null || ofType.NCIsAssignableFrom(data.varType))
				return data;
			if (castableTo && data.varType.NCIsAssignableFrom(ofType))
				return data;
			return null;
		}

		///Get all data names of the blackboard
		public string[] GetDataNames(){
			return variables.Select(v => v.name).ToArray();
		}

		///Get all data names of the blackboard and of specified type
		public string[] GetDataNames(System.Type ofType){
			return variables.Where(v => (ofType).NCIsAssignableFrom(v.varType) ).Select(v => v.name).ToArray();
		}

		public static Blackboard FindGlobal(string bbName){
			if (GetGlobalBlackboards().ContainsKey(bbName))
				return GetGlobalBlackboards()[bbName];
			return null;
		}

		///Gets a Blackboard by its name
		public static Blackboard Find(string bbName){

			var bbs = FindObjectsOfType<Blackboard>();
			foreach(var bb in bbs){
				if (bb.name == bbName)
					return bb;
			}
			return null;
		}


		////////////////////
		//SAVING & LOADING//
		////////////////////

		//** Saving/Loading is irelevant to how the variables are serialized IN the blackboard.
		//It is rather optionaly used to save and load a complete variable data set between scenes or even gaming sessions like a save/load system would.

		public string saveKey{
			get {return "Blackboard-" + name;}
		}

		//Save/Load is not supported in those platforms
		#if !NETFX_CORE && !UNITY_WP8

		///Serrialize the blackboard's data as an 64String in PlayerPrefs. The name of the blackboard is important
		///The final string format that the blackboard is saved as, is returned.
		public string Save(){

			var formatter = new BinaryFormatter();
			var stream = new MemoryStream();
			var dataList = new List<SerializedData>();
			
			foreach (VariableData data in variables){
				var serValue = data.GetSerialized();
				if (serValue == null || (serValue != null && serValue.GetType().IsSerializable) ) {
					dataList.Add(new SerializedData(data.name, data.GetType(), serValue, data.propertyPath ) );
				} else {
					dataList.Add(new SerializedData(data.name, data.GetType(), null, data.propertyPath ) );
					Debug.LogWarning("Blackboard '" + data.varType + "' data doesn't support save/load. '" + data.name + "'");
				}
			}

			formatter.Serialize(stream, dataList);
			PlayerPrefs.SetString(saveKey, Convert.ToBase64String(stream.GetBuffer()));

			Debug.Log("Saved: " + saveKey, gameObject);
			return saveKey;
		}

		///Deserialize and load back all data. The name of the blackboard is used as a string format. Returns false if no saves were found.
		public bool Load(){

			var dataString = PlayerPrefs.GetString(saveKey);

			if (string.IsNullOrEmpty(dataString)){
				Debug.Log("No Save found for: " + saveKey);
				return false;
			}

			foreach (VariableData data in variables)
				DestroyImmediate(data, true);

			variables.Clear();

			var formatter = new BinaryFormatter();
			var stream = new MemoryStream(Convert.FromBase64String(dataString));		
			var loadedData = formatter.Deserialize(stream) as List<SerializedData>;

			foreach (SerializedData serializedData in loadedData){
				var newData = (VariableData)gameObject.AddComponent(serializedData.type);
				newData.hideFlags = HideFlags.HideInInspector;
				newData.name = serializedData.name;
				newData.propertyPath = serializedData.propertyPath;
				newData.InitBinding(this.gameObject);
				newData.SetSerialized(serializedData.value);
				variables.Add(newData);
			}				

			Debug.Log("Loaded: " + saveKey, gameObject);
			return true;
		}

		//The class that is actually serialized and deserialized by Save and Load
		[Serializable]
		private class SerializedData{

			public string name;
			public Type type;
			public object value;
			public string propertyPath;

			public SerializedData(string name, Type type, object value, string propertyPath){
				this.name = name;
				this.type = type;
				this.value = value;
				this.propertyPath = propertyPath;
			}
		}
	
		#else

		//implement ISavable members for the shake of not getting build errors
		public string Save(){
			Debug.LogError("Saving blackboards in NETFX_CORE is not supported", gameObject);
			return null;
		}

		public bool Load(){
			Debug.LogError("Loading blackboards in NETFX_CORE is not supported", gameObject);
			return false;
		}

		#endif



		//////////////////////////////////
		///////GUI & EDITOR STUFF/////////
		//////////////////////////////////
		#if UNITY_EDITOR

		[ContextMenu("Reset")]
		void Reset(){
			
			Undo.RecordObject(this, "Reset");
			name = null;
			isGlobal = false;
			foreach (VariableData data in variables)
				Undo.DestroyObjectImmediate(data);
			variables.Clear();
		}
		[ContextMenu("Copy Component")]
		void CopyComponent(){Debug.Log("Unsupported");}
		[ContextMenu("Paste Component Values")]
		void PasteComponentValues(){Debug.Log("Unsupported");}

		[ContextMenu("Copy Blackboard Variables")]
		void CopyVariables(){
			var original = name;
			name = "Copied";
			Save();
			name = original;
		}
		

		[ContextMenu("Paste Blackboard Variables")]
		void PasteVariables(){
			var original = name;
			name = "Copied";
			Load();
			name = original;
		}

		void OnValidate(){
			
			for (int i = 0; i < variables.Count; i++){
				if ( (variables[i] as UnityEngine.Object) == null)
					variables.RemoveAt(i);
			}
		}

		[UnityEditor.MenuItem("Window/NodeCanvas/Create a Global Blackboard")]
		public static void CreateGlobalBlackboard(){
			var bb = new GameObject("@GlobalBlackboard").AddComponent<Blackboard>();
			bb.name = "Global";
			bb.isGlobal = true;
			UnityEditor.Selection.activeObject = bb;
		}	

		public void ShowBlackboardGUI(){

			Undo.RecordObject(this, "Blackboard Inspector");

			if (!ValidateGlobalBlackboards())
				GUI.color = Color.red;

			GUILayout.BeginHorizontal();
			name = EditorGUILayout.TextField("Blackboard Name", name, new GUIStyle("textfield"), GUILayout.ExpandWidth(true));
			GUILayout.Label("Global", GUILayout.Width(40));
			isGlobal = EditorGUILayout.Toggle(isGlobal, EditorStyles.radioButton, GUILayout.Width(20));
			GUILayout.EndHorizontal();
			GUI.color = Color.white;

			ShowVariablesGUI();

			if (GUI.changed)
		        EditorUtility.SetDirty(this);
		}

		public void ShowVariablesGUI(){

			var layoutOptions = new GUILayoutOption[]{GUILayout.MaxWidth(100), GUILayout.ExpandWidth(true)};

			GUI.backgroundColor = new Color(0.8f,0.8f,1);
			if (GUILayout.Button("Add Variable")){

				GenericMenu.MenuFunction2 Selected = delegate(object selectedType){
					Undo.RecordObject(this, "New Variable");
					var newData = AddData("my" + EditorUtils.TypeName((Type)selectedType), (Type)selectedType);
					Undo.RegisterCreatedObjectUndo(newData, "New Variable");
				};

				var assetPaths = AssetDatabase.GetAllAssetPaths().Select(p => EditorUtils.Strip(p, "/")).ToList();
				var menu = new GenericMenu();
				foreach (KeyValuePair<Type, Type> pair in typeDataRelation){

					if (pair.Key.IsSubclassOf(typeof(MonoBehaviour))){
						if (!assetPaths.Contains(pair.Key.Name +".cs") && !assetPaths.Contains(pair.Key.Name+".js") && !assetPaths.Contains(pair.Key.Name+".boo")){
							Debug.LogWarning(string.Format("Class Name {0} is different from it's script name", pair.Key.Name));
							continue;
						}
					}

					var category = !string.IsNullOrEmpty(pair.Value.Namespace)? pair.Value.Namespace + "/": "";
					var name = EditorUtils.TypeName(pair.Value);
					menu.AddItem(new GUIContent(category + name), false, Selected, pair.Value) ;
				}

				menu.ShowAsContext();
				Event.current.Use();
			}

			GUI.backgroundColor = Color.white;

			//simple column header info
			if (variables.Count != 0){
				GUILayout.BeginHorizontal();
				GUI.color = Color.yellow;
				GUILayout.Label("Name", layoutOptions);
				GUILayout.Label("Value", layoutOptions);
				GUI.color = Color.white;
				GUILayout.EndHorizontal();
			} else {
				EditorGUILayout.HelpBox("Blackboard has no variables", MessageType.Info);
			}


			//The actual variables reorderable list
			EditorUtils.ReorderableList(variables, delegate(int i){

				var data = variables[i];
				if (data == null){
					GUILayout.Label("NULL!!");
					return;
				}

				GUILayout.BeginHorizontal();

				if (!Application.isPlaying){

					//The small box on the left to re-order variables
					GUI.backgroundColor = new Color(1,1,1,0.8f);
					GUILayout.Box("", GUILayout.Width(6));
					GUI.backgroundColor = new Color(0.7f,0.7f,0.7f, 0.3f);
					
					//Make name field red if same name exists
					if (variables.Where(v => v != data).Select(v => v.name).Contains(data.name))
						GUI.backgroundColor = Color.red;
					data.name = EditorGUILayout.TextField(data.name, layoutOptions);
					GUI.backgroundColor = Color.white;

				} else {

					//Don't allow name edits in play mode. Instead show just a label
					GUI.backgroundColor = new Color(0.7f,0.7f,0.7f);
					GUI.color = new Color(0.8f,0.8f,1f);
					GUILayout.Label(data.name, layoutOptions);
				}
				
				//reset coloring
				GUI.color = Color.white;
				GUI.backgroundColor = Color.white;


				//Show the respective data GUI
				Undo.RecordObject(data, "Data Change");
				data.ShowDataGUI(layoutOptions);

				//Set dirty
				if (GUI.changed)
			        EditorUtility.SetDirty(data);

				//reset coloring
				GUI.color = Color.white;
				GUI.backgroundColor = Color.white;
				
				//'X' to delete data
				if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16))){
					if (EditorUtility.DisplayDialog("Delete Data '" + data.name + "'", "Are you sure?", "Yes", "No!")){
						Undo.DestroyObjectImmediate(data);
						Undo.RecordObject(this, "Delete Data");
						variables.Remove(data);
					}
				}

				//GUI.backgroundColor = new Color(0.7f,0.7f,0.7f);
				GUILayout.EndHorizontal();
			});

			//reset coloring
			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;

			if (GUI.changed)
		        EditorUtility.SetDirty(this);
		}

		#endif
	}
}
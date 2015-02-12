using UnityEngine;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;


namespace NodeCanvas{

	///Serialized System.Type
	[Serializable]
	public class NCTypeInfo{
		[SerializeField]
		private string _typeName;
		private Type _type;
		public NCTypeInfo(Type type){
			if (type != null)
				_typeName = type.AssemblyQualifiedName;
			_type = type;
		}
		public Type Get(){
			if (_type == null){
				if (!string.IsNullOrEmpty(_typeName))
					_type = Type.GetType(_typeName);
			}
			return _type;
		}
	}

	///Serialized MethodInfo
	[Serializable]
	public class NCMethodInfo{
		[SerializeField]
		private string _name;
		[SerializeField]
		private NCTypeInfo _declaringType = null;
		[SerializeField]
		private NCTypeInfo[] _paramTypes;
		private MethodInfo _method;

		public NCMethodInfo(MethodInfo method){
			_name = method.Name;
			_declaringType = new NCTypeInfo(method.DeclaringType);
			_paramTypes = method.GetParameters().Select(p => new NCTypeInfo(p.ParameterType) ).ToArray();
			_method = method;
		}

		public MethodInfo Get(){
			if (_method == null){
				var t = _declaringType != null? _declaringType.Get() : null;
				if (t != null)
					_method = _declaringType.Get().NCGetMethod(_name, _paramTypes.Select(p => p.Get()).ToArray());
			}
			return _method;
		}
	}


	///Helper reflection extension methods to work with NETFX_CORE
	public static partial class NCReflection {

		private static IEnumerable GetBaseTypes(Type type){
			
			yield return type;
			Type baseType;

			#if NETFX_CORE
			baseType = type.GetTypeInfo().BaseType;
			#else
			baseType = type.BaseType;
			#endif

			if (baseType != null){
				foreach (var t in GetBaseTypes(baseType)){
					yield return t;
				}
			}
		}

		public static bool NCIsAssignableFrom(this Type type, Type second){
			#if NETFX_CORE
			return type.GetTypeInfo().IsAssignableFrom(second.GetTypeInfo());
			#else
			return type.IsAssignableFrom(second);
			#endif
		}

		public static bool NCIsAbstract(this Type type){
			#if NETFX_CORE
			return type.GetTypeInfo().IsAbstract;
			#else
			return type.IsAbstract;
			#endif			
		}

		public static bool NCIsValueType(this Type type){
			#if NETFX_CORE
			return type.GetTypeInfo().IsValueType;
			#else
			return type.IsValueType;
			#endif						
		}

		public static bool NCIsInterface(this Type type){
			#if NETFX_CORE
			return type.GetTypeInfo().IsInterface;
			#else
			return type.IsInterface;
			#endif			
		}

		public static bool NCIsSubclassOf(this Type type, Type other){
			#if NETFX_CORE
			return type.GetTypeInfo().IsSubclassOf(other);
			#else
			return type.IsSubclassOf(other);
			#endif						
		}

		public static FieldInfo NCGetField(this Type type, string name){
			#if NETFX_CORE
			return GetBaseTypes(type).OfType<Type>().Select(baseType => baseType.GetTypeInfo().GetDeclaredField(name)).FirstOrDefault(field => field != null);
			#else
			return type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
			#endif
		}

		public static PropertyInfo NCGetProperty(this Type type, string name){
			#if NETFX_CORE
			return GetBaseTypes(type).OfType<Type>().Select(baseType => baseType.GetTypeInfo().GetDeclaredProperty(name)).FirstOrDefault(property => property != null);
			#else
			return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			#endif
		}

		public static MethodInfo NCGetMethod(this Type type, string name, bool includePrivate = false){

			#if NETFX_CORE
			var methods = GetBaseTypes(type).OfType<Type>().Select(baseType => baseType.GetTypeInfo().DeclaredMethods).ToList();
			foreach (MethodInfo m in methods){
				if (m.Name == name){
					if (m.IsPrivate && includePrivate)
						return m;
					if (m.IsPublic)
						return m;
				}
			}
			return null;

			#else
			if (includePrivate)
				return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
			#endif
		}

		public static MethodInfo NCGetMethod(this Type type, string name, Type[] paramTypes){
			#if NETFX_CORE
			return type.NCGetMethod(name);
			#else
			return type.GetMethod(name, paramTypes);
			#endif
		}

		public static EventInfo NCGetEvent(this Type type, string name){
			#if NETFX_CORE
			return GetBaseTypes(type).OfType<Type>().Select(baseType => baseType.GetTypeInfo().GetDeclaredEvent(name)).FirstOrDefault(method => method != null);
			#else
			return type.GetEvent(name, BindingFlags.Instance | BindingFlags.Public);
			#endif			
		}

		public static FieldInfo[] NCGetFields(this Type type){

			#if NETFX_CORE
			var fields = new List<FieldInfo>();
			foreach (Type t in GetBaseTypes(type).OfType<Type>())
				fields.AddRange(t.GetTypeInfo().DeclaredFields);
			return fields.ToArray();
			#else
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			#endif
		}

		public static MethodInfo[] NCGetMethods(this Type type){

			#if NETFX_CORE
			var methods = new List<MethodInfo>();
			foreach (Type t in GetBaseTypes(type).OfType<Type>())
				methods.AddRange(t.GetTypeInfo().DeclaredMethods);
			return methods.ToArray();
			#else
			return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			#endif
		}

		public static PropertyInfo[] NCGetProperties(this Type type){
			#if NETFX_CORE
			var props = new List<PropertyInfo>();
			foreach (Type t in GetBaseTypes(type).OfType<Type>())
				props.AddRange(t.GetTypeInfo().DeclaredProperties);
			return props.ToArray();
			#else
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			#endif
		}

		//
		public static T NCGetAttribute<T>(this Type type, bool inherited) where T : Attribute {
			#if NETFX_CORE
			return (T)type.GetTypeInfo().GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
			#else
			return (T)type.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
			#endif			
		}

		public static T NCGetAttribute<T>(this MemberInfo member, bool inherited) where T : Attribute{
			#if NETFX_CORE
			return (T)member.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
			#else
			return (T)member.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
			#endif			
		}
		//

        public static Type[] NCGetGenericArguments(this Type type){
			#if NETFX_CORE
            return type.GetTypeInfo().GenericTypeArguments;
			#else
            return type.GetGenericArguments();
			#endif
        }

        public static Type[] NCGetEmptyTypes(){
			#if NETFX_CORE
			return new Type[0];
			#else
            return Type.EmptyTypes;
			#endif
        }
        
        public static T NCCreateDelegate<T>(this MethodInfo method, object instance){
			return (T)(object)method.NCCreateDelegate(typeof(T), instance);
        }

        public static Delegate NCCreateDelegate(this MethodInfo method, Type type, object instance){
			#if NETFX_CORE
			return method.CreateDelegate(type, instance);
			#else
            return Delegate.CreateDelegate(type, instance, method);
			#endif
        }

		private static List<Assembly> loadedAssemblies;
		public static List<Type> GetAssemblyTypes() {
			#if NETFX_CORE
		    if (loadedAssemblies != null)
		        return loadedAssemblies.Select(t => t.GetType()).ToList();

		    var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

		    loadedAssemblies = new List<Assembly>();
		    var folderFilesAsync = folder.GetFilesAsync();
		    folderFilesAsync.AsTask().Wait();

		    foreach (var file in folderFilesAsync.GetResults()){
		        if (file.FileType == ".dll" || file.FileType == ".exe"){
		            try
		            {
		                var filename = file.Name.Substring(0, file.Name.Length - file.FileType.Length);
		                AssemblyName name = new AssemblyName { Name = filename };
		                Assembly asm = Assembly.Load(name);
		                loadedAssemblies.Add(asm);
		            }
		            catch (BadImageFormatException)
		            {
		                // Thrown reflecting on C++ executable files for which the C++ compiler stripped the relocation addresses
		            }
		        }
		    }
		    return loadedAssemblies.Select(t => t.GetType()).ToList();
			#else

			var types = new List<Type>();
		    foreach(Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
		    	try
		    	{
		    		types.AddRange(ass.GetTypes());
		    	}
		    	catch
		    	{
		    		Debug.Log(ass.FullName + " will be excluded");
		    		continue;
		    	}
		    return types;
		    #endif
		}

	    
	    ///Creates a delegate of T for a MethodInfo with casted method parameters and return type to the specified delegate T types
	    public static T BuildDelegate<T>(MethodInfo method, params object[] missingParamValues) {
	        
	        var queueMissingParams = new Queue<object>(missingParamValues);
	        var dgtMi = typeof(T).NCGetMethod("Invoke");
	        var dgtRet = dgtMi.ReturnType;
	        var dgtParams = dgtMi.GetParameters();

	        var paramsOfDelegate = (dgtParams as IEnumerable<ParameterInfo>).Select(tp => Expression.Parameter(tp.ParameterType, tp.Name)).ToArray();

	        var methodParams = method.GetParameters();

	        if (method.IsStatic)
	        {
	            var paramsToPass = (methodParams as IEnumerable<ParameterInfo>).Select((p, i) => CreateParam(paramsOfDelegate, i, p, queueMissingParams)).ToArray();

	            var call = Expression.Call(method, paramsToPass);
	            var convertCall = Expression.Convert(call, typeof(object));
	            Expression<T> expr = null;
	            if (dgtRet == typeof(void)){
	            	expr = Expression.Lambda<T>(call, paramsOfDelegate);
            	} else {
            		expr = Expression.Lambda<T>(convertCall, paramsOfDelegate);
            	}

	            return expr.Compile();
	        }
	        else
	        {
	            var paramThis = Expression.Convert(paramsOfDelegate[0], method.DeclaringType);
	            var paramsToPass = methodParams.Select((p, i) => CreateParam(paramsOfDelegate, i + 1, p, queueMissingParams)).ToArray();

	            var call = Expression.Call(paramThis, method, paramsToPass);
	            var convertCall = Expression.Convert(call, typeof(object));
	            Expression<T> expr = null;
	            if (dgtRet == typeof(void)){
		            expr = Expression.Lambda<T>(call, paramsOfDelegate);
            	} else {
            		expr = Expression.Lambda<T>(convertCall, paramsOfDelegate);
            	}

	            return expr.Compile();
	        }
	    }

	    private static Expression CreateParam(ParameterExpression[] paramsOfDelegate, int i, ParameterInfo callParamType, Queue<object> queueMissingParams) {
	        if (i < paramsOfDelegate.Length)
	            return Expression.Convert(paramsOfDelegate[i], callParamType.ParameterType);

	        if (queueMissingParams.Count > 0)
	            return Expression.Constant(queueMissingParams.Dequeue());

	        if (callParamType.ParameterType.NCIsValueType() )
	            return Expression.Constant(Activator.CreateInstance(callParamType.ParameterType));

	        return Expression.Constant(null);
	    }
	}
}
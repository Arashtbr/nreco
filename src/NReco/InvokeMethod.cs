#region License
/*
 * NReco library (http://nreco.googlecode.com/)
 * Copyright 2008,2009 Vitaliy Fedorchenko
 * Distributed under the LGPL licence
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Reflection;

using NReco.Converting;
using NReco.Logging;

namespace NReco {
	
	/// <summary>
	/// Invoke object's method that is most compatible with provided arguments
	/// </summary>
	public class InvokeMethod {
		static ILog log = LogManager.GetLogger(typeof(InvokeMethod));

		public object TargetObject { get; set; }

		public string MethodName { get; set; }

		public InvokeMethod(object o, string methodName) {
			TargetObject = o;
			MethodName = methodName;
		}

		protected MethodInfo FindMethod(Type[] argTypes) {
			if (TargetObject is Type) {
				// static method
				return ((Type)TargetObject).GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public, null, argTypes, null);
			}
			return TargetObject.GetType().GetMethod(MethodName, argTypes);
		}

		protected MethodInfo[] GetAllMethods() {
			if (TargetObject is Type) {
				return ((Type)TargetObject).GetMethods(BindingFlags.Static | BindingFlags.Public);
			}
			return TargetObject.GetType().GetMethods();
		}

		public object Invoke(object[] args) {
			Type[] argTypes = new Type[args.Length];
			for (int i = 0; i < argTypes.Length; i++)
				argTypes[i] = args[i] != null ? args[i].GetType() : typeof(object);

			// strict matching first
			MethodInfo targetMethodInfo = FindMethod(argTypes);
			// fuzzy matching
			if (targetMethodInfo==null) {
				MethodInfo[] methods = GetAllMethods();

				if (log.IsEnabledFor(LogEvent.Debug))
					log.Write(LogEvent.Debug, "Invoke '{0}': Found {1} methods for matching", MethodName, methods.Length);
				for (int i=0; i<methods.Length; i++)
					if (methods[i].Name==MethodName &&
						methods[i].GetParameters().Length == args.Length &&
						CheckParamsCompatibility(methods[i].GetParameters(), argTypes, args)) {
						targetMethodInfo = methods[i];
						break;
					}
			}
			if (targetMethodInfo == null) {
				string[] argTypeNames = new string[argTypes.Length];
				for (int i=0; i<argTypeNames.Length; i++)
					argTypeNames[i] = argTypes[i].Name;
				string argTypeNamesStr = String.Join(",",argTypeNames);
				throw new MissingMethodException( 
						(TargetObject is Type ? (Type)TargetObject : TargetObject.GetType()).FullName, MethodName );
			}
			object[] argValues = PrepareActualValues(targetMethodInfo.GetParameters(),args);
			object res = null;
			try {
				res = targetMethodInfo.Invoke( TargetObject is Type ? null : TargetObject, argValues);
			} catch (TargetInvocationException tiEx) {
				if (tiEx.InnerException!=null)
					throw new Exception(tiEx.InnerException.Message, tiEx.InnerException);
				else {
					throw;
				}
			}
			return res;
		}

		protected bool CheckParamsCompatibility(ParameterInfo[] paramsInfo, Type[] types, object[] values) {
			for (int i=0; i<paramsInfo.Length; i++) {
				Type paramType = paramsInfo[i].ParameterType;
				if (paramType.IsInstanceOfType(values[i]))
					continue;
				// null and reference types
				if (values[i]==null && !paramType.IsValueType)
					continue;
				// possible autocast between generic/non-generic common types
				if (ConvertManager.CanChangeType(types[i],paramType))
					continue;
				// incompatible parameter
				return false;
			}
			return true;
		}

		protected object[] PrepareActualValues(ParameterInfo[] paramsInfo, object[] values) {
			object[] res = new object[paramsInfo.Length];
			for (int i=0; i<paramsInfo.Length; i++) {
				if (values[i]==null || paramsInfo[i].ParameterType.IsInstanceOfType(values[i])) {
					res[i] = values[i];
					continue;
				}
				var conv = ConvertManager.FindConverter(values[i].GetType(), paramsInfo[i].ParameterType);
				if (conv!=null) {
					res[i] = conv.Convert( values[i], paramsInfo[i].ParameterType );
					continue;
				}
				throw new InvalidCastException( 
					String.Format("Invoke method '{0}': cannot convert argument #{1} from {2} to {3}",
						MethodName, i, values[i].GetType(), paramsInfo[i].ParameterType));
			}
			return res;
		}


	}

}

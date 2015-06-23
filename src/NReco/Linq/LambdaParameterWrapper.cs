﻿#region License
/*
 * NReco library (http://nreco.googlecode.com/)
 * Copyright 2014 Vitaliy Fedorchenko
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
using System.Linq;
using System.Text;
using System.Reflection;
using NReco.Converting;

namespace NReco.Linq {

	internal sealed class LambdaParameterWrapper : IComparable {
		object _Value;

		public object Value {
			get { return _Value; }
		}

		public LambdaParameterWrapper(object val) {
			if (val is LambdaParameterWrapper)
				_Value = ((LambdaParameterWrapper)val).Value; // prevent nested wrappers
			else if (val is object[]) {
				var objArr = (object[])val;
				for (int i=0; i<objArr.Length; i++)
					if (objArr[i] is LambdaParameterWrapper)
						objArr[i] = ((LambdaParameterWrapper)objArr[i]).Value;
				_Value = val;
			} else {
				_Value = val;
			}
		}

		public int CompareTo(object obj) {
			var objResolved = obj is LambdaParameterWrapper ? ((LambdaParameterWrapper)obj).Value : obj;
			return ValueComparer.Instance.Compare(Value, objResolved);
		}

		public bool IsTrue {
			get {
				if (_Value==null)
					return false;
				return _Value is bool ? (bool)_Value : ConvertManager.ChangeType<bool>(_Value);
			}
		}

		public static LambdaParameterWrapper CreateDictionary(object[] keys, object[] values) {
			if (keys.Length!=values.Length)
				throw new ArgumentException();
			var d = new Dictionary<object,object>();
			for (int i = 0; i < keys.Length; i++) { 
				var k = keys[i];
				var v = values[i];
				// unwrap
				if (k is LambdaParameterWrapper)
					k = ((LambdaParameterWrapper)k).Value;
				if (v is LambdaParameterWrapper)
					v = ((LambdaParameterWrapper)v).Value;
				d[k] = v;
			}
			return new LambdaParameterWrapper(d);
		}

		public static LambdaParameterWrapper InvokeMethod(object obj, string methodName, object[] args) {
			if (obj is LambdaParameterWrapper)
				obj = ((LambdaParameterWrapper)obj).Value;

			if (obj == null)
				throw new NullReferenceException(String.Format("Method {0} target is null", methodName));

			var argsResolved = new object[args.Length];
			for (int i = 0; i < args.Length; i++)
				argsResolved[i] = args[i] is LambdaParameterWrapper ? ((LambdaParameterWrapper)args[i]).Value : args[i];

			var invoke = new InvokeMethod(obj, methodName);
			var res = invoke.Invoke(argsResolved);
			return new LambdaParameterWrapper(res);
		}

		public static LambdaParameterWrapper InvokeDelegate(object obj, object[] args) {
			if (obj is LambdaParameterWrapper)
				obj = ((LambdaParameterWrapper)obj).Value;
			if (obj == null)
				throw new NullReferenceException("Delegate is null");
			if (!(obj is Delegate))
				throw new NullReferenceException(String.Format("{0} is not a delegate", obj.GetType()));
			var deleg = (Delegate)obj;

			var delegParams = deleg.Method.GetParameters();
			if (delegParams.Length != args.Length)
				throw new TargetParameterCountException(
					String.Format("Target delegate expects {0} parameters", delegParams.Length));

			var resolvedArgs = new object[args.Length];
			for (int i = 0; i < resolvedArgs.Length; i++) {
				resolvedArgs[i] = ConvertManager.ChangeType(
					args[i] is LambdaParameterWrapper ? ((LambdaParameterWrapper)args[i]).Value : args[i],
					delegParams[i].ParameterType);
			}
			return new LambdaParameterWrapper( deleg.DynamicInvoke(resolvedArgs) );
		}

		public static LambdaParameterWrapper InvokePropertyOrField(object obj, string propertyName) {
			if (obj == null)
				throw new NullReferenceException(String.Format("Property or field {0} target is null", propertyName));
			if (obj is LambdaParameterWrapper)
				obj = ((LambdaParameterWrapper)obj).Value;

			var prop = obj.GetType().GetProperty(propertyName);
			if (prop != null) {
				var propVal = prop.GetValue(obj, null);
				return new LambdaParameterWrapper(propVal);
			}
			var fld = obj.GetType().GetField(propertyName);
			if (fld != null) {
				var fldVal = fld.GetValue(obj);
				return new LambdaParameterWrapper(fldVal);
			}
			throw new MissingMemberException(obj.GetType().ToString(), propertyName);
		}

		public static LambdaParameterWrapper InvokeIndexer(object obj, object[] args) {
			if (obj == null)
				throw new NullReferenceException(String.Format("Indexer target is null"));
			if (obj is LambdaParameterWrapper)
				obj = ((LambdaParameterWrapper)obj).Value;

			var argsResolved = new object[args.Length];
			for (int i = 0; i < args.Length; i++)
				argsResolved[i] = args[i] is LambdaParameterWrapper ? ((LambdaParameterWrapper)args[i]).Value : args[i];

			if (obj is Array) {
				var objArr = (Array)obj;
				if (objArr.Rank != args.Length) {
					throw new RankException(String.Format("Array rank ({0}) doesn't match number of indicies ({1})",
						objArr.Rank, args.Length));
				}
				var indicies = new long[argsResolved.Length];
				for (int i = 0; i < argsResolved.Length; i++)
					indicies[i] = ConvertManager.ChangeType<long>(argsResolved[i]);

				var res = objArr.GetValue(indicies);
				return new LambdaParameterWrapper(res);
			} else {
				// indexer method
				var invoke = new InvokeMethod(obj, "get_Item");
				var res = invoke.Invoke(argsResolved);
				return new LambdaParameterWrapper(res);
			}
		}

		public static LambdaParameterWrapper operator +(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			if (c1.Value is string || c2.Value is string) {
				return new LambdaParameterWrapper( Convert.ToString(c1.Value) + Convert.ToString(c2.Value));
			} else {
				var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
				var c2decimal = ConvertManager.ChangeType<decimal>(c2.Value);
				return new LambdaParameterWrapper(c1decimal + c2decimal);
			}
		}

		public static LambdaParameterWrapper operator -(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
			var c2decimal = ConvertManager.ChangeType<decimal>(c2.Value);
			return new LambdaParameterWrapper(c1decimal - c2decimal);
		}

		public static LambdaParameterWrapper operator -(LambdaParameterWrapper c1) {
			var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
			return new LambdaParameterWrapper(-c1decimal);
		}

		public static LambdaParameterWrapper operator *(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
			var c2decimal = ConvertManager.ChangeType<decimal>(c2.Value);
			return new LambdaParameterWrapper(c1decimal * c2decimal);
		}

		public static LambdaParameterWrapper operator /(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
			var c2decimal = ConvertManager.ChangeType<decimal>(c2.Value);
			return new LambdaParameterWrapper(c1decimal / c2decimal);
		}

		public static LambdaParameterWrapper operator %(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var c1decimal = ConvertManager.ChangeType<decimal>(c1.Value);
			var c2decimal = ConvertManager.ChangeType<decimal>(c2.Value);
			return new LambdaParameterWrapper(c1decimal % c2decimal);
		}

		public static bool operator ==(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes == 0;
		}
		public static bool operator ==(LambdaParameterWrapper c1, bool c2) {
			var c1bool = ConvertManager.ChangeType<bool>(c1.Value);
			return c1bool == c2;
		}
		public static bool operator ==(bool c1, LambdaParameterWrapper c2) {
			var c2bool = ConvertManager.ChangeType<bool>(c2.Value);
			return c1==c2bool;
		}

		public static bool operator !=(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes != 0;
		}
		public static bool operator !=(LambdaParameterWrapper c1, bool c2) {
			var c1bool = ConvertManager.ChangeType<bool>(c1.Value);
			return c1bool != c2;
		}
		public static bool operator !=(bool c1, LambdaParameterWrapper c2) {
			var c2bool = ConvertManager.ChangeType<bool>(c2.Value);
			return c1 != c2bool;
		}

		public static bool operator >(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes>0;
		}
		public static bool operator <(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes < 0;
		}

		public static bool operator >=(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes >= 0;
		}
		public static bool operator <=(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			var compareRes = ValueComparer.Instance.Compare(c1.Value, c2.Value);
			return compareRes <= 0;
		}

		public static LambdaParameterWrapper operator &(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			return  new LambdaParameterWrapper( c1.IsTrue && c2.IsTrue );
		}
		public static LambdaParameterWrapper operator &(LambdaParameterWrapper c1, bool c2) {
			return new LambdaParameterWrapper( c1.IsTrue && c2 );
		}
		public static bool operator &(bool c1, LambdaParameterWrapper c2) {
			return c1 && c2.IsTrue;
		}
		public static bool operator true(LambdaParameterWrapper x) {
			return x.IsTrue;
		}

		public static LambdaParameterWrapper operator |(LambdaParameterWrapper c1, LambdaParameterWrapper c2) {
			return new LambdaParameterWrapper( c1.IsTrue || ConvertManager.ChangeType<bool>(c2.Value) );
		}
		public static LambdaParameterWrapper operator |(LambdaParameterWrapper c1, bool c2) {
			return new LambdaParameterWrapper( c1.IsTrue || c2 );
		}
		public static bool operator |(bool c1, LambdaParameterWrapper c2) {
			return c1 || c2.IsTrue;
		}
		public static bool operator false(LambdaParameterWrapper x) {
			return !x.IsTrue;
		}

	}

}

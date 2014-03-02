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
using NReco.Converting;

namespace NReco {
	
	/// <summary>
	/// Generic "by value" comparer that uses ConvertManager for types harmonization
	/// </summary>
	public class ValueComparer : IComparer, IComparer<object> {

		static ValueComparer _Instance = new ValueComparer();

		public static ValueComparer Instance {
			get {
				return _Instance;
			}
		}

		public int Compare(object a, object b) {
			if (a == null && b == null)
				return 0;
			if (a == null && b != null)
				return -1;
			if (a != null && b == null)
				return 1;

			if ((a is IList) && (b is IList)) {
				IList aList = (IList)a;
				IList bList = (IList)b;
				if (aList.Count < bList.Count)
					return -1;
				if (aList.Count > bList.Count)
					return +1;
				for (int i = 0; i < aList.Count; i++) {
					int r = Compare(aList[i], bList[i]);
					if (r != 0)
						return r;
				}
				// lists are equal
				return 0;
			}
			// test for quick compare if a type is assignable from b
			if (a is IComparable) {
				var aComp = (IComparable)a;
				// quick compare if types are fully compatible
				if (a.GetType().IsAssignableFrom(b.GetType()))
					return aComp.CompareTo(b);
			}
			if (b is IComparable) {
				var bComp = (IComparable)b;
				// quick compare if types are fully compatible
				if (b.GetType().IsAssignableFrom(a.GetType()))
					return -bComp.CompareTo(a);
			}

			// try to convert b to a and then compare
			if (a is IComparable) {
				var aComp = (IComparable)a;
				var conv = ConvertManager.FindConverter(b.GetType(), a.GetType());
				if (conv != null) {
					var bConverted = conv.Convert(b, a.GetType());
					return aComp.CompareTo(bConverted);
				}
			}
			// try to convert a to b and then compare
			if (b is IComparable) {
				var bComp = (IComparable)b;
				var conv = ConvertManager.FindConverter(a.GetType(), b.GetType());
				// try to compare without any conversions
				if (conv != null) {
					var aConverted = conv.Convert(a, b.GetType());
					return -bComp.CompareTo(aConverted);
				}
			}

			throw new InvalidCastException(String.Format("Cannot compare {0} and {1}", a.GetType(), b.GetType() ));
		}

	}

}

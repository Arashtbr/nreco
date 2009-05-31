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
using System.Text;

namespace NReco.Composition {
	
	/// <summary>
	/// Const value provider
	/// </summary>
	/// <typeparam name="ResT">provider result type</typeparam>
	public class ConstProvider<ContextT,ResT> : IProvider<ContextT,ResT> {
		ResT _Value = default(ResT);

		public ResT Value {
			get { return _Value; }
			set { _Value = value; }
		}

		public ConstProvider() { }

		public ConstProvider(ResT constValue) {
			Value = constValue;
		}


		public ResT Provide(ContextT context) {
			return Value;
		}

	}

	public class ConstProvider : ConstProvider<object,object>, IProvider<object,object> {
		public ConstProvider() { }
		public ConstProvider(object o) {
			Value = o;
		}
	}

}
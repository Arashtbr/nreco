﻿#region License
/*
 * NReco library (http://code.google.com/p/nreco/)
 * Copyright 2008 Vitaliy Fedorchenko
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

namespace NReco.Providers {
	
	public class DictionaryProvider : IProvider<object,IDictionary<string,object>> {

		public IDictionary<string, IProvider<object, object>> NameValueProviders { get; set; }

		public IDictionary<string, object> Provide(object context) {
			var dictionary = new Dictionary<string, object>();
			foreach (var entry in NameValueProviders)
				dictionary[entry.Key] = entry.Value.Provide(context);
			return dictionary;
		}

	}

}

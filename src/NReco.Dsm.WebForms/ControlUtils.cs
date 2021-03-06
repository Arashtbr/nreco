﻿#region License
/*
 * NReco library (http://nreco.googlecode.com/)
 * Copyright 2008-2014 Vitaliy Fedorchenko
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Routing;
using NReco;
using NReco.Converting;
using NReco.Logging;
using NReco.Collections;

namespace NReco.Dsm.WebForms {
	
	/// <summary>
	/// Control class extensions.
	/// </summary>
	public static class ControlUtils {

		/// <summary>
		/// Returns control parents axis of specified type.
		/// </summary>
		/// <returns>control parents axis ordered from direct parent to control tree root</returns>
		public static IEnumerable<T> GetParents<T>(Control ctrl)  {
			while (ctrl.Parent != null) {
				ctrl = ctrl.Parent;
				if (ctrl is T)
					yield return (T)((object)ctrl);
			}
		}

		/// <summary>
		/// Returns control childres of specified type.
		/// </summary>
		/// <remarks>
		/// This method performs breadth-first search (that can avoid full subtree traversal for some cases)
		/// and doesn't uses much memory even for huge subtrees.
		/// </remarks>
		public static IEnumerable<T> GetChildren<T>(Control ctrl) {
			var q = new Queue<Control>();
			for (int i = 0; i < ctrl.Controls.Count; i++)
				q.Enqueue(ctrl.Controls[i]);
			while (q.Count>0) {
				var c = q.Dequeue();
				if (c is T)
					yield return (T)((object)c);
				for (int i = 0; i < c.Controls.Count; i++)
					q.Enqueue(c.Controls[i]);
			}
		}

		public static void SetSelectedItems(ListControl ctrl, string[] values) {
			SetSelectedItems(ctrl, values, false);
		}

		public static void SetSelectedItems(ListControl ctrl, string[] values, bool preserveOrder) {
			foreach (ListItem itm in ctrl.Items)
				itm.Selected = false;
			if (values!=null) {
				if (preserveOrder) {
					int i = 0;
					foreach (string val in values) {
						var itm = ctrl.Items.FindByValue(values[i]);
						if (itm!=null) {
							itm.Selected = true;
							ctrl.Items.Remove(itm);
							ctrl.Items.Insert(i++, itm);
						}
					}
				} else {
					foreach (ListItem itm in ctrl.Items)
						if (values.Contains(itm.Value))
							itm.Selected = true;
				}
			}
		}
		public static string[] GetSelectedItems(ListControl ctrl) {
			var q = from ListItem r in ctrl.Items
					where r.Selected
					select r.Value;
			var res = new List<string>();
			foreach (var val in q)
				res.Add(val);
			return res.ToArray();
		}

		public static object GetControlValue(Control container, string ctrlId) {
			var ctrl = container.FindControl(ctrlId);
			if (ctrl == null)
				return null;
			if (ctrl is ITextControl)
				return ((ITextControl)ctrl).Text;
			if (ctrl is ICheckBoxControl)
				return ((ICheckBoxControl)ctrl).Checked;
			if (ctrl is IDateBoxControl) {
				var val = ((IDateBoxControl)ctrl).Date;
				if (val.HasValue) {
					return val.Value;
				} else {
					return null;
				}
			}
			throw new Exception("Cannot extract control value from " + ctrl.GetType().ToString());
		}
		public static void SetControlValue(Control container, string ctrlId, object val) {
			var ctrl = container.FindControl(ctrlId);
			if (ctrl == null)
				return;
			if (ctrl is ITextControl)
				((ITextControl)ctrl).Text = Convert.ToString(val);
			if (ctrl is ICheckBoxControl)
				((ICheckBoxControl)ctrl).Checked = ConvertManager.ChangeType<bool>(val);
			if (ctrl is IDateBoxControl)
				((IDateBoxControl)ctrl).Date = AssertHelper.IsFuzzyEmpty(val) ? null : (DateTime?)ConvertManager.ChangeType<DateTime>(val);
		}

		public static IList WrapWithDictionaryView(IEnumerable data) {
			var list = new List<object>();
			if (data != null)
				foreach (var elem in data) {
					if (elem is IDictionary) {
						list.Add(new DictionaryView((IDictionary)elem));
					} else {
						list.Add(elem);
					}
				}
			return list;
		}

		public static void IncludeCssFile(Page page, string cssFile) {
			var absCssFile = cssFile;
			if (!cssFile.StartsWith("http") && !cssFile.StartsWith("//"))
				absCssFile = VirtualPathUtility.ToAbsolute(absCssFile);
			if (page.Header==null) return;
			if (page.Header.FindControl(absCssFile.ToLower()) != null)
				return;
			var cssLink = new System.Web.UI.HtmlControls.HtmlLink();
			cssLink.ID = absCssFile.ToLower();
			cssLink.Href = absCssFile;
			cssLink.Attributes.Add("rel", "stylesheet");
			cssLink.Attributes.Add("type", "text/css");
			page.Header.Controls.Add(cssLink);
		}

	}

}

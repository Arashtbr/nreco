<!--
NReco library (http://nreco.googlecode.com/)
Copyright 2008,2009 Vitaliy Fedorchenko
Distributed under the LGPL licence
 
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
-->	
<xsl:stylesheet version='1.0' 
				xmlns:e="urn:schemas-nreco:nreco:entity:v1"
				xmlns:l="urn:schemas-nreco:nreco:web:layout:v1"
				xmlns:xsl='http://www.w3.org/1999/XSL/Transform' 
				xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
				xmlns:Dalc="urn:remove"
				xmlns:NReco="urn:remove"
				xmlns:asp="urn:remove"
				exclude-result-prefixes="msxsl">

	<xsl:output method='xml' indent='yes' />

	<xsl:template match="l:field[l:editor/l:jwysiwyg]" mode="register-editor-css">
		<link rel="stylesheet" type="text/css" href="css/jwysiwyg/jquery.wysiwyg.css" />
	</xsl:template>	

	<xsl:template match="l:field[l:editor/l:jwysiwyg]" mode="register-editor-control">
		<xsl:apply-templates select="l:editor/l:jwysiwyg/l:plugins/node()" mode="register-editor-control"/>
	</xsl:template>
	
	<xsl:template match="l:field[l:editor/l:jwysiwyg]" mode="form-view-editor">
		<xsl:param name="mode"/>
		<xsl:variable name="uniqueId"><xsl:value-of select="@name"/>_<xsl:value-of select="$mode"/>_<xsl:value-of select="generate-id(.)"/></xsl:variable>
		<asp:TextBox id="{@name}" runat="server" Text='@@lt;%# Bind("{@name}") %@@gt;' TextMode="multiline" OnLoad="jWysiwygEditor_{$uniqueId}_onLoad">
			<xsl:if test="l:editor/l:jwysiwyg/@rows">
				<xsl:attribute name="Rows"><xsl:value-of select="l:editor/l:jwysiwyg/@rows"/></xsl:attribute>
			</xsl:if>
		</asp:TextBox>
		<script language="c#" runat="server">
		protected void jWysiwygEditor_<xsl:value-of select="$uniqueId"/>_onLoad(object sender, EventArgs e) {
			var scriptName = "js/jquery.wysiwyg.js";
			var scriptTag = "@@lt;s"+"cript language='javascript' src='"+scriptName+"'@@gt;@@lt;/s"+"cript@@gt;";
			if (!Page.ClientScript.IsStartupScriptRegistered(Page.GetType(), scriptName)) {
				Page.ClientScript.RegisterStartupScript(Page.GetType(), scriptName, scriptTag, false);
			}
			// one more for update panel
			System.Web.UI.ScriptManager.RegisterClientScriptInclude(Page, Page.GetType(), scriptName, "ScriptLoader.axd?path="+scriptName);
		}
		</script>	
		<script language="javascript">
		jQuery('#@@lt;%# Container.FindControl("<xsl:value-of select="@name"/>").ClientID %@@gt;').wysiwyg(
			{
				controls : {
						bold          : { visible : true, tags : ['b', 'strong'], css : { fontWeight : 'bold' } },
						italic        : { visible : true, tags : ['i', 'em'], css : { fontStyle : 'italic' } },
						strikeThrough : { visible :  true },
						underline     : { visible :  true },

						separator00 : { visible :  true },

						justifyLeft   : { visible :  true },
						justifyCenter : { visible :  true },
						justifyRight  : { visible :  true},
						justifyFull   : { visible :  true },

						separator01 : { visible :  true},
						
						indent  : { visible :  true },
						outdent : { visible :  true },
						
						separator02 : { visible :  true },

						subscript   : { visible :  true },
						superscript : { visible :  true},
						
						separator03 : { visible :  true },
						
						undo : { visible :  true },
						redo : { visible :  true },
						
						separator04 : { visible :  true },
						
						insertOrderedList    : { visible :  true },
						insertUnorderedList  : { visible :  true },
						insertHorizontalRule : { visible :  true },
						separator06 : { separator : true },
						
						<xsl:if test="l:editor/l:jwysiwyg/l:plugins/l:*[@toolbar='insertImage']">
						insertImage : {
							visible : true,
							exec    : function()
							{
								jwysiwygOpen<xsl:value-of select="$uniqueId"/><xsl:value-of select="generate-id(l:editor/l:jwysiwyg/l:plugins/l:*[@toolbar='insertImage'])"/>( 
									function(imgUrl) {
										$('#@@lt;%# Container.FindControl("<xsl:value-of select="@name"/>").ClientID %@@gt;').wysiwyg('insertImage', imgUrl, {});
									}
								);
							},
							tags : ['img']
						},						
						</xsl:if>
						
						separator07 : { visible : true},
						cut   : { visible : true },
						copy  : { visible : true},
						paste : { visible : true },
						html : {visible : true}
				}
			}
		);
		</script>
		<xsl:for-each select="l:editor/l:jwysiwyg/l:plugins/node()">
			<xsl:apply-templates select="." mode="editor-jwysiwyg-plugin">
				<xsl:with-param name="openJsFunction">jwysiwygOpen<xsl:value-of select="$uniqueId"/><xsl:value-of select="generate-id(.)"/></xsl:with-param>
			</xsl:apply-templates>
		</xsl:for-each>
	</xsl:template>		
	
</xsl:stylesheet>
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

	<xsl:template match="l:vfsmanager" mode="aspnet-renderer">
		@@lt;%@ Register TagPrefix="Plugin" tagName="VfsManager" src="~/templates/renderers/VfsManager.ascx" %@@gt;
		<Plugin:VfsManager runat="server" xmlns:Plugin="urn:remove" FileSystemName="{@filesystem}"/> 
	</xsl:template>

	<xsl:template match="l:vfs-insert-image" mode="register-jwysiwyg-plugin-control">
		@@lt;%@ Register TagPrefix="Plugin" tagName="VfsSelector" src="~/templates/renderers/VfsSelector.ascx" %@@gt;
	</xsl:template>	

	<xsl:template match="l:vfs-insert-image" mode="register-editor-css">
		<link rel="stylesheet" type="text/css" href="css/jqueryFileTree/jqueryFileTree.css" />
	</xsl:template>
	
	<xsl:template match="l:vfs-insert-image" mode="editor-jwysiwyg-plugin">
		<xsl:param name="openJsFunction"/>
		<Plugin:VfsSelector runat="server" xmlns:Plugin="urn:remove" 
			OpenJsFunction="{$openJsFunction}"
			FileSystemName="{@filesystem}"/> 
	</xsl:template>
	
	
	<xsl:template match="l:field[l:editor/l:singlefile]" mode="register-editor-control">
		@@lt;%@ Register TagPrefix="Plugin" tagName="SingleFileEditor" src="~/templates/editors/SingleFileEditor.ascx" %@@gt;
	</xsl:template>

	<xsl:template match="l:field[l:editor/l:singlefile]" mode="form-view-editor">
		<Plugin:SingleFileEditor runat="server" xmlns:Plugin="urn:remove"
			id="{@name}"
			FileSystemName="{l:editor/l:singlefile/@filesystem}"
			BasePath="{l:editor/l:singlefile/@basepath}"
			Value='@@lt;%# Bind("{@name}") %@@gt;'
		/>
	</xsl:template>		
	
	<xsl:template match="l:field[l:editor/l:singlefile]" mode="aspnet-renderer">
		<Plugin:SingleFileEditor runat="server" xmlns:Plugin="urn:remove"
			id="{@name}"
			ReadOnly="true"
			FileSystemName="{l:editor/l:singlefile/@filesystem}"
			BasePath="{l:editor/l:singlefile/@basepath}"
			Value='@@lt;%# Eval("{@name}") %@@gt;'
		/>		
	</xsl:template>

	
	
</xsl:stylesheet>
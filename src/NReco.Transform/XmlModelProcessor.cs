﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.IO;
using System.Text.RegularExpressions;

using NI.Vfs;

namespace NReco.Transform {
	
	/// <summary>
	/// XML models processor based on VFS and XSLT
	/// </summary>
	public class XmlModelProcessor {

		protected IFileSystem FileSystem { get; set; }

		protected Encoding TransformResultEncoding = new UTF8Encoding(true);

		/// <summary>
		/// Initializes new instance of XmlModelProcessor with specified root path of local filesystem
		/// </summary>
		/// <param name="rootPath"></param>
		public XmlModelProcessor(string rootPath) {
			FileSystem = new LocalFileSystem(rootPath);
		}

		/// <summary>
		/// Initializes new instance of XmlModelProcessor with specified IFileSystem component
		/// </summary>
		/// <param name="fs">filesystem that should be used by model processor for reading XML models and writing tranformation results</param>
		public XmlModelProcessor(IFileSystem fs) {
			FileSystem = fs;
		}

		public string[] TransformModel(string xmlFilePath) {
			var xmlFile = FileSystem.ResolveFile(xmlFilePath);
			if (!xmlFile.Exists())
				throw new FileNotFoundException(xmlFilePath);
			var basePath = Path.GetDirectoryName(xmlFile.Name);
			
			TextReader inputXmlContent = null;
			using (var fsInputStream = xmlFile.Content.GetStream(FileAccess.Read) ) {
				var vfsResolver = new VfsXmlResolver(FileSystem, basePath);
				var xmlRdr = XmlReader.Create(
					new StreamReader(fsInputStream), null,
					new XmlParserContext(null, null, null, XmlSpace.Default) {
						BaseURI = vfsResolver.AbsoluteBaseUri.ToString() 
				});

				var xIncludingXmlRdr = new Mvp.Xml.XInclude.XIncludingReader(xmlRdr);
				xIncludingXmlRdr.XmlResolver = vfsResolver;

				var xPathDoc = new XPathDocument(xIncludingXmlRdr);
				var nav = xPathDoc.CreateNavigator();
				inputXmlContent = new StringReader( nav.OuterXml );
			}

			return TransformModel(inputXmlContent, basePath, Path.GetFileNameWithoutExtension(xmlFile.Name) );
		}

		protected string NormalizeFilePath(string filePath) {
			if (filePath.Contains("..")) {
				var fakeRootPath = "f:\\";
				var fakeAbsolutePath = Path.GetFullPath(Path.Combine(fakeRootPath, filePath));
				return fakeAbsolutePath.Substring(Path.GetPathRoot(fakeAbsolutePath).Length);
			}
			return filePath;
		}

		public string[] TransformModel(TextReader xmlContentRdr, string basePath, string outputFile) {
			var xPathDoc = new XPathDocument(xmlContentRdr);
			var nav = xPathDoc.CreateNavigator();
			var xmlStylesheetPI = nav.SelectSingleNode("processing-instruction('xml-stylesheet')[contains(.,'type=\"text/xsl\"')]");
			if (xmlStylesheetPI == null)
				throw new Exception("Cannot transform XML mode: xml-stylesheet processing instruction is missed");

			var piAttrs = ReadPIAttributes(Convert.ToString(xmlStylesheetPI.TypedValue));
			if (!piAttrs.ContainsKey("href"))
				throw new Exception("xml-stylesheet href is missing");
			var xslPath = Path.Combine(basePath, piAttrs["href"]);

			var xslt = GetXslTransform(xslPath);
			var resWr = new StringWriter();
			xslt.Transform(xPathDoc, new XmlTextWriter(resWr) {
				Formatting = Formatting.Indented,
				IndentChar = '\t',
				Indentation = 1
			});

			var outputContent = resWr.ToString();
			var outputBasePath = basePath; 

			if (piAttrs.ContainsKey("output-file"))
				outputFile = piAttrs["output-file"];

			if (piAttrs.ContainsKey("output-base-path"))
				outputBasePath = Path.Combine(outputBasePath, piAttrs["output-base-path"] );

			var outputFilesList = new List<string>();
			if (outputFile == "*") {
				var resultXPathDoc = new XPathDocument(new StringReader(outputContent));
				var filesNav = resultXPathDoc.CreateNavigator().Select("/files/file");
				
				foreach (XPathNavigator fNav in filesNav) {
					var fileName = fNav.GetAttribute("name",String.Empty);
					if (String.IsNullOrEmpty(fileName))
						throw new Exception("Cannot write file result: name attribute is missing");
					var filePath = Path.Combine(outputBasePath, fileName);
					WriteResult(filePath, PrepareTransformedContent(fNav.InnerXml));
					outputFilesList.Add( NormalizeFilePath( filePath ) );
				}
			} else {
				var outFilePath = Path.Combine(outputBasePath, outputFile);
				WriteResult(outFilePath, PrepareTransformedContent(outputContent) );
				outputFilesList.Add( NormalizeFilePath(outFilePath) );
			}
			return outputFilesList.ToArray();
		}

		protected void WriteResult(string outputFilePath, string content) {
			var contentPreamble = TransformResultEncoding.GetPreamble();
			var contentBytes = TransformResultEncoding.GetBytes(content);
			var outputFile = FileSystem.ResolveFile(outputFilePath);
			if (outputFile.Type == FileType.File) {
				var fileSize = outputFile.Content.Size;
				if (fileSize == (contentBytes.Length+contentPreamble.Length) ) {
					// read content and compare
					string currentFileContent = outputFile.ReadAllText();
					if (currentFileContent == content)
						return; // content is identical - no need to perform write operation
				}
				outputFile.Delete(); // remove old file
			}
			outputFile.CreateFile();
			using (var outFileStream = outputFile.Content.GetStream(FileAccess.Write) ) {
				outFileStream.Write( contentPreamble, 0, contentPreamble.Length );
				outFileStream.Write( contentBytes, 0, contentBytes.Length );
			}
		}

		protected XslCompiledTransform GetXslTransform(string xslPath) {
			var xslFile = FileSystem.ResolveFile( xslPath );
			if (!xslFile.Exists())
				throw new FileNotFoundException( xslPath );

			var xslt = new XslCompiledTransform();
			var xmlResolver = new VfsXmlResolver(FileSystem, Path.GetDirectoryName(xslPath) );

			using (var xslInputStream = xslFile.Content.GetStream(FileAccess.Read) ) {
				xslt.Load(new XmlTextReader(new StreamReader(xslInputStream)),
					XsltSettings.Default, xmlResolver);
			}
			return xslt;
		}

		protected IDictionary<string, string> ReadPIAttributes(string piData) {
			var piXmlNode = String.Format("<pi {0}/>", piData);
			var piDoc = new XmlDocument();
			piDoc.LoadXml( piXmlNode );
			var res = new Dictionary<string,string>();
			foreach (XmlAttribute xmlAttr in piDoc.DocumentElement.Attributes) {
				res[ xmlAttr.Name ] = xmlAttr.Value;
			}
			return res;
		}


		static Regex RemoveNamespaceRegex = new Regex(@"xmlns:[a-z0-9]+\s*=\s*[""']urn:remove[^""']*[""']", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

		public string PrepareTransformedContent(string content) {
			content = XmlHelper.DecodeSpecialChars(content);
			// also lets take about special namespace, 'urn:remove' that used when xmlns declaration should be totally removed 
			// (for 'asp' prefix for instance)
			return RemoveNamespaceRegex.Replace(content, String.Empty);
		}

	}

}

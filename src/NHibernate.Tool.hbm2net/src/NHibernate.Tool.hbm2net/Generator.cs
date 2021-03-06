using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

using log4net;

using NHibernate.Util;

using Element = System.Xml.XmlElement;

namespace NHibernate.Tool.hbm2net
{
	/// <summary> </summary>
	public class Generator
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public virtual string BaseDirName
		{
			get { return baseDirName; }
			set
			{
				if ((Object) value != null)
				{
					this.baseDirName = value;
				}
			}
		}

		private string rendererClass = "NHibernate.Tool.hbm2net.VelocityRenderer";
		private string baseDirName = "generated";
		private string packageName = null;
		private string suffix;
		private string prefix;
		private string extension = "cs";
		private bool lowerFirstLetter = false;
		private DirectoryInfo workingDirectory;
		public NameValueCollection params_Renamed = new NameValueCollection();

		/// <summary> Constructs a new Generator using the defaults.</summary>
		public Generator(DirectoryInfo workingDirectory)
		{
			this.workingDirectory = workingDirectory;
			this.suffix = string.Empty;
			this.prefix = string.Empty;
		}
        IRenderer renderer;
        public Generator(IRenderer renderer)
        {
            this.renderer = renderer;
            this.packageName = string.Empty;
        }

		/// <summary> Constructs a new Generator, configured from XML.</summary>
		public Generator(DirectoryInfo workingDirectory, Element generateElement) : this(workingDirectory)
		{
			string value_Renamed = null;

			// set rendererClass field
			if (
				(Object)
				(this.rendererClass =
				 (generateElement.Attributes["renderer"] == null ? null : generateElement.Attributes["renderer"].Value)) == null)
			{
				throw new Exception("attribute renderer is required.");
			}

			// set dirName field
			if (
				(Object)
				(value_Renamed = (generateElement.Attributes["dir"] == null ? null : generateElement.Attributes["dir"].Value)) !=
				null)
			{
				this.baseDirName = value_Renamed;
			}

			// set packageName field
			this.packageName = (generateElement.Attributes["package"] == null
                                    ? string.Empty : generateElement.Attributes["package"].Value);

			// set prefix
			if (
				(Object)
				(value_Renamed = (generateElement.Attributes["prefix"] == null ? null : generateElement.Attributes["prefix"].Value)) !=
				null)
			{
				this.prefix = value_Renamed;
			}

			// set suffix
			if (
				(Object)
				(value_Renamed = (generateElement.Attributes["suffix"] == null ? null : generateElement.Attributes["suffix"].Value)) !=
				null)
			{
				this.suffix = value_Renamed;
			}

			// set extension
			if (
				(Object)
				(value_Renamed =
				 (generateElement.Attributes["extension"] == null ? null : generateElement.Attributes["extension"].Value)) != null)
			{
				this.extension = value_Renamed;
			}

			// set lowerFirstLetter
			value_Renamed = (generateElement.Attributes["lowerFirstLetter"] == null
			                 	? null : generateElement.Attributes["lowerFirstLetter"].Value);
			try
			{
				this.lowerFirstLetter = Boolean.Parse(value_Renamed);
			}
			catch
			{
			}

			IEnumerator iter = generateElement.SelectNodes("param").GetEnumerator();
			while (iter.MoveNext())
			{
				Element childNode = (Element) iter.Current;
				params_Renamed[childNode.Attributes["name"].Value] = childNode.InnerText;
			}
		}
        IFileCreationObserver fileObserver;
		/// <summary> </summary>
		public virtual void Generate(IDictionary classMappingsCol,IFileCreationObserver fileObserver,bool checkIfNewer)
		{
            this.fileObserver = fileObserver;
			log.Info("Working on " + classMappingsCol.Count + " classes/component, output generated in:" + BaseDirName);
            if (null == renderer)
            {
                renderer = (IRenderer)SupportClass.CreateNewInstance(System.Type.GetType(this.rendererClass));
                //Configure renderer
                renderer.Configure(workingDirectory, params_Renamed);
            }

			//Running through actual classes
			for (IEnumerator classMappings = classMappingsCol.Values.GetEnumerator(); classMappings.MoveNext();)
			{
				ClassMapping classMapping = (ClassMapping) classMappings.Current;
                WriteRecursive(classMapping, classMappingsCol, renderer, checkIfNewer);
			}
			//Running through components
			for (IEnumerator cmpMappings = ClassMapping.Components; cmpMappings.MoveNext();)
			{
				ClassMapping mapping = (ClassMapping) cmpMappings.Current;
                Write(mapping, classMappingsCol, renderer, checkIfNewer);
			}
		}

		private void WriteRecursive(ClassMapping classMapping, IDictionary class2classmap, IRenderer renderer,bool checkIfNewer)
		{
			Write(classMapping, class2classmap, renderer,checkIfNewer);

			if (!(classMapping.Subclasses.Count == 0))
			{
				IEnumerator it = classMapping.Subclasses.GetEnumerator();
				while (it.MoveNext())
				{
					WriteRecursive((ClassMapping) it.Current, class2classmap, renderer,checkIfNewer);
				}
			}
		}


		/// <summary> </summary>
		private void Write(ClassMapping classMapping, IDictionary class2classmap, IRenderer renderer,bool checkIfNewer)
		{
			string saveToPackage = renderer.GetSaveToPackage(classMapping);
			string saveToClassName = renderer.GetSaveToClassName(classMapping);
			FileInfo dir = this.GetDir(saveToPackage);
            StreamWriter writer = null; 
            // a render implementing this interface can
            // decide a stream for output, so a directive in the 
            // generation code can drive the output naming...
            ICanProvideStream streamProvider = renderer as ICanProvideStream;
            string fileName=null;
            bool performGeneration = true;
            try
            {
                if (null == streamProvider)
                {
                    fileName = Path.Combine(dir.FullName, this.GetFileName(saveToClassName));
                    FileInfo file = new FileInfo(fileName);
                    if (checkIfNewer)
                    {
                        FileInfo sourceFileinfo = SourceFileInfoMap.Instance.LookupByMapping(classMapping);
                        performGeneration = sourceFileinfo.LastWriteTimeUtc >= file.LastWriteTimeUtc;
                        if (false == performGeneration)
                            LogFileSkipped(sourceFileinfo, file);
                    }
                    if (performGeneration)
                    {
                        log.Debug("Writing " + file);
                        writer = new StreamWriter(new FileStream(file.FullName, FileMode.Create));
                    }
                }
                else
                {
                    if (performGeneration)
                    {
                        if (checkIfNewer)
                        {
                            FileInfo sourceFileinfo = SourceFileInfoMap.Instance.LookupByMapping(classMapping);
                            FileInfo target;
                            performGeneration = streamProvider.CheckIfSourceIsNewer(sourceFileinfo.LastWriteTimeUtc, dir.FullName, classMapping, out target);
                            if (false == performGeneration)
                                LogFileSkipped(sourceFileinfo, target);

                        }
                        if (performGeneration)
                            writer = new StreamWriter(streamProvider.GetStream(classMapping, dir.FullName, out fileName));
                        log.Debug("Renderer:" + renderer.GetType().Name + " provided a stream for output.");
                    }
                }

                if (performGeneration)
                {
                    renderer.Render(GetPackageName(saveToPackage), GetName(saveToClassName), classMapping, class2classmap, writer);
                    writer.Close();
                    if (null != fileObserver)
                    {
                        fileObserver.FileCreated(fileName);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error writing file:", e);
            }
		}

        private void LogFileSkipped(FileInfo sourceFileinfo, FileInfo file)
        {
            log.Warn(string.Format("Skipping:{1} - target file {0} is newer than source hbm:{1}",file.FullName,sourceFileinfo.Name));
        }

		/// <summary> </summary>
		private string GetFileName(string className)
		{
			return this.GetName(className) + "." + this.extension;
		}

		/// <summary> </summary>
		private string GetName(string className)
		{
			string name = null;

			if (this.lowerFirstLetter)
			{
				name = className.Substring(0, (1) - (0)).ToLower() + className.Substring(1, (className.Length) - (1));
			}
			else
			{
				name = className;
			}

			return this.prefix + name + this.suffix;
		}

		private string GetPackageName(string packageName)
		{
			if ((Object) this.packageName == null)
			{
				return (Object) packageName == null ? string.Empty : packageName;
			}
			else
			{
				return this.packageName;
			}
		}

		/// <summary> </summary>
		private FileInfo GetDir(string packageName)
		{
			FileInfo baseDir = new FileInfo(this.baseDirName);
			FileInfo dir = null;

			string p = GetPackageName(packageName);

			dir = new FileInfo(Path.Combine(baseDir.FullName, p.Replace(StringHelper.Dot, Path.DirectorySeparatorChar)));

			// if the directory exists, make sure it is a directory
			bool tmpBool;
			if (File.Exists(dir.FullName))
				tmpBool = true;
			else
				tmpBool = Directory.Exists(dir.FullName);
			if (tmpBool)
			{
				if (!Directory.Exists(dir.FullName))
				{
					throw new Exception("The path: " + dir.FullName + " exists, but is not a directory");
				}
			}
				// else make the directory and any non-existent parent directories
			else
			{
				if (!Directory.CreateDirectory(dir.FullName).Exists)
				{
					throw new Exception("unable to create directory: " + dir.FullName);
				}
			}

			return dir;
		}
	}
}
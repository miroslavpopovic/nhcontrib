/*
* Created on 25-03-2003
*
* To change this generated comment go to 
* Window>Preferences>Java>Code Generation>Code Template
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace NHibernate.Tool.hbm2net
{
	/// <author>  max
	/// </author>
	public abstract class AbstractRenderer : IRenderer
	{
		internal NameValueCollection properties;
		private DirectoryInfo workingDriectory;

		public virtual void Render(string savedToPackage, string savedToClass, ClassMapping classMapping,
		                           IDictionary class2classmap, StreamWriter writer)
		{
		}

		public virtual void Configure(DirectoryInfo workingDirectory, NameValueCollection props)
		{
			this.workingDriectory = workingDirectory;
			this.properties = props;
		}

		public virtual string GetFieldScope(FieldProperty field, string localScopeName, string defaultScope)
		{
			return field.GetScope(localScopeName, defaultScope);
		}

		public virtual string GetPackageDeclaration(string savedToPackage, ClassMapping classMapping)
		{
			if ((Object) savedToPackage != null && !savedToPackage.Trim().Equals(""))
			{
				return "namespace " + savedToPackage + "";
			}
			else if ((Object) classMapping.GeneratedPackageName != null)
			{
				return "namespace " + classMapping.GeneratedPackageName + "";
			}
			return "";
		}

		protected internal virtual void GeneratePackageDelaration(string savedToPackage, ClassMapping classMapping, StreamWriter w)
		{
			string string_Renamed = GetPackageDeclaration(savedToPackage, classMapping);
			if (string_Renamed.Length > 0)
			{
				w.WriteLine(string_Renamed);
			}
			else
			{
				w.WriteLine("// default package");
			}
		}

		public virtual string GetSaveToClassName(ClassMapping classMapping)
		{
			return classMapping.GeneratedName;
		}

		public virtual string GetSaveToPackage(ClassMapping classMapping)
		{
			return classMapping.GeneratedPackageName;
		}

		public DirectoryInfo WorkingDirectory
		{
			get { return this.workingDriectory; }
		}
    }
}
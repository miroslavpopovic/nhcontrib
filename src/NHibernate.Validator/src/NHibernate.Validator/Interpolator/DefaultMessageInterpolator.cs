using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using NHibernate.Util;
using NHibernate.Validator.Engine;

namespace NHibernate.Validator.Interpolator
{
	public class DefaultMessageInterpolator : IMessageInterpolator
	{
		//TODO: Log this class!
		//private static Log log = LogFactory.getLog( DefaultMessageInterpolator.class );

		private CultureInfo culture = null;

		private Dictionary<string, object> attributeParameters = new Dictionary<string, object>();

		[NonSerialized] private ResourceManager messageBundle;

		[NonSerialized] private ResourceManager defaultMessageBundle;

		private string attributeMessage;

		private string interpolateMessage;

		public void Initialize(ResourceManager messageBundle, ResourceManager defaultMessageBundle, CultureInfo culture)
		{
			this.culture = culture;
			this.messageBundle = messageBundle;
			this.defaultMessageBundle = defaultMessageBundle;
		}

		public void Initialize(Attribute attribute, IMessageInterpolator defaultInterpolator)
		{
			//Get all parametters of the Attribute: the name and their values.
			//For example:
			//In LengthAttribute the parametter are Min and Max.
			System.Type clazz = attribute.GetType();
			
			IRuleArgs HasMessage = attribute as IRuleArgs;

			if (HasMessage == null)
				throw new ArgumentException("Attribute " + clazz + " doesn't implements IRuleArgs interface.");
			else
				this.attributeMessage = HasMessage.Message;

			foreach(PropertyInfo property in clazz.GetProperties())
			{
				attributeParameters.Add(property.Name, property.GetValue(attribute, null));
			}

			//if (attributeParameters.ContainsKey("Message"))
			//    attributeMessage = (string) attributeParameters["Message"];
			//else 
			//    throw new ArgumentException("Attribute " + clazz + " does not have an (accessible) Message attribute");
			
		}

		private String Replace(String message)
		{
			StringTokenizer tokens = new StringTokenizer(message, "#{}", true);
			StringBuilder buf = new StringBuilder(30);
			bool escaped = false;
			bool el = false;

			IEnumerator ie = tokens.GetEnumerator();

			while(ie.MoveNext())
			{
				string token = (string) ie.Current;

				if (!escaped && "#".Equals(token))
				{
					el = true;
				}
				if (!el && "{".Equals(token))
				{
					escaped = true;
				}
				else if (escaped && "}".Equals(token))
				{
					escaped = false;
				}
				else if (!escaped)
				{
					if ("{".Equals(token))
					{
						el = false;
					}
					buf.Append(token);
				}
				else
				{
					Object variable = attributeParameters.ContainsKey(token) ? attributeParameters[token] : null;
					if (variable != null)
					{
						buf.Append(variable);
					}
					else
					{
						string _string = null;
						try
						{
							//Diferent behavior that Hibernate.Validator
							//Try first with the current Culture in the Custom ResourceManager
							//Else trywith the default Culture in the Custom ResourceManager
							if (culture == null)
								_string = messageBundle != null ? messageBundle.GetString(token) : null;
							else
								_string = messageBundle != null ? messageBundle.GetString(token,culture) : null;
						}
						catch(MissingManifestResourceException)
						{
							//give a second chance with the default resource bundle
							if (messageBundle.Equals(defaultMessageBundle))
							{
								//return the unchanged string
								buf.Append('{').Append(token).Append('}');
							}
						}
						if (_string == null)
						{
							try
							{
								//Try first with the current Culture in the Default ResourceManager
								//Else trywith the default Culture in the Default ResourceManager
								if(culture == null)
									_string = defaultMessageBundle.GetString(token);
								else
									_string = defaultMessageBundle.GetString(token,culture);
							}
							catch(MissingManifestResourceException)
							{
								//return the unchanged string
								buf.Append('{').Append(token).Append('}');
							}
						}
						if (_string != null)
						{
							buf.Append(Replace(_string));
						}
					}
				}
			}
			return buf.ToString();
		}

		public string Interpolate(string message, IValidator validator, IMessageInterpolator defaultInterpolator)
		{
			if (attributeMessage.Equals(message))
			{
				//short cut
				if (interpolateMessage == null)
				{
					interpolateMessage = Replace(attributeMessage);
				}
				return interpolateMessage;
			}
			else
			{
				//TODO keep them in a weak hash map, but this might not even be useful
				return Replace(message);
			}
		}

		public string GetAttributeMessage()
		{
			return attributeMessage;
		}
	}
}
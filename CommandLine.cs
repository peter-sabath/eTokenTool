using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Articy.EToken
{
	public static class CommandLine
	{
		public static bool ParamtersSet { get; private set; }

		public static string ProcessName { get; set; }
		public static int Count
		{
			get { return sParameters.Count; }
		}

		// be sure we have a non empty list to ease usage
		private static List<string> sParameters = new List<string>();

		private static readonly CultureInfo sUsCulture = CultureInfo.GetCultureInfo("en-us");

		private static readonly Dictionary<string, int> sParameterPositions =
			new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

		public static void SetParameters (List<string> aParameters, bool aFirstIsProcessName = false)
		{
			sParameters = aParameters ?? new List<string>();
			ParamtersSet = true;
			sParameterPositions.Clear();
			if (aFirstIsProcessName)
			{
				ProcessName = sParameters[0];
				sParameters.RemoveAt(0);
			}
			else
			{
				var assemblyName = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
				ProcessName = assemblyName.ManifestModule.FullyQualifiedName;
			}
		}

		public static void SetParameters( string[] aParameters, bool aFirstIsProcessName = false)
		{
			SetParameters(new List<string>(aParameters ?? new string[] {}), aFirstIsProcessName);
		}

		public static string GetParameterAt(int aIndex)
		{
			return sParameters[aIndex];
		}

		public static int GetSwitchPosition( string aName )
		{
			if( aName == null )
				throw new ArgumentNullException();

			int position;
			if (sParameterPositions.TryGetValue(aName, out position))
				return position;

			aName = aName.ToLower().Trim();
			for( int i = 0; i < sParameters.Count; i++ )
			{
				string s = sParameters[ i ];
				string param = s.ToLower();
				if( !param.StartsWith( "-" ) && !param.StartsWith( "/" ) ) continue;
				if( param.Substring( 1 ).Equals( aName ) )
				{
					sParameterPositions[aName] = i;
					return i;
				}
			}
			sParameterPositions[aName] = -1;
			return -1;
		}

		public static bool IsSwitch( int aIndex, out string aName )
		{
			if ( aIndex >= 0 && aIndex < sParameters.Count )
			{
				string s = sParameters[aIndex];
				if (s.StartsWith("-") || s.StartsWith("/"))
				{
					aName = s.Substring(1).ToLower();
					return true;
				}
			}
			aName = null;
			return false;
		}

		public static bool HasSwitch( string aName )
		{
			int pos = GetSwitchPosition(aName);
			return pos >= 0;
		}

		public static string GetSwitchValue( string aName )
		{
			int pos = GetSwitchPosition( aName );

			if( pos < 0 || pos == sParameters.Count - 1 )
				return null;  // not found or last element
		    string value = sParameters[pos + 1];
            if ( value.StartsWith("-") )
            {
                return null;
            }
		    return value;
		}

		public static string GetSwitchValue( string aName, string aDefault )
		{
			return GetSwitchValue( aName ) ?? aDefault;
		}

		public static double GetSwitchValueDouble( string aName )
		{
			string val = GetSwitchValue( aName );
			return Double.Parse( val, NumberStyles.Float, sUsCulture );
		}

		public static double GetSwitchValueDouble( string aName, double aDefault )
		{
			string val = GetSwitchValue(aName);
			double number;
			return Double.TryParse(val, NumberStyles.Float, sUsCulture, out number) ? number : aDefault;
		}

		public static int GetSwitchValueInt( string aName )
		{
			string val = GetSwitchValue(aName);
			return Int32.Parse(val, NumberStyles.Integer, sUsCulture);
		}

		public static int GetSwitchValueInt(string aName, int aDefault)
		{
			string val = GetSwitchValue(aName);
			int number;
			return Int32.TryParse(val, NumberStyles.Integer, sUsCulture, out number) ? number : aDefault;
		}

		public static string AsString()
		{
			var sb = new StringBuilder();
			foreach (string s in sParameters)
			{
				if ( s.Contains(" ") || string.IsNullOrEmpty(s) )
					sb.Append("\"").Append(s).Append("\" ");
				else
					sb.Append(s).Append(" ");
			}
			return sb.ToString().TrimEnd();
		}

		public static List<string> GetParameters()
		{
			return new List<String>(sParameters);
		}

		public static void AddParameter( string aName, string aValue, bool aRemoveOther = true )
		{
			var withDash = aName.StartsWith("-") || aName.StartsWith("/") ? aName : "-" + aName;
			var withoutDash = aName.StartsWith("-") || aName.StartsWith("/") ? aName.Substring(1) : aName;

			if ( aRemoveOther )
				RemoveParameter( withoutDash, aValue != null );

			sParameters.Add(withDash);
			if (aValue != null)
				sParameters.Add(aValue);

			sParameterPositions.Clear();
		}

		public static void RemoveParameter( string aName, bool aRemoveValue = false )
		{
			var pos = GetSwitchPosition(aName);
			if (pos >= 0)
			{
				sParameters.RemoveAt(pos);
				if ( aRemoveValue)
					sParameters.RemoveAt(pos);
			}
			sParameterPositions.Clear();
		}
	}
}

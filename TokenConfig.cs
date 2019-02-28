using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Articy.EToken
{
	public class TokenConfig
	{
		public ReadOnlyDictionary<string, string> Entries
		{
			get { return new ReadOnlyDictionary<string, string>(mContainerPasswordMap); }
		}

		public int Count
		{
			get { return mContainerPasswordMap.Count; }
		}


		private readonly Dictionary<string, string> mAliasContainerMap;
		private readonly Dictionary<string, string> mContainerPasswordMap;


		//config
		//container-id#alias=encryptedpw

		public TokenConfig()
		{
			mAliasContainerMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			mContainerPasswordMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		}

		private static string ConfigName()
		{
			var defaultConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eTokenTool", "eTokenTool.cfg");
			return CommandLine.GetSwitchValue("config", defaultConfig);
		}

		public string GetAliasForEntry(string aTokenName)
		{
			var alias = mAliasContainerMap.FirstOrDefault(aAliasEntry => aAliasEntry.Value == aTokenName);
			return alias.Key;
		}

		public void Load()
		{
			var fn = ConfigName();
			if (!File.Exists(fn)) return;
			// load config
			var lines = File.ReadAllLines(fn);
			foreach (var line in lines)
			{
				var pos1 = line.IndexOf("=");
				if (pos1 <= 0)
					continue;

				string alias = null;
				var token = line.Substring(0, pos1).Trim();
				var pw = line.Substring(pos1 + 1).Trim();
				var pos2 = line.IndexOf("#");
				if (pos2 > 0)
				{
					alias = token.Substring(pos2 + 1).Trim();
					token = token.Substring(0, pos2);
				}
				AddEntry(token, pw, alias, false);  // don't encrypt already encrypted password
			}
		}

		public void Save()
		{
			var sb = new StringBuilder();
			foreach (var entry in mContainerPasswordMap)
			{
				sb.Append(entry.Key);
				var alias = GetAliasForEntry(entry.Key);
				if (alias != null)
				{
					sb.Append("#").Append(alias);
				}

				sb.Append("=")
				  .Append(entry.Value)
				  .Append("\n");
			}

			var lines = sb.ToString().Trim().Split('\n');
			var fn = ConfigName();
			Directory.CreateDirectory(new FileInfo(fn).DirectoryName);
			File.WriteAllLines(fn,lines);
		}

		public void AddEntry(string aTokenName, string aPassword, string aAlias = null, bool aEncryptPassword = true)
		{
			// force empty or whitespace strings to null
			aAlias = String.IsNullOrWhiteSpace(aAlias) ? null : aAlias;

			aPassword = aEncryptPassword
				? DpApi.Encrypt(CommandLine.HasSwitch("machine") ? DpApi.KeyType.MachineKey : DpApi.KeyType.UserKey, aPassword)
				: aPassword;

			mContainerPasswordMap.Add(aTokenName, aPassword);
			if ( aAlias != null )
				mAliasContainerMap.Add(aAlias, aTokenName);
		}

		public bool RemoveEntry(string aTokenNameOrAlias)
		{
			string key;
			// try to resolve alias
			mAliasContainerMap.TryGetValue(aTokenNameOrAlias, out key);
			if (key != null)
			{
				// parameter was an alias -> remove it
				mAliasContainerMap.Remove(aTokenNameOrAlias);
				aTokenNameOrAlias = key;	// change to container name
			}
			else
			{
				// param is a container name remove alias if found
				var oldAlias = GetAliasForEntry(aTokenNameOrAlias);
				if (oldAlias != null)
					mAliasContainerMap.Remove(oldAlias);
			}
			return mContainerPasswordMap.Remove(aTokenNameOrAlias);
		}

		public KeyValuePair<string, string> GetEntry(string aTokenNameOrAlias)
		{
			string key;
			mAliasContainerMap.TryGetValue(aTokenNameOrAlias, out key);
			string password;
			aTokenNameOrAlias = key ?? aTokenNameOrAlias;
			return mContainerPasswordMap.TryGetValue(aTokenNameOrAlias, out password)
				? new KeyValuePair<string, string>(aTokenNameOrAlias, password)
				: new KeyValuePair<string, string>(null, null);
		}


		public string GetPassword(string aTokenNameOrAlias)
		{
			string key;
			mAliasContainerMap.TryGetValue(aTokenNameOrAlias, out key);
			string password;
			mContainerPasswordMap.TryGetValue(key ?? aTokenNameOrAlias, out password);
			return password;
		}

		public string DecryptPassword(string aEncryptedPassword)
		{
			return DpApi.Decrypt(aEncryptedPassword);
		}
	}
}

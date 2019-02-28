using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Articy.EToken
{
	public class ETokenTool
	{
		private static TokenConfig sConfig;

		private const int cExitCodeWrongParameters = 1;
		private const int cExitCodeUnknownCommand = 2;
		private const int cExitCodeFailedToOpenToken = 3;
		private const int cExitCodeFailedToSetPassword = 4;
		private const int cExitCodeTokenRecordNotFound = 5;
		private const int cExitCodeFailedToDecryptPassword = 6;
		private const int cExitCodeFailedWithException = 7;

		private static void ErrorExit( string aErrorInfo, int aExitCode, bool aAddWin32Error = false)
		{
			var txt = aErrorInfo;
			if (aAddWin32Error)
			{
				var errCode = Marshal.GetLastWin32Error();
				string errorMessage = new Win32Exception(errCode).Message;
				txt += $"\r\nErrorCode: 0x{errCode:X}\r\n{errorMessage}";
			}
			Console.WriteLine(txt);
			Environment.Exit(aExitCode);
		}

		private static void Usage()
		{

			Environment.Exit(0);
		}

		private static void TestOrLogin(bool aDoLogin)
		{
			var id = CommandLine.GetSwitchValue("id", null);
			if (id == null)
			{
				foreach (var entry in sConfig.Entries.Keys)
				{
					AccessToken(entry, aDoLogin);
				}
			}
			else
			{
				AccessToken(id, aDoLogin);
			}
		}

		private static void AccessToken(string aTokenOrAlias, bool aSetPassword)
		{
			var entry = sConfig.GetEntry(aTokenOrAlias);
			if ( entry.Key == null )
				ErrorExit($"Token '{aTokenOrAlias}' not found in config", cExitCodeTokenRecordNotFound, false);
			using (var co = new CryptoObject())
			{
				// try to open token via CSP
				if ( !co.OpenToken(entry.Key) )
					ErrorExit($"Unable to open token '{entry.Key}'", cExitCodeFailedToOpenToken, true);

				if (!aSetPassword)
					return;

				// decrypt password for crypto-API call
				string password = null;
				try
				{
					password = sConfig.DecryptPassword(entry.Value);
				}
				catch
				{
					ErrorExit($"Failed to decrypt password for token '{entry.Key}'", cExitCodeFailedToDecryptPassword, true);
				}

				if ( !co.SetPassword( password ) )
					ErrorExit($"Failed to set password for token '{entry.Key}'", cExitCodeFailedToSetPassword, true);

				password = null;
				GC.Collect();
			}
		}

		private static void AddToken()
		{
			var token = CommandLine.GetSwitchValue("token");
			var alias = CommandLine.GetSwitchValue("alias");
			var pw = CommandLine.GetSwitchValue("password");

			if ( string.IsNullOrWhiteSpace(token) )
				ErrorExit($"The 'add' commands requires a non empty '-token' parameter", cExitCodeWrongParameters);

			if (string.IsNullOrWhiteSpace(pw))
				ErrorExit($"The 'add' commands requires a non empty '-password' parameter", cExitCodeWrongParameters);

			sConfig.AddEntry(token, pw, alias);
			sConfig.Save();
			Environment.Exit(0);
		}

		private static void RemoveToken()
		{
			var tokenOrAlias = CommandLine.GetSwitchValue("id", null);

			if (string.IsNullOrWhiteSpace(tokenOrAlias))
				ErrorExit($"The 'remove' commands requires an '-id' value", cExitCodeWrongParameters);

			if (sConfig.RemoveEntry(tokenOrAlias))
			{
				sConfig.Save();
			}
			else
			{
				ErrorExit($"Token '{tokenOrAlias}' not found in config", cExitCodeTokenRecordNotFound);
			}
		}

		private static void Login()
		{
			TestOrLogin(true);
		}

		private static void Test()
		{
			TestOrLogin(false);
		}

		private static void List()
		{
			var sb = new StringBuilder();

			sb.Append(sConfig.Count).AppendLine(" entries found:");

			foreach (var entry in sConfig.Entries)
			{
				var alias = sConfig.GetAliasForEntry(entry.Key);
				sb.Append("  ").Append(entry.Key);
				if (alias != null)
					sb.Append(" as '").Append(alias).Append("'");
				sb.AppendLine();
			}
			Console.WriteLine(sb.ToString());
		}

		public static void Main(string[] aArgs)
		{
			try
			{
				CommandLine.SetParameters(Environment.GetCommandLineArgs(), true);

				// we need as least 1 parameter (the command)
				if (CommandLine.Count == 0)
					Usage();

				sConfig = new TokenConfig();
				sConfig.Load();

				var cmd = CommandLine.GetParameterAt(0).ToLowerInvariant();
				switch (cmd)
				{
					case "add":
						AddToken();
						break;
					case "remove":
						RemoveToken();
						break;
					case "login":
						Login();
						break;
					case "test":
						Test();
						break;
					case "list":
						List();
						break;
					default:
						ErrorExit("Unknown command", cExitCodeUnknownCommand);
						break;
				}

				/*
					eTokenTool add [-config <configname>] -token <container-id> -password <password> [-alias <alias-name>] [-machine]
					eTokenTool remove [-config <configname>] -id <container-id | alias-name>
					eTokenTool login [-config <configname>] [-id <container-id | alias-name>]
					eTokenTool test [-config <configname>] [-id <container-id | alias-name>]
					eTokenTool list [-config <configname>]
				*/

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Environment.Exit(cExitCodeFailedWithException);
			}
		}
	}
}

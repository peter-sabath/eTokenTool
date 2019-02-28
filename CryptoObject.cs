using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Articy.EToken
{
	public class CryptoObject : IDisposable
	{
		#region P/Invoke imports
		// ReSharper disable InconsistentNaming
		// ReSharper disable IdentifierTypo

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptAcquireContext(out IntPtr hProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptSetProvParam(IntPtr hProv, uint dwParam, [In] byte[] pbData, uint dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

		// ReSharper enable InconsistentNaming
		// ReSharper enable IdentifierTypo
		#endregion

		private const string cProviderName = "eToken Base Cryptographic Provider";


		private IntPtr mHandle;

		public CryptoObject()
		{
			mHandle = IntPtr.Zero;
		}

		public void Dispose()
		{
			if (mHandle != IntPtr.Zero)
			{
				CryptReleaseContext(mHandle, 0);
			}
		}

		public bool OpenToken(string aContainerId)
		{
			return CryptAcquireContext(out mHandle, aContainerId, cProviderName, /*PROV_RSA_FULL*/ 1, /*CRYPT_SILENT*/ 0x00000040);
		}

		public bool SetPassword(string aPassword)
		{
			if (mHandle != IntPtr.Zero)
			{
				throw new ArgumentException("Container not aquired", "mHandle");
			}
			var data = Encoding.UTF8.GetBytes(aPassword);
			return CryptSetProvParam(mHandle, /*PP_SIGNATURE_PIN*/ 33, data, 0);
		}
	}
}

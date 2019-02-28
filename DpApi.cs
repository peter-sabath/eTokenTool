using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Articy.EToken
{
	/// <summary>
	/// Encrypts and decrypts data using DPAPI functions.
	/// </summary>
	public static class DpApi
	{
		#region P/Invoke imports
		// ReSharper disable InconsistentNaming
		// ReSharper disable IdentifierTypo

		// Wrapper for DPAPI CryptProtectData function.
		[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool CryptProtectData(
			ref DATA_BLOB pPlainText,
			string szDescription,
			ref DATA_BLOB pEntropy,
			IntPtr pReserved,
			ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
			int dwFlags,
			ref DATA_BLOB pCipherText
		);

		// Wrapper for DPAPI CryptUnprotectData function.
		[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool CryptUnprotectData(
			ref DATA_BLOB pCipherText,
			ref string pszDescription,
			ref DATA_BLOB pEntropy,
			IntPtr pReserved,
			ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
			int dwFlags,
			ref DATA_BLOB pPlainText
		);
		#endregion

		#region structures & constants for P/Invoke calls

		// DPAPI key initialization flags.
		// UI interaction is forbidden
		private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

		// Use the machine key instead of the user key for encryption
		private const int CRYPTPROTECT_LOCAL_MACHINE = 0x4;

		// BLOB structure used to pass data to DPAPI functions.
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct DATA_BLOB
		{
			public int cbData;
			public IntPtr pbData;
		}

		// Prompt structure to be used for required parameters.
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct CRYPTPROTECT_PROMPTSTRUCT
		{
			public int cbSize;
			public int dwPromptFlags;
			public IntPtr hwndApp;
			public string szPrompt;
		}
		// ReSharper enable InconsistentNaming
		// ReSharper enable IdentifierTypo

		#endregion

		// Flag indicating the type of key. DPAPI terminology refers to
		// key types as user store or machine store.
		public enum KeyType
		{
			UserKey = 1,
			MachineKey
		};

		#region convenience methods
		/// <summary>
		/// Initializes empty prompt structure to 'empty' since we do not use UI
		/// </summary>
		/// <param name="aPromptStruct">Prompt parameter</param>
		private static void InitPrompt(ref CRYPTPROTECT_PROMPTSTRUCT aPromptStruct)
		{
			aPromptStruct.cbSize = Marshal.SizeOf(typeof(CRYPTPROTECT_PROMPTSTRUCT));
			aPromptStruct.dwPromptFlags = 0;
			aPromptStruct.hwndApp = IntPtr.Zero;
			aPromptStruct.szPrompt = null;
		}

		/// <summary>
		/// Initializes a BLOB structure from a byte array.
		/// </summary>
		/// <param name="aData">Original data in a byte array format.</param>
		/// <param name="aBlob">Returned blob structure.</param>
		/// <param name="aExceptionText">The text that is used as message for a possible exception</param>
		private static void InitBlob(byte[] aData, ref DATA_BLOB aBlob, string aExceptionText)
		{
			try
			{
				// Use empty array for null parameter.
				if (aData == null)
					aData = new byte[0];

				// Allocate memory for the BLOB data.
				aBlob.pbData = Marshal.AllocHGlobal(aData.Length);

				// Make sure that memory allocation was successful.
				if (aBlob.pbData == IntPtr.Zero)
					throw new Exception("Unable to allocate data buffer for BLOB structure.");

				// Specify number of bytes in the BLOB.
				aBlob.cbData = aData.Length;

				// Copy data from original source to the BLOB structure.
				Marshal.Copy(aData, 0, aBlob.pbData, aData.Length);
			}
			catch (Exception ex)
			{
				throw new Exception(aExceptionText, ex);
			}
		}

		private static byte[] BytesFromBlob( ref DATA_BLOB aBlob )
		{
			var length = aBlob.cbData;
			var result = new byte[length];

			// Copy cipher-text from the BLOB to a byte array.
			if ( length > 0 )
				Marshal.Copy(aBlob.pbData, result, 0, length);

			return result;
		}

		/// <summary>
		/// free the unmanaged blob data
		/// </summary>
		/// <param name="aBlob"></param>
		private static void FreeBlob(ref DATA_BLOB aBlob)
		{
			if (aBlob.cbData > 0)
				Marshal.FreeHGlobal(aBlob.pbData);
		}

		#endregion

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt a plaintext
		/// string value.
		/// </summary>
		/// <param name="aKeyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specified, it will be required
		/// for decryption.
		/// </param>
		/// <param name="aPlainText">Plaintext data to be encrypted.</param>
		/// <param name="aEntropy">Optional entropy which - if specified - will be required to perform decryption.</param>
		/// <param name="aDescription">
		/// Optional description of data to be encrypted. If this value is
		/// specified, it will be stored along with encrypted data and
		/// returned as a separate value during decryption.
		/// </param>
		/// <returns>Encrypted value in a base64-encoded format.</returns>
		public static string Encrypt(KeyType aKeyType, string aPlainText, string aEntropy = null, string aDescription= null)
		{
			// Make sure that parameters are valid.
			if (aPlainText == null) aPlainText = String.Empty;
			if (aEntropy == null) aEntropy = String.Empty;

			// Call encryption routine and convert returned bytes into
			// a base64-encoded value.
			return Convert.ToBase64String(
				Encrypt(aKeyType,
					Encoding.UTF8.GetBytes(aPlainText),
					Encoding.UTF8.GetBytes(aEntropy),
					aDescription));
		}

		/// <summary>
		/// Calls DPAPI CryptProtectData function to encrypt an array of
		/// plaintext bytes.
		/// </summary>
		/// <param name="aKeyType">
		/// Defines type of encryption key to use. When user key is
		/// specified, any application running under the same user account
		/// as the one making this call, will be able to decrypt data.
		/// Machine key will allow any application running on the same
		/// computer where data were encrypted to perform decryption.
		/// Note: If optional entropy is specified, it will be required
		/// for decryption.
		/// </param>
		/// <param name="aPlainTextBytes">Plaintext data to be encrypted.</param>
		/// <param name="aEntropyBytes">Optional entropy which - if specified - will be required to perform decryption.</param>
		/// <param name="aDescription">
		/// Optional description of data to be encrypted. If this value is
		/// specified, it will be stored along with encrypted data and
		/// returned as a separate value during decryption.
		/// </param>
		/// <returns>Encrypted value.</returns>
		public static byte[] Encrypt(KeyType aKeyType, byte[] aPlainTextBytes, byte[] aEntropyBytes, string aDescription)
		{
			// Make sure that parameters are valid.
			if (aPlainTextBytes == null) aPlainTextBytes = new byte[0];
			if (aEntropyBytes == null) aEntropyBytes = new byte[0];
			if (aDescription == null) aDescription = String.Empty;

			// Create BLOBs to hold data.
			var plainTextBlob = new DATA_BLOB();
			var cipherTextBlob = new DATA_BLOB();
			var entropyBlob = new DATA_BLOB();

			// We only need prompt structure because it is a required // parameter.
			var prompt = new CRYPTPROTECT_PROMPTSTRUCT();
			InitPrompt(ref prompt);

			try
			{
				// Convert plaintext bytes into a BLOB structure.
				InitBlob(aPlainTextBytes, ref plainTextBlob, "Cannot initialize plaintext BLOB.");

				// Convert entropy bytes into a BLOB structure.
				InitBlob(aEntropyBytes, ref entropyBlob, "Cannot initialize entropy BLOB.");

				// Disable any types of UI.
				int flags = CRYPTPROTECT_UI_FORBIDDEN;

				// When using machine-specific key, set up machine flag.
				if (aKeyType == KeyType.MachineKey)
					flags |= CRYPTPROTECT_LOCAL_MACHINE;

				// Call DPAPI to encrypt data.
				var success = CryptProtectData(ref plainTextBlob,
					aDescription,
					ref entropyBlob,
					IntPtr.Zero,
					ref prompt,
					flags,
					ref cipherTextBlob);

				// Check the result.
				if (!success)
				{
					// If operation failed, retrieve last Win32 error.
					var errCode = Marshal.GetLastWin32Error();

					// Win32Exception will contain error message corresponding
					// to the Windows error code.
					throw new Exception("CryptProtectData failed.", new Win32Exception(errCode));
				}

				// return byte array from cipher-text blob
				return BytesFromBlob(ref cipherTextBlob);
			}
			catch (Exception ex)
			{
				throw new Exception("DPAPI was unable to encrypt data.", ex);
			}
			// Free all memory allocated for BLOBs.
			finally
			{
				FreeBlob(ref plainTextBlob);
				FreeBlob(ref cipherTextBlob);
				FreeBlob(ref entropyBlob);
			}
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt cipher-text bytes.
		/// This function does not use additional entropy and does not
		/// return data description.
		/// </summary>
		/// <param name="aCipherText">Encrypted data formatted as a base64-encoded string.</param>
		/// <returns>Decrypted data returned as a UTF-8 string.</returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string aCipherText)
		{
			string description;
			return Decrypt(aCipherText, String.Empty, out description);
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt cipher-text bytes.
		/// This function does not use additional entropy.
		/// </summary>
		/// <param name="aCipherText">Encrypted data formatted as a base64-encoded string.</param>
		/// <param name="aDescription">Returned description of data specified during encryption.</param>
		/// <returns>Decrypted data returned as a UTF-8 string.</returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string aCipherText, out string aDescription)
		{
			return Decrypt(aCipherText, String.Empty, out aDescription);
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt cipher-text bytes.
		/// </summary>
		/// <param name="aCipherText">Encrypted data formatted as a base64-encoded string.</param>
		/// <param name="aEntropy">Optional entropy, which is required if it was specified during encryption.</param>
		/// <param name="aDescription">Returned description of data specified during encryption.</param>
		/// <returns>Decrypted data returned as a UTF-8 string.</returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static string Decrypt(string aCipherText, string aEntropy, out string aDescription)
		{
			// Make sure that parameters are valid.
			if (aEntropy == null) aEntropy = String.Empty;

			return Encoding.UTF8.GetString(
				Decrypt(Convert.FromBase64String(aCipherText),
					Encoding.UTF8.GetBytes(aEntropy),
					out aDescription));
		}

		/// <summary>
		/// Calls DPAPI CryptUnprotectData to decrypt cipher-text bytes.
		/// </summary>
		/// <param name="aCipherTextBytes">Encrypted data.</param>
		/// <param name="aEntropyBytes">Optional entropy, which is required if it was specified during encryption.</param>
		/// <param name="aDescription">Returned description of data specified during encryption.</param>
		/// <returns>Decrypted data bytes.</returns>
		/// <remarks>
		/// When decrypting data, it is not necessary to specify which
		/// type of encryption key to use: user-specific or
		/// machine-specific; DPAPI will figure it out by looking at
		/// the signature of encrypted data.
		/// </remarks>
		public static byte[] Decrypt(byte[] aCipherTextBytes, byte[] aEntropyBytes, out string aDescription)
		{
			// Create BLOBs to hold data.
			DATA_BLOB plainTextBlob = new DATA_BLOB();
			DATA_BLOB cipherTextBlob = new DATA_BLOB();
			DATA_BLOB entropyBlob = new DATA_BLOB();

			// We only need prompt structure because it is a required
			// parameter.
			var prompt = new CRYPTPROTECT_PROMPTSTRUCT();
			InitPrompt(ref prompt);

			// Initialize description string.
			aDescription = String.Empty;

			try
			{
				// Convert cipher-text bytes into a BLOB structure.
				InitBlob(aCipherTextBytes, ref cipherTextBlob, "Cannot initialize cipher-text BLOB.");

				// Convert entropy bytes into a BLOB structure.
				InitBlob(aEntropyBytes, ref entropyBlob, "Cannot initialize entropy BLOB.");

				// Disable any types of UI. (Key type is part of the cipher-text so we do not need to specify)
				int flags = CRYPTPROTECT_UI_FORBIDDEN;

				// Call DPAPI to decrypt data.
				bool success = CryptUnprotectData(ref cipherTextBlob,
					ref aDescription,
					ref entropyBlob,
					IntPtr.Zero,
					ref prompt,
					flags,
					ref plainTextBlob);

				// Check the result.
				if (!success)
				{
					// If operation failed, retrieve last Win32 error.
					var errCode = Marshal.GetLastWin32Error();

					// Win32Exception will contain error message corresponding to the Windows error code.
					throw new Exception("CryptUnprotectData failed.", new Win32Exception(errCode));
				}

				return BytesFromBlob(ref plainTextBlob);
			}
			catch (Exception ex)
			{
				throw new Exception("DPAPI was unable to decrypt data.", ex);
			}
			// Free all memory allocated for BLOBs.
			finally
			{
				FreeBlob(ref plainTextBlob);
				FreeBlob(ref cipherTextBlob);
				FreeBlob(ref entropyBlob);
			}
		}
	}
}

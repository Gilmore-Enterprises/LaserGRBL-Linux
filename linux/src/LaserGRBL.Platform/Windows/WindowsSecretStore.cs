using System;
using System.Text;

namespace LaserGRBL.Platform.Windows
{
	/// <summary>Windows DPAPI-backed secret store (same logic as the original DataProtector.cs).</summary>
	public class WindowsSecretStore : ISecretStore
	{
		public string Encrypt(string text)
		{
			if (string.IsNullOrEmpty(text)) return text ?? "";
#if WINDOWS
			try
			{
				var data = Encoding.Unicode.GetBytes(text);
				var encrypted = System.Security.Cryptography.ProtectedData.Protect(
					data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
				return Convert.ToBase64String(encrypted);
			}
			catch { }
#endif
			return text;
		}

		public string Decrypt(string cipher, string fallback)
		{
			if (string.IsNullOrEmpty(cipher)) return cipher ?? fallback;
#if WINDOWS
			try
			{
				var data = Convert.FromBase64String(cipher);
				var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
					data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
				return Encoding.Unicode.GetString(decrypted);
			}
			catch { }
#endif
			return fallback;
		}
	}
}

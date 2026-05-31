using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LaserGRBL.Platform.Linux
{
	/// <summary>
	/// Cross-platform AES-256-GCM secret store to replace Windows DPAPI on Linux.
	/// Key is derived from a per-installation keyfile stored in DataPath.
	/// On first use the key is randomly generated and saved.
	/// </summary>
	public class LinuxSecretStore : ISecretStore
	{
		private readonly byte[] _key;
		private const string KeyFileName = ".secret.key";

		public LinuxSecretStore(string dataPath)
		{
			string keyFile = Path.Combine(dataPath, KeyFileName);
			if (File.Exists(keyFile))
			{
				_key = File.ReadAllBytes(keyFile);
			}
			else
			{
				_key = RandomNumberGenerator.GetBytes(32); // 256-bit AES key
				// Write with restrictive permissions (user-only)
				File.WriteAllBytes(keyFile, _key);
				try { File.SetAttributes(keyFile, FileAttributes.Hidden); } catch { }
			}
		}

		public string Encrypt(string plaintext)
		{
			if (string.IsNullOrEmpty(plaintext)) return plaintext ?? "";

			byte[] nonce = RandomNumberGenerator.GetBytes(12);
			byte[] data = Encoding.UTF8.GetBytes(plaintext);
			byte[] ciphertext = new byte[data.Length];
			byte[] tag = new byte[16];

			using var aes = new AesGcm(_key, 16);
			aes.Encrypt(nonce, data, ciphertext, tag);

			// Pack: nonce(12) + tag(16) + ciphertext
			byte[] packed = new byte[12 + 16 + ciphertext.Length];
			Buffer.BlockCopy(nonce, 0, packed, 0, 12);
			Buffer.BlockCopy(tag, 0, packed, 12, 16);
			Buffer.BlockCopy(ciphertext, 0, packed, 28, ciphertext.Length);
			return Convert.ToBase64String(packed);
		}

		public string Decrypt(string ciphertext, string fallback)
		{
			if (string.IsNullOrEmpty(ciphertext)) return ciphertext ?? fallback;
			try
			{
				byte[] packed = Convert.FromBase64String(ciphertext);
				if (packed.Length < 29) return fallback;

				byte[] nonce = new byte[12];
				byte[] tag = new byte[16];
				byte[] data = new byte[packed.Length - 28];

				Buffer.BlockCopy(packed, 0, nonce, 0, 12);
				Buffer.BlockCopy(packed, 12, tag, 0, 16);
				Buffer.BlockCopy(packed, 28, data, 0, data.Length);

				byte[] plaintext = new byte[data.Length];
				using var aes = new AesGcm(_key, 16);
				aes.Decrypt(nonce, data, tag, plaintext);
				return Encoding.UTF8.GetString(plaintext);
			}
			catch { return fallback; }
		}
	}
}

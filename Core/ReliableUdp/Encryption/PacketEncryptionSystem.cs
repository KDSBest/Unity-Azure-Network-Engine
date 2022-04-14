using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;

namespace ReliableUdp.Encryption
{
	public class PacketEncryptionSystem
	{
		private byte[] certData;
		private Aes aes;
		public bool IsInitialized = false;

		public PacketEncryptionSystem(byte[] certData)
		{
			this.certData = certData;
		}

		public byte[] InitializeAes()
		{
			CreateAes();
			aes.GenerateIV();
			aes.GenerateKey();

			byte[] encAesKey = EncryptKey(aes.Key);
			byte[] encAesIV = EncryptKey(aes.IV);

			byte[] encData = new byte[4 + encAesKey.Length + 4 + encAesIV.Length];
			byte[] keySize = BitConverter.GetBytes(encAesKey.Length);
			byte[] ivSize = BitConverter.GetBytes(encAesIV.Length);

			Array.Copy(keySize, 0, encData, 0, keySize.Length);
			Array.Copy(ivSize, 0, encData, 4, ivSize.Length);
			Array.Copy(encAesKey, 0, encData, keySize.Length + ivSize.Length, encAesKey.Length);
			Array.Copy(encAesIV, 0, encData, keySize.Length + ivSize.Length + encAesKey.Length, encAesIV.Length);

			return encData;
		}

		private void CreateAes()
		{
			aes = AesManaged.Create();
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
		}

		public void ConnectionAccepted()
		{
			this.IsInitialized = true;
		}

		public void InitializeAes(byte[] data)
		{
			CreateAes();

			int keySize = BitConverter.ToInt32(data, 0);
			int ivSize = BitConverter.ToInt32(data, 4);
			byte[] encKey = new byte[keySize];
			byte[] encIV = new byte[ivSize];
			Array.Copy(data, 8, encKey, 0, keySize);
			Array.Copy(data, 8 + keySize, encIV, 0, ivSize);

			aes.Key = DecryptKey(encKey);
			aes.IV = DecryptKey(encIV);

			IsInitialized = true;
		}

		public byte[] EncryptAes(byte[] data)
		{
			ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

			using (MemoryStream msEncrypt = new MemoryStream())
			{
				using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					csEncrypt.Write(data, 0, data.Length);
				}

				byte[] result = msEncrypt.ToArray();

				return result;
			}
		}

		public byte[] DecryptAes(byte[] data)
		{
			ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

			using (MemoryStream msDecrypt = new MemoryStream())
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
				{
					csDecrypt.Write(data, 0, data.Length);
				}

				byte[] result = msDecrypt.ToArray();

				return result;
			}
		}

		public byte[] EncryptKey(byte[] data)
		{
			using (var cert = new X509Certificate2(certData))
			{
				using (RSA rsaPublicKey = cert.GetRSAPublicKey())
				{
					return rsaPublicKey.Encrypt(data, RSAEncryptionPadding.Pkcs1);
				}
			}
		}

		public byte[] DecryptKey(byte[] data)
		{
			using (var cert = new X509Certificate2(certData))
			{
				using (RSA rsaPublicKey = cert.GetRSAPrivateKey())
				{
					return rsaPublicKey.Decrypt(data, RSAEncryptionPadding.Pkcs1);
				}
			}
		}
	}
}

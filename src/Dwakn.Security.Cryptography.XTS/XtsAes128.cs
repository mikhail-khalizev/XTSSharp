using System;
using System.Security.Cryptography;

namespace Dwakn.Security.Cryptography.XTS
{
	public class XtsAes128 : Xts
	{
		private const int KEY_LENGTH = 128;
		private const int KEY_BYTE_LENGTH = KEY_LENGTH/8;

		protected XtsAes128(Func<SymmetricAlgorithm> create, byte[] key1, byte[] key2)
			: base(create, VerifyKey(KEY_LENGTH, key1), VerifyKey(KEY_LENGTH, key2))
		{
		}

		public static Xts Create(byte[] key1, byte[] key2)
		{
			VerifyKey(KEY_LENGTH, key1);
			VerifyKey(KEY_LENGTH, key2);

			return new XtsAes128(Aes.Create, key1, key2);
		}

		public static Xts Create(byte[] key)
		{
			VerifyKey(KEY_LENGTH*2, key);

			byte[] key1 = new byte[KEY_BYTE_LENGTH];
			byte[] key2 = new byte[KEY_BYTE_LENGTH];

			Buffer.BlockCopy(key, 0, key1, 0, KEY_BYTE_LENGTH);
			Buffer.BlockCopy(key, KEY_BYTE_LENGTH, key2, 0, KEY_BYTE_LENGTH);

			return new XtsAes128(Aes.Create, key1, key2);
		}
	}
}
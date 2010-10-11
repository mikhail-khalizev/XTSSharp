using System;
using System.Security.Cryptography;

namespace Dwakn.Security.Cryptography.XTS
{
	public class XtsAes128 : Xts
	{
		private const int KEY_LENGTH = 128;

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
	}
}
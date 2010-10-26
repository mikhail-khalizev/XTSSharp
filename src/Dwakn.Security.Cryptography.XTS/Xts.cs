using System;
using System.Security.Cryptography;

namespace Dwakn.Security.Cryptography.XTS
{
	/// <summary>
	/// Xts.  See <see cref="XtsAes128"/> and <see cref="XtsAes256"/>.
	/// </summary>
	public class Xts
	{
		private readonly SymmetricAlgorithm _key1;
		private readonly SymmetricAlgorithm _key2;

		/// <summary>
		/// Creates a new Xts implementation.
		/// </summary>
		/// <param name="create">Function to create the implementations</param>
		/// <param name="key1">Key 1</param>
		/// <param name="key2">Key 2</param>
		protected Xts(Func<SymmetricAlgorithm> create, byte[] key1, byte[] key2)
		{
			if (create == null)
				throw new ArgumentNullException("create");
			if (key1 == null)
				throw new ArgumentNullException("key1");
			if (key2 == null)
				throw new ArgumentNullException("key2");

			_key1 = create();
			_key2 = create();

			if (key1.Length != key2.Length)
				throw new ArgumentException("Key lengths don't match");

			//set the key sizes
			_key1.KeySize = key1.Length*8;
			_key2.KeySize = key2.Length*8;

			//set the keys
			_key1.Key = key1;
			_key2.Key = key2;

			//ecb mode
			_key1.Mode = CipherMode.ECB;
			_key2.Mode = CipherMode.ECB;

			//no padding - we're always going to be writing full blocks
			_key1.Padding = PaddingMode.None;
			_key2.Padding = PaddingMode.None;

			//fixed block size of 128 bits.
			_key1.BlockSize = 16*8;
			_key2.BlockSize = 16*8;
		}

		/// <summary>
		/// Creates an xts encryptor
		/// </summary>
		public XtsCryptoTransform CreateEncryptor()
		{
			return new XtsCryptoTransform(_key1.CreateEncryptor(), _key2.CreateEncryptor(), false);
		}

		/// <summary>
		/// Creates an xts decryptor
		/// </summary>
		public XtsCryptoTransform CreateDecryptor()
		{
			return new XtsCryptoTransform(_key1.CreateDecryptor(), _key2.CreateEncryptor(), true);
		}

		/// <summary>
		/// Verify that the key is of an expected size of bits
		/// </summary>
		/// <param name="expectedSize">Expected size of the key in bits</param>
		/// <param name="key">The key</param>
		/// <returns>The key</returns>
		/// <exception cref="ArgumentNullException">If the key is null</exception>
		/// <exception cref="ArgumentException">If the key length does not match the expected length</exception>
		protected static byte[] VerifyKey(int expectedSize, byte[] key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (key.Length*8 != expectedSize)
				throw new ArgumentException(string.Format("Expected key length of {0} bits, got {1}", expectedSize, key.Length*8));

			return key;
		}
	}
}
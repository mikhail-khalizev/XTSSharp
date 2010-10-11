using System.Collections.Generic;
using System.Text;

namespace Dwakn.Security.Cryptography.XTS.Test
{
	internal static class Extensions
	{
		/// <summary>
		/// Hex lookup
		/// </summary>
		private static readonly char[] HexLookup = new[]
		                                           	{
		                                           		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e'
		                                           		, 'f'
		                                           	};

		/// <summary>
		/// Converts a byte array to a hex string
		/// </summary>
		/// <param name="bytes">Bytes to convert</param>
		/// <returns>Hex representation of the byte array</returns>
		public static string ToHex(this byte[] bytes)
		{
			var buffer = new StringBuilder(bytes.Length*2);
			int length = bytes.Length;
			for (int i = 0; i < length; i++)
			{
				buffer.Append(HexLookup[bytes[i] >> 4]);
				buffer.Append(HexLookup[bytes[i] & 15]);
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Converts a byte array to a hex string
		/// </summary>
		/// <param name="bytes">Bytes to convert</param>
		/// <returns>Hex representation of the byte array</returns>
		public static string ToHex(this IEnumerable<byte> bytes)
		{
			var buffer = new StringBuilder();

			foreach (var b in bytes)
			{
				buffer.Append(HexLookup[b >> 4]);
				buffer.Append(HexLookup[b & 15]);
			}

			return buffer.ToString();
		}
	}
}
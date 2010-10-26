using System.IO;

namespace Dwakn.Security.Cryptography.XTS
{
	/// <summary>
	/// A random access, xts encrypted stream
	/// </summary>
	public class XtsStream : RandomAccessSectorStream
	{
		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">Xts implementation to use</param>
		public XtsStream(Stream baseStream, Xts xts)
			: this(baseStream, xts, XtsSectorStream.DEFAULT_SECTOR_SIZE)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="baseStream">The base stream</param>
		/// <param name="xts">Xts implementation to use</param>
		/// <param name="sectorSize">Sector size</param>
		public XtsStream(Stream baseStream, Xts xts, int sectorSize)
			: base(new XtsSectorStream(baseStream, xts, sectorSize), true)
		{
		}
	}
}
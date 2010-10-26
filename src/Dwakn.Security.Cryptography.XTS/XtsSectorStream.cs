using System.IO;

namespace Dwakn.Security.Cryptography.XTS
{
	public class XtsSectorStream : BaseSectorStream
	{
		public const int DEFAULT_SECTOR_SIZE = 512;

		private readonly byte[] _tempBuffer;
		private readonly Xts _xts;
		private XtsCryptoTransform _decryptor;
		private XtsCryptoTransform _encryptor;

		public XtsSectorStream(Stream baseStream, Xts xts)
			: this(baseStream, DEFAULT_SECTOR_SIZE, xts)
		{
		}

		public XtsSectorStream(Stream baseStream, int sectorSize, Xts xts)
			: base(baseStream, sectorSize)
		{
			_xts = xts;

			_tempBuffer = new byte[sectorSize];
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_encryptor != null)
				_encryptor.Dispose();

			if (_decryptor != null)
				_decryptor.Dispose();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			if (count == 0)
				return;

			var currentSector = CurrentSector;

			if (_encryptor == null)
				_encryptor = _xts.CreateEncryptor();

			int transformedCount = _encryptor.TransformBlock(buffer, offset, count, _tempBuffer, 0, currentSector);

			base.Write(_tempBuffer, 0, transformedCount);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			var currentSector = CurrentSector;

			var ret = base.Read(_tempBuffer, 0, count);

			if (ret == 0)
				return 0;

			if (_decryptor == null)
				_decryptor = _xts.CreateDecryptor();

			return _decryptor.TransformBlock(_tempBuffer, 0, ret, buffer, offset, currentSector);
		}
	}
}
using System;
using System.IO;

namespace Dwakn.Security.Cryptography.XTS
{
	public class BaseSectorStream : Stream
	{
		private readonly Stream _baseStream;
		private ulong _currentSector;

		public BaseSectorStream(Stream baseStream, int sectorSize)
		{
			SectorSize = sectorSize;
			_baseStream = baseStream;
		}

		public int SectorSize { get; private set; }

		public override bool CanRead
		{
			get { return _baseStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _baseStream.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _baseStream.CanWrite; }
		}

		public override long Length
		{
			get { return _baseStream.Length; }
		}

		public override long Position
		{
			get { return _baseStream.Position; }
			set
			{
				ValidateSizeMultiple(value);

				_baseStream.Position = value;
				_currentSector = (ulong) (value/SectorSize);
			}
		}

		protected ulong CurrentSector
		{
			get { return _currentSector; }
		}

		private void ValidateSizeMultiple(long value)
		{
			if (value%SectorSize != 0)
				throw new ArgumentException(string.Format("Value needs to be a multiple of {0}", SectorSize));
		}

		protected void ValidateSize(long value)
		{
			if (value != SectorSize)
				throw new ArgumentException(string.Format("Value needs to be {0}", SectorSize));
		}

		protected void ValidateSize(int value)
		{
			if (value != SectorSize)
				throw new ArgumentException(string.Format("Value needs to be {0}", SectorSize));
		}

		public override void Flush()
		{
			_baseStream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long newPosition;
			switch (origin)
			{
				case SeekOrigin.Begin:
					newPosition = offset;
					break;
				case SeekOrigin.End:
					newPosition = Length - offset;
					break;
				default:
					newPosition = Position + offset;
					break;
			}

			Position = newPosition;

			return newPosition;
		}

		public override void SetLength(long value)
		{
			ValidateSizeMultiple(value);

			_baseStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			var ret = _baseStream.Read(buffer, offset, count);
			_currentSector++;
			return ret;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			ValidateSize(count);

			_baseStream.Write(buffer, offset, count);
			_currentSector++;
		}
	}
}
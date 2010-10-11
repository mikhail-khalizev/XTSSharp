using System;
using System.IO;

namespace Dwakn.Security.Cryptography.XTS
{
	public class ReadWriteSectorStream : Stream
	{
		private readonly byte[] _buffer;
		private readonly int _bufferSize;
		private readonly BaseSectorStream _s;
		private bool _bufferDirty;
		private bool _bufferLoaded;
		private int _bufferPos;

		public ReadWriteSectorStream(BaseSectorStream s)
		{
			_s = s;
			_buffer = new byte[s.SectorSize];
			_bufferSize = s.SectorSize;
		}

		public override bool CanRead
		{
			get { return _s.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _s.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return _s.CanWrite; }
		}

		public override long Length
		{
			get { return _s.Length + _bufferPos; }
		}

		public override long Position
		{
			get { return _bufferLoaded ? (_s.Position - _bufferSize + _bufferPos) : _s.Position + _bufferPos; }
			set
			{
				if (value < 0L)
					throw new ArgumentOutOfRangeException("value");

				var sectorPosition = (value%_bufferSize);
				var position = value - sectorPosition;

				//see if its within the current sector
				if (_bufferLoaded)
				{
					var basePosition = _s.Position - _bufferSize;
					if (value > basePosition && value < basePosition + _bufferSize)
					{
						_bufferPos = (int) sectorPosition;
						return;
					}
				}
				//outside the current buffer

				//write it
				if (_bufferDirty)
					WriteSector();

				_s.Position = position;

				//read this sector
				ReadSector();

				//bump us forward if need be
				_bufferPos = (int) sectorPosition;
			}
		}

		protected override void Dispose(bool disposing)
		{
			Flush();

			base.Dispose(disposing);
		}

		public override void Flush()
		{
			if (_bufferDirty)
				WriteSector();
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
			var remainder = value%_s.SectorSize;

			if (remainder > 0)
			{
				value = (value - remainder) + _bufferSize;
			}

			_s.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (Position + count > _s.Length)
				throw new IndexOutOfRangeException("Attempt to read beyond end of stream");

			if (!_bufferLoaded)
				ReadSector();

			var totalBytesRead = 0;
			while (count > 0)
			{
				var bytesToRead = Math.Min(count, _bufferSize - _bufferPos);

				Buffer.BlockCopy(_buffer, _bufferPos, buffer, offset, bytesToRead);

				offset += bytesToRead;
				_bufferPos += bytesToRead;
				count -= bytesToRead;

				totalBytesRead += bytesToRead;

				if (_bufferPos == _bufferSize)
					ReadSector();
			}

			return totalBytesRead;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				if (!_bufferLoaded)
					ReadSector();

				var bytesToWrite = Math.Min(count, _bufferSize - _bufferPos);

				Buffer.BlockCopy(buffer, offset, _buffer, _bufferPos, bytesToWrite);

				offset += bytesToWrite;
				_bufferPos += bytesToWrite;
				count -= bytesToWrite;
				_bufferDirty = true;

				if (_bufferPos == _bufferSize)
					WriteSector();
			}
		}

		private void ReadSector()
		{
			if (_s.Position == _s.Length)
				return;

			var bytesRead = _s.Read(_buffer, 0, _buffer.Length);
			Array.Clear(_buffer, bytesRead, _buffer.Length - bytesRead);

			_bufferLoaded = true;
			_bufferPos = 0;

			_bufferDirty = false;
		}

		private void WriteSector()
		{
			if (_bufferLoaded)
			{
				//go back to beginning of the current sector
				_s.Seek(-_bufferSize, SeekOrigin.Current);
			}

			//clean the end of it
			if (_bufferPos != _bufferSize)
				Array.Clear(_buffer, _bufferPos, _bufferSize - _bufferPos);

			//write it
			_s.Write(_buffer, 0, _bufferSize);
			_bufferDirty = false;
			_bufferLoaded = false;
			_bufferPos = 0;

			_bufferDirty = false;
		}
	}
}
﻿using System;
using System.IO;

namespace Dwakn.Security.Cryptography.XTS
{
	/// <summary>
	/// A wraps a sector based stream and provides random access to it
	/// </summary>
	public class RandomAccessSectorStream : Stream
	{
		private readonly byte[] _buffer;
		private readonly int _bufferSize;
		private readonly SectorStream _s;
		private readonly bool _isStreamOwned;
		private bool _bufferDirty;
		private bool _bufferLoaded;
		private int _bufferPos;

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="s">Base stream</param>
		public RandomAccessSectorStream(SectorStream s)
			: this(s, false)
		{
		}

		/// <summary>
		/// Creates a new stream
		/// </summary>
		/// <param name="s">Base stream</param>
		/// <param name="isStreamOwned">Does this stream own the base stream? i.e. should it be automatically disposed?</param>
		public RandomAccessSectorStream(SectorStream s, bool isStreamOwned)
		{
			_s = s;
			_isStreamOwned = isStreamOwned;
			_buffer = new byte[s.SectorSize];
			_bufferSize = s.SectorSize;
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get { return _s.CanRead; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get { return _s.CanSeek; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get { return _s.CanWrite; }
		}

		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		public override long Length
		{
			get { return _s.Length + _bufferPos; }
		}

		/// <summary>
		/// Gets or sets the position within the current stream.
		/// </summary>
		/// <returns>The current position within the stream.</returns>
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

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
		protected override void Dispose(bool disposing)
		{
			Flush();

			base.Dispose(disposing);

			if (_isStreamOwned)
				_s.Dispose();
		}

		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		public override void Flush()
		{
			if (_bufferDirty)
				WriteSector();
		}

		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
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

		/// <summary>
		/// Sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		public override void SetLength(long value)
		{
			var remainder = value%_s.SectorSize;

			if (remainder > 0)
			{
				value = (value - remainder) + _bufferSize;
			}

			_s.SetLength(value);
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
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

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
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

		/// <summary>
		/// Reads a sector
		/// </summary>
		private void ReadSector()
		{
			if (_bufferLoaded && _bufferDirty)
			{
				WriteSector();
			}

			if (_s.Position == _s.Length)
			{
				return;
			}

			var bytesRead = _s.Read(_buffer, 0, _buffer.Length);

			//clean the end of it
			if (bytesRead != _bufferSize)
				Array.Clear(_buffer, bytesRead, _buffer.Length - bytesRead);

			_bufferLoaded = true;
			_bufferPos = 0;
			_bufferDirty = false;
		}

		/// <summary>
		/// Writes a sector
		/// </summary>
		private void WriteSector()
		{
			if (_bufferLoaded)
			{
				//go back to beginning of the current sector
				_s.Seek(-_bufferSize, SeekOrigin.Current);
			}

			//write it
			_s.Write(_buffer, 0, _bufferSize);
			_bufferDirty = false;
			_bufferLoaded = false;
			_bufferPos = 0;
			Array.Clear(_buffer, 0, _bufferSize);
		}
	}
}
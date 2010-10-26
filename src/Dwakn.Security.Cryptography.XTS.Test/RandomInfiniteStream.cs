using System;
using System.IO;

namespace Dwakn.Security.Cryptography.XTS.Test
{
	class RandomInfiniteStream : Stream
	{
		private readonly Random _random;

		public RandomInfiniteStream()
		{
			_random = new Random();
		}

		public RandomInfiniteStream(int seed)
		{
			_random = new Random(seed);
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override long Length
		{
			get { throw new NotImplementedException(); }
		}

		public override long Position
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (offset != 0 && count != buffer.Length)
			{
				//bleh
				var bleh = new byte[count];
				_random.NextBytes(bleh);

				Buffer.BlockCopy(bleh, 0, buffer, offset, count);
			}
			else
			{
				_random.NextBytes(buffer);
			}

			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}
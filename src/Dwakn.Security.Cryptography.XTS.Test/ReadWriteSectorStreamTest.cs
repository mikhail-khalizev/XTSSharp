using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Dwakn.Security.Cryptography.XTS.Test
{
	[TestFixture]
	public class ReadWriteSectorStreamTest
	{
		[Test]
		public void Can_read()
		{
			var random = new Random(1);

			var b = new byte[1024*2];
			random.NextBytes(b);

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					foreach (var t in b)
					{
						var r = new byte[1];
						stream.Read(r, 0, 1);

						Assert.AreEqual(t, r[0]);
					}
				}
			}
		}

		[Test]
		public void Can_read_large()
		{
			var random = new Random(1);

			var b = new byte[1024*2];
			random.NextBytes(b);

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					var r = new byte[b.Length - 512];
					stream.Read(r, 0, r.Length);

					Assert.AreEqual(b.Take(b.Length - 512).ToArray(), r);
				}
			}
		}

		[Test]
		public void Can_write()
		{
			var random = new Random(1);

			var b = new byte[1024*2];
			random.NextBytes(b);

			using (var s = new MemoryStream())
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{/*
					stream.Write(b, 0, 512);
					stream.Write(b, 512, 512);
					stream.Write(b, 1024, 512);
					stream.Write(b, 1536, 512);*/
					
					for (var x = 0; x < b.Length; x++)
					{
						stream.Write(b, x, 1);
					}
				}

				Assert.AreEqual(b.Length, s.Length);
				Assert.AreEqual(b, s.ToArray());
			}
		}

		[Test]
		public void Can_write_large()
		{
			var random = new Random(1);

			var b = new byte[1024*3];
			random.NextBytes(b);

			using (var s = new MemoryStream())
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					stream.Write(b, 0, b.Length/2);
					stream.Write(b, b.Length/2, b.Length/2);
				}

				Assert.AreEqual(b.Length, s.Length);
				Assert.AreEqual(b, s.ToArray());
			}
		}

		[Test]
		public void Can_write_partial()
		{
			var random = new Random(1);

			var b = new byte[(1024*2) - 10];
			random.NextBytes(b);

			using (var s = new MemoryStream())
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					for (var x = 0; x < b.Length; x++)
					{
						stream.Write(b, x, 1);
					}
				}

				var ending = s.ToArray().Skip(b.Length).ToArray();

				Assert.AreEqual(b.Length, s.Length - 10);
				Assert.AreEqual(b, s.ToArray().Take(b.Length).ToArray());
				Assert.IsTrue(s.ToArray().Skip(b.Length).All(x => x == 0));
			}
		}

		[Test]
		public void Can_seek_and_read()
		{
			var random = new Random(1);

			var b = new byte[(1024 * 2)];
			random.NextBytes(b);

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					stream.Seek(1024 + 512, SeekOrigin.Begin);

					var r = new byte[256];
					stream.Read(r, 0, r.Length);

					Assert.AreEqual(b.Skip(1024 + 512).Take(r.Length).ToArray(), r);
				}
			}
		}

		[Test]
		public void Can_seek_and_write()
		{
			var random = new Random(1);

			var b = new byte[(1024 * 2)];
			random.NextBytes(b);

			var r = new byte[256];

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					stream.Seek(1024 + 512, SeekOrigin.Begin);

					stream.Write(r, 0, r.Length);
				}

				Assert.AreEqual(r, s.ToArray().Skip(1024 + 512).Take(r.Length).ToArray());
			}
		}

		[Test]
		public void Can_seek_read_write_seek_and_read()
		{
			var random = new Random(1);

			var b = new byte[(1024 * 3)];
			random.NextBytes(b);

			var empty = new byte[256];
			var read = new byte[256];

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new BaseSectorStream(s, 1024))
				using (var stream = new ReadWriteSectorStream(sectorS))
				{
					stream.Seek(1024 + 512, SeekOrigin.Begin);

					stream.Write(empty, 0, empty.Length);
					stream.Position = 512;
					stream.Read(read, 0, read.Length);
					stream.Write(empty, 0, empty.Length);
				}

				Assert.AreEqual(empty, s.ToArray().Skip(1024 + 512).Take(empty.Length).ToArray());
				Assert.AreEqual(b.Skip(512).Take(read.Length).ToArray(), read);
				Assert.AreEqual(empty, b.Skip(512 + read.Length).Take(read.Length).ToArray());
			}
		}
	}
}
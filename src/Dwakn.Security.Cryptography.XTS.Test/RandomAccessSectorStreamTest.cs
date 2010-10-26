using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Dwakn.Security.Cryptography.XTS.Test
{
	[TestFixture]
	public class RandomAccessSectorStreamTest
	{
		[Test]
		public void Can_read()
		{
			var random = new Random(1);

			var b = new byte[1024*2];
			random.NextBytes(b);

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
				{
					/*
					stream.Write(b, 0, 512);
					stream.Write(b, 512, 512);
					stream.Write(b, 1024, 512);
					stream.Write(b, 1536, 512);
					*/
					
					for (var x = 0; x < b.Length; x++)
					{
						stream.Write(b, x, 1);
					}
				}

				Console.WriteLine(b.ToHex());
				Console.WriteLine(s.ToArray().ToHex());


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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
				{
					for (var x = 0; x < b.Length; x++)
					{
						stream.Write(b, x, 1);
					}

					Console.WriteLine("");
				}

				var ending = s.ToArray().Skip(b.Length).ToArray();

				Assert.AreEqual(b.Length, s.Length - 10);
				Assert.AreEqual(b, s.ToArray().Take(b.Length).ToArray());
				Assert.IsTrue(s.ToArray().Skip(b.Length).All(x => x == 0), s.ToArray().Skip(b.Length).ToHex());
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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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

			var b = new byte[(1024*3)];
			random.NextBytes(b);

			var empty = new byte[256];
			var read = new byte[256];

			using (var s = new MemoryStream(b))
			{
				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
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


		[Test]
		public void Can_random_read_write()
		{
			var random = new Random(1);
			var data = new byte[1024*10];
			random.NextBytes(data);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					for (int x = 0; x < 10; x++)
					{
						var isWrite = random.Next(0, 2) > 0;
						var index = random.Next(0, data.Length - 1);
						var bytes = random.Next(0, Math.Min(1024, data.Length - 1 - index));

						if (isWrite)
						{
							stream.Position = index;
							var r = new byte[bytes];
							random.NextBytes(r);
							Buffer.BlockCopy(r, 0, data, index, bytes);

							stream.Write(r, 0, r.Length);
						}

						{
							stream.Position = index;
							//read some data
							var r = new byte[bytes];
							stream.Read(r, 0, bytes);

							try
							{
								Assert.AreEqual(data.Skip(index).Take(bytes).ToArray(), r);
							}
							catch (Exception)
							{
								Console.WriteLine(data.Skip(index).Take(bytes).ToHex());
								Console.WriteLine(r.ToHex());

								throw;
							}
						}
					}
				}
			}
		}

		[Test]
		public void Flushes_partial_sector_when_reading()
		{
			var random = new Random(1);

			var b = new byte[1000];
			random.NextBytes(b);

			using (var s = new MemoryStream())
			{
				using (var sectorS = new SectorStream(s, 1024))
				{
					sectorS.Write(new byte[1024], 0, 1024);
					sectorS.Write(new byte[1024], 0, 1024);
				}

				s.Seek(0, SeekOrigin.Begin);

				using (var sectorS = new SectorStream(s, 1024))
				using (var stream = new RandomAccessSectorStream(sectorS))
				{
					for (var x = 0; x < b.Length; x++)
					{
						stream.Write(b, x, 1);
					}

					var data = new byte[200];
					stream.Read(data, 0, data.Length);
				}

				try
				{
					Assert.AreEqual(b, s.ToArray().Take(b.Length).ToArray());
				}
				catch (Exception)
				{
					Console.WriteLine(b.ToHex());
					Console.WriteLine(s.ToArray().Take(b.Length).ToHex());
					
					throw;
				}
			}
		}
	}
}
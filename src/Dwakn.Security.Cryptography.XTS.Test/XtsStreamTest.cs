﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Dwakn.Security.Cryptography.XTS.Test
{
	[TestFixture]
	public class XtsStreamTest
	{
		[Test]
		public void Direct_read_write()
		{
			var b = new byte[1024];

			var key1 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var key2 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				{
					xtsStream.Write(b, 0, b.Length);
				}
				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				{
					var temp = new byte[b.Length];
					xtsStream.Read(temp, 0, temp.Length);

					Assert.IsTrue(temp.All(x => x == 0), temp.ToHex());
				}
			}
		}

		/// <summary>
		/// Tests for memory leaks
		/// </summary>
		[Test]
		[Ignore("Used for memory leak testing")]
		public void Long_running_write()
		{
			var key1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var key2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var xts = XtsAes128.Create(key1, key2);

			var data = new byte[1024];

			using (var nullStream = Stream.Null)
			using (var randomInfiniteStream = new RandomInfiniteStream())
			{
				using (var xtsStream = new XtsSectorStream(nullStream, 1024, xts))
				{
					while (true)
					{
						randomInfiniteStream.Read(data, 0, data.Length);
						xtsStream.Write(data, 0, data.Length);
					}
				}
			}
		}

		[Test]
		public void Rws_random_read()
		{
			var random = new Random(1);
			var data = new byte[1024*10];
			random.NextBytes(data);

			var key1 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var key2 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					for (int x = 0; x < 1000; x++)
					{
						var index = random.Next(0, data.Length - 1);
						var bytes = random.Next(0, Math.Min(1024, data.Length - 1 - index));
						stream.Position = index;

						//read some data
						var r = new byte[bytes];
						stream.Read(r, 0, bytes);

						Assert.AreEqual(data.Skip(index).Take(bytes).ToArray(), r);
					}
				}
			}
		}

		[Test]
		public void Rws_random_read_write()
		{
			const int sectorSize = 1024;

			var random = new Random(1);
			var data = new byte[sectorSize * 10];
			random.NextBytes(data);

			var key1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var key2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, sectorSize, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, sectorSize, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					for (int x = 0; x < 100; x++)
					{
						var isWrite = random.Next(0, 2) > 0;
						var index = random.Next(0, data.Length - 1);
						var bytes = random.Next(0, Math.Min(sectorSize, data.Length - 1 - index));
						
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

							var reference = data.Skip(index).Take(bytes).ToArray();

							try
							{
								Assert.AreEqual(reference, r);
							}
							catch (Exception)
							{
								Console.WriteLine(reference.ToHex());
								Console.WriteLine(r.ToHex());
								
								throw;
							}
						}
					}
				}
			}
		}

		[Test]
		public void Rws_read_seek_write()
		{
			var b = new byte[1024*1024];

			var key1 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var key2 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Seek((10*1024) + 5, SeekOrigin.Begin);

					var temp = new byte[2048 + 4];
					stream.Read(temp, 0, temp.Length);

					Assert.IsTrue(temp.All(x => x == 0), temp.ToHex());
				}
			}
		}

		[Test]
		public void Rws_read_write()
		{
			var b = new byte[1024];

			var key1 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var key2 = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new RandomAccessSectorStream(xtsStream))
				{
					//stream.Seek((10 * 1024) + 5, SeekOrigin.Begin);

					var temp = new byte[b.Length];
					stream.Read(temp, 0, temp.Length);

					Assert.IsTrue(temp.All(x => x == 0), temp.ToHex());
				}
			}
		}
	}
}
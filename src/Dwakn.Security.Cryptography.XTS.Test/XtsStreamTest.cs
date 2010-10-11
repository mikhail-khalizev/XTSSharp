using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

			var key1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var key2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
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

		[Test]
		public void Rws_read_write()
		{
			var b = new byte[1024];

			var key1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var key2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var xts = XtsAes128.Create(key1, key2);

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					//stream.Seek((10 * 1024) + 5, SeekOrigin.Begin);

					var temp = new byte[b.Length];
					stream.Read(temp, 0, temp.Length);

					Assert.IsTrue(temp.All(x => x == 0), temp.ToHex());
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
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);
				
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					stream.Seek((10*1024) + 5, SeekOrigin.Begin);

					var temp = new byte[2048 + 4];
					stream.Read(temp, 0, temp.Length);

					Assert.IsTrue(temp.All(x => x == 0), temp.ToHex());
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
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);
				Console.WriteLine("-------------------------------------------");
				
				using (var xtsStream = new XtsSectorStream(s, 1024, xts))
				using (var stream = new ReadWriteSectorStream(xtsStream))
				{
					stream.Position = 7716;
					
					byte[] t = new byte[956];
					stream.Read(t, 0, t.Length);
					
					Console.WriteLine("-------------------------------------------");

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
	}
}

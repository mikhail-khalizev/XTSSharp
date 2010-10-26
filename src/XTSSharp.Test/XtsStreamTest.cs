// Copyright (c) 2010 Gareth Lennox (garethl@dwakn.com)
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:

//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in the documentation
//       and/or other materials provided with the distribution.
//     * Neither the name of Gareth Lennox nor the names of its
//       contributors may be used to endorse or promote products derived from this
//       software without specific prior written permission.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace XTSSharp.Test
{
	[TestFixture]
	public abstract class XtsStreamTest
	{
		protected abstract Xts Create();

		[Test]
		public void Direct_read_write()
		{
			var b = new byte[1024];

			var xts = Create();

			using (var s = new MemoryStream())
			{
				using (var xtsStream = new XtsSectorStream(s, xts, 1024))
				{
					xtsStream.Write(b, 0, b.Length);
				}
				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var xtsStream = new XtsSectorStream(s, xts, 1024))
				{
					var temp = new byte[b.Length];
					xtsStream.Read(temp, 0, temp.Length);

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

			var xts = Create();

			using (var s = new MemoryStream())
			{
				using (var stream = new XtsStream(s, xts, 1024))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var stream = new XtsStream(s, xts, 1024))
				{
					for (int x = 0; x < 100; x++)
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

			var xts = Create();

			using (var s = new MemoryStream())
			{
				using (var stream = new XtsStream(s, xts, 1024))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.AreEqual(data.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var stream = new XtsStream(s, xts, 1024))
				{
					for (int x = 0; x < 20; x++)
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

			var xts = Create();

			using (var s = new MemoryStream())
			{
				using (var stream = new XtsStream(s, xts, 1024))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var stream = new XtsStream(s, xts, 1024))
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

			var xts = Create();

			using (var s = new MemoryStream())
			{
				using (var stream = new XtsStream(s, xts, 1024))
				{
					stream.Write(b, 0, b.Length);
				}

				Assert.AreEqual(b.Length, s.Length);

				s.Seek(0, SeekOrigin.Begin);

				using (var stream = new XtsStream(s, xts, 1024))
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
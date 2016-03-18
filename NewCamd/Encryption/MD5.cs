//
// MD5.cs: MD5 Encryption algorithm implementation in managed code.
//

using System;
using System.Text;

namespace NewCamd.Encryption
{
	/// <summary>
	/// MD5 Hash generator
	/// Ported to C# using:
	///    http://theory.lcs.mit.edu/~rivest/md5.c
	///    lib/libcrypt/crypt-md5.c
	/// </summary>
	public class MD5
	{
		private const int S11 = 7;
		private const int S12 = 12;
		private const int S13 = 17;
		private const int S14 = 22;
		private const int S21 = 5;
		private const int S22 = 9;
		private const int S23 = 14;
		private const int S24 = 20;
		private const int S31 = 4;
		private const int S32 = 11;
		private const int S33 = 16;
		private const int S34 = 23;
		private const int S41 = 6;
		private const int S42 = 10;
		private const int S43 = 15;
		private const int S44 = 21;
		private byte[] itoa64 = {
			(byte)'.', (byte)'/', (byte)'0', (byte)'1', (byte)'2', (byte)'3',
			(byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9',
			(byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F',
			(byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L',
			(byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'Q', (byte)'R',
			(byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X',
			(byte)'Y', (byte)'Z', (byte)'a', (byte)'b', (byte)'c', (byte)'d',
			(byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j',
			(byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p',
			(byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v',
			(byte)'w', (byte)'x', (byte)'y', (byte)'z' };
		private byte[] PADDING = new byte[64]{
				0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
			};
		public class MDcontext {
			public uint[] i = new uint[2];
			public uint[] buf = new uint[4];
			public byte[] _in = new byte[64];
			public byte[] digest = new byte[16];
		}
		public void MD5Init(ref MDcontext MD)
		{
			MD.i[0] = MD.i[0] = (uint)0;
			MD.buf[0] = (uint)0x67452301;
			MD.buf[1] = (uint)0xefcdab89;
			MD.buf[2] = (uint)0x98badcfe;
			MD.buf[3] = (uint)0x10325476;
		}
		public void MD5Update(ref MDcontext MD, byte[] inBuf, uint inLen)
		{
			uint[] _in = new uint[16];
			int mdi = (int)((MD.i[0] >> 3) & 0x3F);
			uint j;
			if ((MD.i[0] + ((uint)inLen << 3)) < MD.i[0])
				MD.i[1]++;
			MD.i[0] += ((uint)inLen << 3);
			MD.i[1] += ((uint)inLen >> 29);
			j = 0;
			while (Convert.ToBoolean(inLen--)) {
				MD._in[mdi++] = inBuf[j++];
				if (mdi == 0x40) {
					for (int i = 0, ii = 0 ; i < 16 ; i++, ii += 4)
						_in[i] = 	(((uint)MD._in[ii + 3]) << 24) |
									(((uint)MD._in[ii + 2]) << 16) |
									(((uint)MD._in[ii + 1]) << 8) |
									((uint)MD._in[ii]);
					Transform(ref MD.buf, _in);
					mdi = 0;
				}
			}
		}
		public void MD5Final(ref MDcontext MD)
		{
			uint[] _in = new uint[16];
			int mdi = (int)((MD.i[0] >> 3) & 0x3F);
			uint padLen;
			_in[14] = MD.i[0];
			_in[15] = MD.i[1];
			padLen = (uint)((mdi < 56) ? (56 - mdi) : (120 - mdi));
			MD5Update(ref MD, PADDING, padLen);
			for (uint i = 0, ii = 0 ; i < 14 ; i++, ii += 4)
				_in[i] =	(((uint)MD._in[ii + 3]) << 24) |
							(((uint)MD._in[ii + 2]) << 16) |
							(((uint)MD._in[ii + 1]) << 8) |
							((uint)MD._in[ii]);
			Transform(ref MD.buf, _in);
			for (uint i = 0, ii = 0 ; i < 4 ; i++, ii += 4) {
				MD.digest[ii] = (byte)(MD.buf[i] & 0xFF);
				MD.digest[ii + 1] = (byte)((MD.buf[i] >> 8) & 0xFF);
				MD.digest[ii + 2] = (byte)((MD.buf[i] >> 16) & 0xFF);
				MD.digest[ii + 3] = (byte)((MD.buf[i] >> 24) & 0xFF);
			}
		}
		public void FF(ref uint a, uint b, uint c, uint d, uint x_ac, int s)
		{
			a += ((b & c) | ((~b) & d)) + x_ac;
			a =  ((a << s) | (a >> (32 - s)));
			a += b;
		}
		public void GG(ref uint a, uint b, uint c, uint d, uint x_ac, int s)
		{
			a += ((b & d) | (c & (~d))) + x_ac;
			a =  ((a << s) | (a >> (32 - s)));
			a += b;
		}
		public void HH(ref uint a, uint b, uint c, uint d, uint x_ac, int s)
		{
			a += (b ^ c ^ d) + x_ac;
			a =  ((a << s) | (a >> (32 - s)));
			a += b;
		}
		public void II(ref uint a, uint b, uint c, uint d, uint x_ac, int s)
		{
			a += (c ^ (b | (~d))) + x_ac;
			a =  ((a << s) | (a >> (32 - s)));
			a += b;
		}
		public void Transform(ref uint[] buf, uint[] _in)
		{
			uint a = buf[0], b = buf[1], c = buf[2], d = buf[3];
			FF ( ref a, b, c, d, (uint)_in[ 0] + 0xd76aa478, S11);
			FF ( ref d, a, b, c, (uint)_in[ 1] + 0xe8c7b756, S12);
			FF ( ref c, d, a, b, (uint)_in[ 2] + 0x242070db, S13);
			FF ( ref b, c, d, a, (uint)_in[ 3] + 0xc1bdceee, S14);
			FF ( ref a, b, c, d, (uint)_in[ 4] + 0xf57c0faf, S11);
			FF ( ref d, a, b, c, (uint)_in[ 5] + 0x4787c62a, S12);
			FF ( ref c, d, a, b, (uint)_in[ 6] + 0xa8304613, S13);
			FF ( ref b, c, d, a, (uint)_in[ 7] + 0xfd469501, S14);
			FF ( ref a, b, c, d, (uint)_in[ 8] + 0x698098d8, S11);
			FF ( ref d, a, b, c, (uint)_in[ 9] + 0x8b44f7af, S12);
			FF ( ref c, d, a, b, (uint)_in[10] + 0xffff5bb1, S13);
			FF ( ref b, c, d, a, (uint)_in[11] + 0x895cd7be, S14);
			FF ( ref a, b, c, d, (uint)_in[12] + 0x6b901122, S11);
			FF ( ref d, a, b, c, (uint)_in[13] + 0xfd987193, S12);
			FF ( ref c, d, a, b, (uint)_in[14] + 0xa679438e, S13);
			FF ( ref b, c, d, a, (uint)_in[15] + 0x49b40821, S14);

			GG ( ref a, b, c, d, (uint)_in[ 1] + 0xf61e2562, S21);
			GG ( ref d, a, b, c, (uint)_in[ 6] + 0xc040b340, S22);
			GG ( ref c, d, a, b, (uint)_in[11] + 0x265e5a51, S23);
			GG ( ref b, c, d, a, (uint)_in[ 0] + 0xe9b6c7aa, S24);
			GG ( ref a, b, c, d, (uint)_in[ 5] + 0xd62f105d, S21);
			GG ( ref d, a, b, c, (uint)_in[10] + 0x02441453, S22);
			GG ( ref c, d, a, b, (uint)_in[15] + 0xd8a1e681, S23);
			GG ( ref b, c, d, a, (uint)_in[ 4] + 0xe7d3fbc8, S24);
			GG ( ref a, b, c, d, (uint)_in[ 9] + 0x21e1cde6, S21);
			GG ( ref d, a, b, c, (uint)_in[14] + 0xc33707d6, S22);
			GG ( ref c, d, a, b, (uint)_in[ 3] + 0xf4d50d87, S23);
			GG ( ref b, c, d, a, (uint)_in[ 8] + 0x455a14ed, S24);
			GG ( ref a, b, c, d, (uint)_in[13] + 0xa9e3e905, S21);
			GG ( ref d, a, b, c, (uint)_in[ 2] + 0xfcefa3f8, S22);
			GG ( ref c, d, a, b, (uint)_in[ 7] + 0x676f02d9, S23);
			GG ( ref b, c, d, a, (uint)_in[12] + 0x8d2a4c8a, S24);

			HH ( ref a, b, c, d, (uint)_in[ 5] + 0xfffa3942, S31);
			HH ( ref d, a, b, c, (uint)_in[ 8] + 0x8771f681, S32);
			HH ( ref c, d, a, b, (uint)_in[11] + 0x6d9d6122, S33);
			HH ( ref b, c, d, a, (uint)_in[14] + 0xfde5380c, S34);
			HH ( ref a, b, c, d, (uint)_in[ 1] + 0xa4beea44, S31);
			HH ( ref d, a, b, c, (uint)_in[ 4] + 0x4bdecfa9, S32);
			HH ( ref c, d, a, b, (uint)_in[ 7] + 0xf6bb4b60, S33);
			HH ( ref b, c, d, a, (uint)_in[10] + 0xbebfbc70, S34);
			HH ( ref a, b, c, d, (uint)_in[13] + 0x289b7ec6, S31);
			HH ( ref d, a, b, c, (uint)_in[ 0] + 0xeaa127fa, S32);
			HH ( ref c, d, a, b, (uint)_in[ 3] + 0xd4ef3085, S33);
			HH ( ref b, c, d, a, (uint)_in[ 6] + 0x04881d05, S34);
			HH ( ref a, b, c, d, (uint)_in[ 9] + 0xd9d4d039, S31);
			HH ( ref d, a, b, c, (uint)_in[12] + 0xe6db99e5, S32);
			HH ( ref c, d, a, b, (uint)_in[15] + 0x1fa27cf8, S33);
			HH ( ref b, c, d, a, (uint)_in[ 2] + 0xc4ac5665, S34);

			II ( ref a, b, c, d, (uint)_in[ 0] + 0xf4292244, S41);
			II ( ref d, a, b, c, (uint)_in[ 7] + 0x432aff97, S42);
			II ( ref c, d, a, b, (uint)_in[14] + 0xab9423a7, S43);
			II ( ref b, c, d, a, (uint)_in[ 5] + 0xfc93a039, S44);
			II ( ref a, b, c, d, (uint)_in[12] + 0x655b59c3, S41);
			II ( ref d, a, b, c, (uint)_in[ 3] + 0x8f0ccc92, S42);
			II ( ref c, d, a, b, (uint)_in[10] + 0xffeff47d, S43);
			II ( ref b, c, d, a, (uint)_in[ 1] + 0x85845dd1, S44);
			II ( ref a, b, c, d, (uint)_in[ 8] + 0x6fa87e4f, S41);
			II ( ref d, a, b, c, (uint)_in[15] + 0xfe2ce6e0, S42);
			II ( ref c, d, a, b, (uint)_in[ 6] + 0xa3014314, S43);
			II ( ref b, c, d, a, (uint)_in[13] + 0x4e0811a1, S44);
			II ( ref a, b, c, d, (uint)_in[ 4] + 0xf7537e82, S41);
			II ( ref d, a, b, c, (uint)_in[11] + 0xbd3af235, S42);
			II ( ref c, d, a, b, (uint)_in[ 2] + 0x2ad7d2bb, S43);
			II ( ref b, c, d, a, (uint)_in[ 9] + 0xeb86d391, S44);

			buf[0] += a;
			buf[1] += b;
			buf[2] += c;
			buf[3] += d;
		}
		private string MDToString(byte[] digest)
		{
			string MDstr = "";
			for (int i = 0 ; i < 16 ; i++)
				MDstr += String.Format("{0:x2}", digest[i]);
			return MDstr;
		}
		
		/// <summary>This method encrypt the given string, using MD5 Algorithm</summary>
		/// <param name="s">The string to be encrypted.</param>
		/// <returns>The encrypted password.</returns>
		public string Encrypt(string s)
		{
			MDcontext MDp = new MDcontext();
			MD5Init(ref MDp);
			MD5Update(ref MDp, (new ASCIIEncoding()).GetBytes(s), (uint)s.Length);
			MD5Final(ref MDp);
			return MDToString(MDp.digest);
		}

		/// <summary>This method encrypt the given string with the salt indicated, using MD5 Algorithm</summary>
		/// <param name="pwd">The password to be encrypted.</param>
		/// <param name="salt"> The salt.</param>
		/// <returns>The encrypted password.</returns>
		public string Encrypt(string pwd, string salt)
		{
			string e_pass = "";
			MDcontext MDp1 = new MDcontext();
			MDcontext MDp2 = new MDcontext();
			string magic = "$1$";
			if(salt.Length > 19)
				salt = salt.Substring(0,19);
			MD5Init(ref MDp1);
			MD5Update(ref MDp1, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
			MD5Update(ref MDp1, (new ASCIIEncoding()).GetBytes(magic), (uint)magic.Length);
			MD5Update(ref MDp1, (new ASCIIEncoding()).GetBytes(salt), (uint)salt.Length);
			MD5Init(ref MDp2);
			MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
			MD5Update(ref  MDp2, (new ASCIIEncoding()).GetBytes(salt), (uint)salt.Length);
			MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
			MD5Final(ref MDp2);
			for (int j = 0 ; j < 16 ; j++)
				MDp1.digest[j] = MDp2.digest[j];
			for (int pl = pwd.Length ; pl > 0 ; pl -= 16)
				MD5Update(ref MDp1, MDp1.digest, (uint)((pl > 16) ? 16 : pl));
			for (int i = 0 ; i < 16 ; i++)
				MDp1.digest[i] = 0x00;
			for (int i = pwd.Length ; Convert.ToBoolean(i) ; i >>= 1)
				if (Convert.ToBoolean(i & 1))
					MD5Update(ref MDp1, MDp1.digest, 1);
				else
					MD5Update(ref MDp1, (new ASCIIEncoding()).GetBytes(pwd), 1);
			MD5Final(ref MDp1);
			for (int i = 0 ; i < 1000 ; i++) {
				MD5Init(ref MDp2);
				if (Convert.ToBoolean(i & 1))
					MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
				else
					MD5Update(ref MDp2, MDp1.digest, 16);
				if (Convert.ToBoolean(i % 3))
					MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(salt), (uint)salt.Length);
				if (Convert.ToBoolean(i % 7))
					MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
				if (Convert.ToBoolean(i & 1))
					MD5Update(ref MDp2, MDp1.digest, 16);
				else
					MD5Update(ref MDp2, (new ASCIIEncoding()).GetBytes(pwd), (uint)pwd.Length);
				MD5Final(ref MDp2);
				for (int j = 0 ; j < 16 ; j++)
					MDp1.digest[j] = MDp2.digest[j];
			}
			e_pass += magic;
			e_pass += salt + '$';
			ulong l;
			l = (ulong)((MDp1.digest[ 0] << 16) | (MDp1.digest[ 6] << 8) | MDp1.digest[12]);
			_crypt_to64(ref e_pass, l, 4);
			l = (ulong)((MDp1.digest[ 1] << 16) | (MDp1.digest[ 7] << 8) | MDp1.digest[13]);
			_crypt_to64(ref e_pass, l, 4);
			l = (ulong)((MDp1.digest[ 2] << 16) | (MDp1.digest[ 8] << 8) | MDp1.digest[14]);
			_crypt_to64(ref e_pass, l, 4);
			l = (ulong)((MDp1.digest[ 3] << 16) | (MDp1.digest[ 9] << 8) | MDp1.digest[15]);
			_crypt_to64(ref e_pass, l, 4);
			l = (ulong)((MDp1.digest[ 4] << 16) | (MDp1.digest[10] << 8) | MDp1.digest[ 5]);
			_crypt_to64(ref e_pass, l, 4);
			l = (ulong)MDp1.digest[11];
			_crypt_to64(ref e_pass, l, 2);
			return e_pass;
		}
		public void _crypt_to64(ref string s, ulong v, int n)
		{
			while (--n >= 0) {
				s += (char)itoa64[v & 0x3f];
				v >>= 6;
			}
		}
	}
}

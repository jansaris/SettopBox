//
// HashAlgorithm.cs: Defines a Hash Algorithm.
//

using System;


namespace NewCamd.Encryption {
	
	public abstract class HashAlgorithm {
		
		/// <summary>Encrypt the given plain text with the Algorithm indicated</summary>
		/// <param name="p">The plain text</param>
		/// <param name="s">The salt</param>
		/// <returns> The encrypted string </returns>
		public abstract string Encrypt(string p, string s);
		
		/// <summary>Encrypt the given plain text with the Algorithm indicated</summary>
		/// <param name="p">The plain text</param>
		/// <returns> The encrypted string </returns>
		public abstract string Encrypt(string s);
	}
}

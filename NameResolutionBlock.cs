﻿/*
Copyright (c) 2013, Andrew Walsh
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. All advertising materials mentioning features or use of this software
   must display the following acknowledgement:
   This product includes software developed by the <organization>.
4. Neither the name of the <organization> nor the
   names of its contributors may be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY <COPYRIGHT HOLDER> ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PcapngFile
{
	public class NameResolutionBlock : BlockBase
	{
		private const UInt16 NameServerIp4AddressOptionCode = 3;
		private const UInt16 NameServerIp6AddressOptionCode = 4;
		private const UInt16 NameServerNameOptionCode = 2;

		public bool IsIpVersion6
		{
			get { return this.NameServerIp4Address == null; }
		}
		public byte[] NameServerIp4Address { get; private set; }
		public byte[] NameServerIp6Address { get; private set; }
		public string NameServerName { get; private set; }		
		public ReadOnlyCollection<NameResolutionRecord> Records { get; private set; }

		internal NameResolutionBlock(BinaryReader reader)
			: base(reader)
		{
            long startPosition = reader.BaseStream.Position;
			this.Records = this.ReadRecords(reader);
			this.ReadOptions(reader); //We try to read an option, but it makes no sense.  We are reading the options too early!
            long endPosition = reader.BaseStream.Position;
            if (endPosition - startPosition != this.TotalLength - 8-4) { //minus 8: substract the general header; minus 4: we havn't read the trailing length yet
                Console.WriteLine("Did not understand all of the NameResolutionBlock. We have skipped " + (this.TotalLength - 8 - 4 - (endPosition - startPosition)) + " bytes.");
                reader.BaseStream.Seek(((long)this.TotalLength) - 8 - 4 - (endPosition - startPosition), SeekOrigin.Current); //VERDICT: we overread 4 bytes!!
            }
            this.ReadClosingField(reader);
		}

		override protected void OnReadOptionsCode(UInt16 code, byte[] value)
		{
			switch (code)
			{
				case NameServerIp4AddressOptionCode:
					this.NameServerIp4Address = value;
					break;
				case NameServerIp6AddressOptionCode:
					this.NameServerIp6Address = value;
					break;
				case NameServerNameOptionCode:
					this.NameServerName = UTF8Encoding.UTF8.GetString(value);
					break;
			}
		}

		private ReadOnlyCollection<NameResolutionRecord> ReadRecords(BinaryReader reader)
		{
			var records = new List<NameResolutionRecord>();
			NameResolutionRecord record;
			do
			{
				record = new NameResolutionRecord(reader);
				records.Add(record);
			} 
			while (record.IpAddress != null);

			return new ReadOnlyCollection<NameResolutionRecord>(records);
		}
	}
}

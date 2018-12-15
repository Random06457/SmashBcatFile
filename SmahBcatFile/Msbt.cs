using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmahBcatPopupFile
{
    [Serializable]
    public class MsbtParsingException : Exception
    {
        public MsbtParsingException() { }
        public MsbtParsingException(string message) : base(message) { }
        public MsbtParsingException(string message, Exception inner) : base(message, inner) { }
        protected MsbtParsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class Msbt
    {
        public class TextEntry
        {
            public string Label { get; set; } = "";
            public string Value { get; set; } = "";
            public byte[] Attribut { get; set; } = new byte[0];

            public override string ToString()
            {
                return Label ?? "";
            }
        }

        public const string HEADER_MAGIC = "MsgStdBn";
        public const string LABEL_MAGIC = "LBL1";
        public const string TEXT_MAGIC = "TXT2";
        public const string ATTR_MAGIC = "ATR1";

        public ushort Version { get; set; }
        public List<TextEntry> TextEntries = new List<TextEntry>();

        public Msbt(BinaryDataReader br)
        {
            ParseFile(br);
        }
        public Msbt(Stream s) : this(new BinaryDataReader(s))
        {

        }
        public Msbt(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                BinaryDataReader br = new BinaryDataReader(fs);
                ParseFile(br);
            }
        }

        public void ParseFile(BinaryDataReader br)
        {
            br.ByteOrder = ByteOrder.BigEndian;

            long startPos = br.Position;

            //HEADER
            string magic = Encoding.ASCII.GetString(br.ReadBytes(8));
            if (magic != HEADER_MAGIC)
                throw new MsbtParsingException("Invalid Header");

            var bom = (ByteOrder)br.ReadUInt16();

            if (bom == ByteOrder.BigEndian || bom == ByteOrder.LittleEndian)
                br.ByteOrder = bom;
            else throw new MsbtParsingException("Invalid BOM");

            br.ReadInt16();//padding
            Version = br.ReadUInt16();
            int chunckCount = br.ReadInt16();
            br.ReadInt16();//padding
            uint fileSize = br.ReadUInt32();
            br.ReadBytes(0xA);//padding

            for (int i = 0; i < chunckCount; i++)
                ProcessChunk(br);

            br.Position = startPos + fileSize;
        }

        public void ProcessChunk(BinaryDataReader br)
        {
            List<Tuple<Action<BinaryDataReader, uint>, string>> callbacks = new List<Tuple<Action<BinaryDataReader, uint>, string>>()
            {
                new Tuple<Action<BinaryDataReader, uint>, string>(ProcessLabel, LABEL_MAGIC),
                new Tuple<Action<BinaryDataReader, uint>, string>(ProcessAttribut, ATTR_MAGIC),
                new Tuple<Action<BinaryDataReader, uint>, string>(ProcessText, TEXT_MAGIC),
            };

            string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
            uint size = br.ReadUInt32();
            br.ReadUInt64();//padding

            bool found = false;
            foreach (var callback in callbacks)
            {
                if (magic == callback.Item2)
                {
                    callback.Item1(br, size);
                    found = true;
                    break;
                }
            }

            if (!found) throw new MsbtParsingException($"Unknow chunk : \"{magic}\"");
        }

        public void ProcessLabel(BinaryDataReader br, uint size)
        {
            long start = br.Position;

            int count = br.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                br.Position = start + 4 + 8 * i; //count + (c + off) * i

                int c = br.ReadInt32();
                int off = br.ReadInt32();

                br.Position = start + off;
                for (int j = 0; j < c; j++)
                {
                    TextEntries.Add(new TextEntry());
                    byte len = br.ReadByte();
                    TextEntries.Last().Label = Encoding.ASCII.GetString(br.ReadBytes(len));
                    br.ReadInt32();//index
                }
            }

            br.Position = start + size;
            Align(br, 0x10);
        }
        public void ProcessAttribut(BinaryDataReader br, uint size)
        {
            long start = br.Position;
            int count = br.ReadInt32();
            int sizePerItem = br.ReadInt32();
            
            for (int i = 0; i < count; i++)
                TextEntries[i].Attribut = br.ReadBytes(sizePerItem);

            br.Position = start + size;
            Align(br, 0x10);
        }
        public void ProcessText(BinaryDataReader br, uint size)
        {
            long start = br.Position;
            int count = br.ReadInt32();

            List<int> offs = new List<int>();
            for (int i = 0; i < count; i++)
                offs.Add(br.ReadInt32());
            offs.Add((int)size);


            for (int i = 0; i < count; i++)
            {
                br.Position = start + offs[i];

                int len = offs[i + 1] - offs[i];
                Encoding enc = br.ByteOrder == ByteOrder.BigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
                TextEntries[i].Value = enc.GetString(br.ReadBytes(len)).Replace("\0", "\r\n\r\n");
            }

            br.Position = start + size;
            Align(br, 0x10);
        }

        public static void Align(BinaryDataReader br, int padd)
        {
            br.BaseStream.Position += (br.Position % padd) != 0
                ? padd - (br.Position % padd)
                : 0;
        }
    }
}

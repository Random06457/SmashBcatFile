using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmahBcatPopupFile
{
    public class SmashBcatFile
    {
        public enum Langs
        {
            ja,
            enUS,
            fr,
            es,
            enGB, //I guess
            frCA, //I guess
            es419, //I guess
            de,
            nl,
            it,
            ru,
            zhCN, //?? zhCn or zhHans or zgHant
            zhHans,//?? zhCn or zhHans or zgHant
            ko,
        }

        private const int MSBT_COUNT = 14;
        private const int IMAGE_INDEX = MSBT_COUNT;
        private const int URL_INDEX = IMAGE_INDEX + 1;
        private const int FILE_COUNT = URL_INDEX + 1;
        private const int NAME_LEN = 0x40;

        private bool event2File;

        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public List<Msbt> TextFile { get; set; } = new List<Msbt>();
        public Bitmap Img { get; set; }
        public string Url { get; set; }

        public SmashBcatFile(BinaryDataReader br)
        {
            ParseFile(br);
        }
        public SmashBcatFile(Stream s) : this(new BinaryDataReader(s))
        {

        }
        public SmashBcatFile(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                BinaryDataReader br = new BinaryDataReader(fs);
                ParseFile(br);
            }
        }

        private static List<Tuple<int, int>> GetChunks(BinaryDataReader br, int count)
        {
            List<Tuple<int, int>> offs = new List<Tuple<int, int>>();
            for (int i = 0; i < count; i++)
                offs.Add(new Tuple<int, int>(br.ReadInt32(), br.ReadInt32()));

            return offs;
        }

        public void ParseFile(BinaryDataReader br)
        {
            br.ByteOrder = ByteOrder.LittleEndian;

            uint filesize = br.ReadUInt32();

            br.BaseStream.Position = 0x40;
            event2File = br.ReadByte() != 0; //idk how to check otherwise

            br.Position = (event2File) ? 0x40 : 0x50;
            Name1 = Encoding.ASCII.GetString(br.ReadBytes(0x40)).Replace("\0", "");
            Name2 = (event2File) ? null : Encoding.ASCII.GetString(br.ReadBytes(0x40)).Replace("\0", "");

            br.BaseStream.Position = (event2File) ? 0xCC : 0xF0;
            var offs = GetChunks(br, FILE_COUNT);

            for (int i = 0; i < MSBT_COUNT; i++)
            {
                br.Position = offs[i].Item1;

                /* we have to copy the file into a buffer to avoid padding problems */
                byte[] buffer = br.ReadBytes(offs[i+1].Item1 - offs[i].Item1); //should be safe since MSBT_COUNT < FILE_COUNT
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    TextFile.Add(new Msbt(ms));
                }
            }

            br.Position = offs[IMAGE_INDEX].Item1;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(br.ReadBytes(offs[IMAGE_INDEX].Item2));
                Img = (Bitmap)Image.FromStream(ms);
            }

            br.BaseStream.Position = offs[URL_INDEX].Item1;
            Url = Encoding.ASCII.GetString(br.ReadBytes(offs[URL_INDEX].Item2)).Replace("\0", "");
            
        }
        public static void SplitFile(string infile, string outDir)
        {
            using (var fs = File.OpenRead(infile))
            {
                byte[] buffer;
                BinaryDataReader br = new BinaryDataReader(fs);

                br.BaseStream.Position = 0x40;
                bool event2 = br.ReadByte() != 0; //idk how to check otherwise
                if (!event2)
                {
                    br.Position = 0x50;
                    string name1 = Encoding.ASCII.GetString(br.ReadBytes(0x40)).Replace("\0", "");
                    string name2 = Encoding.ASCII.GetString(br.ReadBytes(0x40)).Replace("\0", "");
                    string info = $"Name 1 : {name1}\r\nName 2 : {name2}";

                    File.WriteAllText($@"{outDir}\header_info.txt", info);
                }
                else
                {
                    br.Position = 0x40;
                    string name1 = Encoding.ASCII.GetString(br.ReadBytes(0x40)).Replace("\0", "");
                    string info = $"Name 1 : {name1}";

                    File.WriteAllText($@"{outDir}\header_info.txt", info);
                }

                br.BaseStream.Position = (event2) ? 0xCC : 0xF0;
                var offs = GetChunks(br, FILE_COUNT);
                for (int i = 0; i < MSBT_COUNT; i++)
                {
                    br.Position = offs[i].Item1;
                    buffer = br.ReadBytes(offs[i].Item2);
                    File.WriteAllBytes($@"{outDir}\Message_{(Langs)i}.msbt", buffer);
                }

                br.Position = offs[IMAGE_INDEX].Item1;
                buffer = br.ReadBytes(offs[IMAGE_INDEX].Item2);
                File.WriteAllBytes($@"{outDir}\image.jpg", buffer);

                br.Position = offs[URL_INDEX].Item1;
                string text = Encoding.ASCII.GetString(br.ReadBytes(offs[URL_INDEX].Item2));
                File.WriteAllText($@"{outDir}\url.txt", text);
            }
        }
    }
}

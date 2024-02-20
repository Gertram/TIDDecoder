using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TIDDecoder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        internal class Flags
        {
            public bool isDirectHeader { get; set; }
            public bool isARGB { get; set; }
            public bool isMorgan { get; set; }
            public bool isFlipVertial { get; set; }
        }
        private static Flags getFlags(int value)
        {
            //default value == 0x80
            var flags = new Flags();
            //flags.isDirectHeader = (value & 0b1000000) > 0;
            //flags.isARGB = (value & 0b01000000) > 0;
            //flags.isMorgan = (value & 0b00100000) > 0;
            //flags.isFlipVertial = (value & 0b00010000) > 0;
            flags.isDirectHeader = (value & 0x1) > 0;
            flags.isARGB = (value & 0x2) > 0;
            flags.isMorgan = (value & 0x4) > 0;
            flags.isFlipVertial = (value & 0x8) > 0;
            return flags;
        }
        private static byte[] DecodeFromRGBA(byte[] input, int width, int height)
        {
            var output = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y * width * 4 + x * 4] = input[y * width * 4 + x * 4 + 3];
                    output[y * width * 4 + x * 4 + 1] = input[y * width * 4 + x * 4];
                    output[y * width * 4 + x * 4 + 2] = input[y * width * 4 + x * 4 + 1];
                    output[y * width * 4 + x * 4 + 3] = input[y * width * 4 + x * 4 + 2];
                }
            }
            return output;
        }
        private static byte[] decodeABGR(byte[] input, int width, int height)
        {
            var output = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y * width * 4 + x * 4] = input[y * width * 4 + x * 4 + 3];
                    output[y * width * 4 + x * 4 + 1] = input[y * width * 4 + x * 4+3];
                    output[y * width * 4 + x * 4 + 2] = input[y * width * 4 + x * 4 + 2];
                    output[y * width * 4 + x * 4 + 3] = input[y * width * 4 + x * 4 + 1];
                }
            }
            return output;
        }
        private static byte[] DecodeFromBGRA(byte[] input, int width, int height)
        {
            var output = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[y * width * 4 + x * 4] = input[y * width * 4 + x * 4 + 3];
                    output[y * width * 4 + x * 4 + 3] = input[y * width * 4 + x * 4];
                    output[y * width * 4 + x * 4 + 2] = input[y * width * 4 + x * 4 + 1];
                    output[y * width * 4 + x * 4 + 1] = input[y * width * 4 + x * 4 + 2];
                }
            }
            return output;
        }
        internal static Bitmap CreateBitmap(byte[] outArray, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = outArray[y * width * 4 + x * 4];
                    var r = outArray[y * width * 4 + x * 4 + 1];
                    var g = outArray[y * width * 4 + x * 4 + 2];
                    var b = outArray[y * width * 4 + x * 4 + 3];

                    bitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                }
            }
            return bitmap;
        }
        internal class Header
        {
            internal Flags Flags { get; set; }
            internal int Width { get; set; }
            internal int Height { get; set; }
            internal int FileSize { get; set; }
            internal string Format { get; set; }
        }
        internal static Header ReadHeader(BinaryReader reader)
        {
            reader.BaseStream.Position = 0x3;
            var flag = reader.ReadByte();
            var flags = getFlags(flag);
            Func<int> readInt32 = flags.isDirectHeader ? delegate ()
            {
                return BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray());
            }
            : delegate ()
            {
                return BitConverter.ToInt32(reader.ReadBytes(4).ToArray());
            };
            reader.BaseStream.Position = 0x44;
            var width = readInt32();
            var height = readInt32();
            reader.BaseStream.Position = 0x58;
            var fileSize = readInt32();
            reader.BaseStream.Position = 0x64;
            string format;
            flags.isDirectHeader = true;
            if (flags.isDirectHeader)
            {
                format = new string(reader.ReadChars(4));
            }
            else
            {
                format = new string(reader.ReadChars(4).Reverse().ToArray());
            }
            return new Header
            {
                Flags = flags,
                Width = width,
                Height = height,
                FileSize = fileSize,
                Format = format
            };
        }
        internal static Bitmap DecodeFile(string input, string output)
        {
            using var reader = new BinaryReader(new BufferedStream(File.OpenRead(input)));
            var start = new string(reader.ReadChars(3));
            if (start != "TID")
            {
                Console.WriteLine("Incorrect filetype");
                return null;
            }
            var header = ReadHeader(reader);
            reader.BaseStream.Position = 0x80;
            var fileSize = header.FileSize;
            var width = header.Width;
            var height = header.Height;
            var format = header.Format;
            var inArray = reader.ReadBytes(fileSize);
            var outArray = new byte[width * height * 4];
            int k = 4;
            /*var flag = reader.ReadByte();
            var flags = getFlags(flag);
            reader.BaseStream.Position = 0x44;
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            reader.BaseStream.Position = 0x58;
            var fileSize = reader.ReadInt32();
            reader.BaseStream.Position = 0x64;
            var format = new string(reader.ReadChars(4));
            reader.BaseStream.Position = 0x80;
            var inArray = reader.ReadBytes(fileSize);
            var outArray = new byte[width * height * 4];
            int k = 4;*/
            if (format == "DXT1")
            {
                DxtDecoder.DecompressDXT1(inArray, width, height, outArray);
            }
            else if (format == "DXT3")
            {
                DxtDecoder.DecompressDXT3(inArray, width, height, outArray);
            }
            else if (format == "DXT5")
            {
                DxtDecoder.DecompressDXT5(inArray, width, height, outArray);
                
            }
            else
            {
                //DxtDecoder.DecompressDXT5(inArray, width, height, outArray);
                //outArray = decodeBGRA(inArray, width, height);
                //outArray = decodeABGR(inArray, width, height);
                outArray = inArray;
            }
            header.Flags.isARGB = false;
            if (!header.Flags.isARGB)
            {
                outArray = DecodeFromBGRA(outArray,width,height);
                //outArray = DecodeFromRGBA(outArray, width, height);
            }
            var bitmap = CreateBitmap(outArray, width, height);
           /* if (flags.isFlipVertial)
            {
                bitmap = CropImage(bitmap, new Rectangle(0, 0, 1920, 1080));
            }
*/
            bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);

            return bitmap;
        }
        public static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }
        internal static void ImportFile(string tidFile,string pngFile)
        {
            var stream = File.OpenRead(tidFile);
            using var reader = new BinaryReader(stream);
            var header = ReadHeader(reader);
            using var bitmap = new Bitmap(pngFile);
            if(bitmap.Width != header.Width)
            {
                Console.WriteLine("Incorrect width");
            }
            if(bitmap.Height != header.Height)
            {
                Console.WriteLine("Incorrect height");
            }
            var fileSize = bitmap.Width * bitmap.Height*4;
            var data = new byte[fileSize];
            var pos = 0;
            if(header.Format != "\0\0\0\0")
            {
                Console.WriteLine($"Format is unsupported now\"{header.Format}\"");
            }
            if (header.Flags.isARGB)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++, pos += 4)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        data[pos] = pixel.A;
                        data[pos + 1] = pixel.R;
                        data[pos + 2] = pixel.G;
                        data[pos + 3] = pixel.B;
                    }
                }
            }
            else
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++, pos += 4)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        data[pos] = pixel.R;
                        data[pos + 1] = pixel.G;
                        data[pos + 2] = pixel.B;
                        data[pos + 3] = pixel.A;
                    }
                }
            }
            reader.BaseStream.Position = 0;
            var rawHeader = reader.ReadBytes(0x80);
            reader.Close();
            stream.Close();
            stream = File.OpenWrite(tidFile);
            using var writer = new BinaryWriter(stream);
            writer.Write(rawHeader);
            writer.Write(data);
            stream.Close();
        }
        internal static bool HandleFiles(List<string> paths)
        {
            var tasks = new List<Task>();
            var result = true;
            foreach (var path in paths.Where(x => !x.EndsWith(".png")))
            {
                var task = new Task(delegate
                {
                    try
                    {
                        var pngFile = Path.ChangeExtension(path, ".png");
                        if (!paths.Contains(pngFile))
                        {
                            DecodeFile(path, Path.ChangeExtension(path, ".png"));
                            //pictureBox1.Image = DecodeFile(path, Path.ChangeExtension(path, ".png"));
                            //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            return;
                        }
                        ImportFile(path, pngFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        result = false;
                    }
                });
                task.Start();
                tasks.Add(task);

            }
            Task.WaitAll(tasks.ToArray());
            return result;
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            var paths = new List<string>(e.Data.GetData(DataFormats.FileDrop) as string[]);
            HandleFiles(paths);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
    }
}

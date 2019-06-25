using System;
using System.Linq;
using System.Drawing;
using Imazen.WebP;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using System.IO;
using SharpCompress.Common;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ComicCompressor
{
    public class Compressor
    {
        private Logger Logger { get; set; }

        public int Quality { get; set; } = 75;

        public Compressor(Logger logger)
        {
            Logger = logger;
        }

        public void Compress(string filename, string outputFile = null) 
        {
            outputFile = Path.ChangeExtension(outputFile ?? filename, "cbz");


            Logger.Log("Processing: " + filename, LogLevel.Verbose);

            IArchive archive = null;

            if (filename.EndsWith(".cbr")) 
            {
                archive = RarArchive.Open(filename);
            } 
            else if (filename.EndsWith(".cbz")) 
            {
                archive = ZipArchive.Open(filename);
            }

            ProcessArchive(archive, outputFile);

            Logger.Log("Finished: " + filename, LogLevel.Verbose);
        }

        private void ProcessArchive(IArchive archive, string outputPath)
        {
            var fileEntries = archive.Entries.Where(e => !e.IsDirectory);

            var encoder = new SimpleEncoder();

            using (var output = ZipArchive.Create())
            {
                var imageStreams = new List<Stream>();
                foreach (var entry in fileEntries) 
                {
                    ProcessEntry(entry, imageStreams, output, encoder);
                }

                archive.Dispose();
                
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                output.SaveTo(outputPath, CompressionType.Deflate);

                imageStreams.ForEach(i => i.Dispose());
            }
        }

        private void ProcessEntry(IArchiveEntry entry, IList<Stream> imageStreams, ZipArchive output, SimpleEncoder encoder)
        {
            using (var stream = entry.OpenEntryStream())
            {
                var ms = new MemoryStream();
                imageStreams.Add(ms);

                if (entry.Key.StartsWith("__MACOSX"))
                {
                    return;
                }

                if (!entry.Key.EndsWith(".jpg") && !entry.Key.EndsWith(".png") )
                {
                    stream.CopyTo(ms);
                    output.AddEntry(entry.Key, ms);
                    return;
                }

                Bitmap bits;

                try 
                {
                    bits = new Bitmap(stream);
                }
                catch(Exception e)
                {
                    Logger.LogError("Error parsing bitmap: " + entry.Key);
                    Logger.LogDebug(e, LogLevel.Error);
                    return;
                }

                if (bits.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                {
                    var newBits = ChangePixelFormat(bits);
                    bits.Dispose();
                    bits = newBits;
                }
                
                try
                {
                    encoder.Encode(bits, ms, Quality);
                    output.AddEntry(entry.Key + ".webp", ms);
                }
                catch(Exception e)
                {
                    Logger.LogError("Error encoding entry: " + entry.Key);
                    Logger.LogDebug(e, LogLevel.Error);
                }
                finally
                {
                    bits.Dispose();
                }
            }
        }

        private Bitmap ChangePixelFormat(Bitmap orig, System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        {
            Bitmap clone = new Bitmap(orig.Width, orig.Height, format);

            using (Graphics gr = Graphics.FromImage(clone)) 
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            return clone;

        }

    }
}
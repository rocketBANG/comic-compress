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
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace ComicCompressor
{
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [FileOrDirectoryExists]
        [Option("-i|--input <FOLDER/FIlE>")]
        [Required]
        public string Input { get; }

        [Option("-o|--output <FOLDER>")]
        public string OutputFolder { get; } = "output";

        [Option("-r|--recursive")]
        public bool Recursive { get; } = false;

        [Option("-s|--skip", Description = "Skip processing file if it already exists in the output folder")]
        public bool Skip { get; } = false;

        [Option("-q|--quality", Description = "Quality to use for the webp files (default: 75)")]
        public int Quality { get; } = 75;

        private void OnExecute()
        {
            Queue<string> files;

            if (Directory.Exists(Input))
            {
                files = new Queue<string>(Directory.GetFileSystemEntries(Input));
            }
            else
            {
                // No directory but must exist so it is a file
                files = new Queue<string>();
                files.Enqueue(Input);
            }

            var tasks = new List<Task>();
            while(files.Count > 0)
            {
                var file = files.Dequeue();
                if (Directory.Exists(file)) 
                {
                    if (Recursive)
                    {
                        foreach (var f in Directory.GetFileSystemEntries(file))
                        {
                            files.Enqueue(f);
                        }
                    }
                    continue;
                }

                var task = new Task(() => {
                    try
                    {
                        ProcessFile(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + file);
                        Console.WriteLine(e);
                    }
                });
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        void ProcessFile(string filename) 
        {
            var relativePath = Path.GetRelativePath(Input, filename);
            var outputPath = Path.Join(OutputFolder, Path.ChangeExtension(relativePath, "cbz"));

            if ((File.Exists(outputPath) && Skip) || (!filename.EndsWith(".cbr") && !filename.EndsWith(".cbz")))
            {
                return;
            }


            // Console.WriteLine("Processing: " + filename);
            IArchive archive = null;

            if (filename.EndsWith(".cbr")) 
            {
                archive = RarArchive.Open(filename);
            } 
            else if (filename.EndsWith(".cbz")) 
            {
                archive = ZipArchive.Open(filename);
            }

            var fileEntries = archive.Entries.Where(e => !e.IsDirectory);

            var encoder = new SimpleEncoder();

            using (var output = ZipArchive.Create())
            {
                var imageStreams = new List<Stream>();
                foreach (var entry in fileEntries) 
                {
                    ProcessEntry(entry, imageStreams, output, encoder);
                }
                
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                output.SaveTo(outputPath, CompressionType.Deflate);

                imageStreams.ForEach(i => i.Dispose());
            }

            // Console.WriteLine("Finished: " + filename);
            archive.Dispose();
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
                    Console.WriteLine("Error: " + entry.Key);
                    Console.WriteLine(e);
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
                    Console.WriteLine("Error: " + entry.Key);
                    Console.WriteLine(e);
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

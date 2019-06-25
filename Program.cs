using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace ComicCompressor
{
    public class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [FileOrDirectoryExists]
        [Option("-i|--input <FOLDER/FIlE>", Description = "The file or folder to convert")]
        [Required]
        public string Input { get; }

        [Option("-o|--output <FOLDER>", Description = "Base path of the output (will be in output/subfolders if the recursive option is enabled), default: converted_comics")]
        public string OutputFolder { get; } = "converted_comics";

        [Option("-r|--recursive", Description = "Recursively traverse the input folder (include all subfolders)")]
        public bool Recursive { get; } = false;

        [Option("-s|--skip", Description = "Skip processing file if it already exists in the output folder")]
        public bool Skip { get; } = false;

        [Option("-q|--quality", Description = "Quality to use for the webp files (default: 75)")]
        public int Quality { get; } = 75;

        [Option("-p|--parallel", Description = "Run in parallel, utilizing all computing resources")]
        public bool Parallel { get; } = false;

        private Logger Logger { get; set; }

        private void OnExecute()
        {
            Logger = new Logger();
            Logger.Debug = true;
            Logger.LoggingLevel = LogLevel.Warning;

            IList<string> files = new FileParser().ListAllFiles(Input, Recursive);

            Process(files);
        }

        private void Process(IList<string> files)
        {
            Compressor compressor = new Compressor(Logger);
            compressor.Quality = Quality;

            if (!Parallel)
            {
                foreach (var file in files)
                {
                    CompressTask(compressor, file);
                }
                return;
            }

            var tasks = new List<Task>();
            foreach (var file in files)
            {
                var task = new Task(() => CompressTask(compressor, file));
                task.Start();
                tasks.Add(task);

            }

            Task.WaitAll(tasks.ToArray());
        }

        private void CompressTask(Compressor compressor, string filename)
        {
            var relativePath = Path.GetRelativePath(Input, filename);
            var outputPath = Path.Join(OutputFolder, Path.ChangeExtension(relativePath, "cbz"));

            if ((File.Exists(outputPath) && Skip) || (!filename.EndsWith(".cbr") && !filename.EndsWith(".cbz")))
            {
                return;
            }

            try
            {
                compressor.Compress(filename, outputPath);
            }
            catch (Exception e)
            {
                Logger.Log("Error processing file: " + filename, LogLevel.Error);
                Logger.LogDebug(e, LogLevel.Error);
            }
        }

    }
}

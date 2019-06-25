using System.Collections.Generic;
using System.IO;

namespace ComicCompressor
{
    public class FileParser
    {
        public IList<string> ListAllFiles(string input, bool recursive)
        {
            Queue<string> unexplored;
            IList<string> files = new List<string>();

            if (Directory.Exists(input))
            {
                unexplored = new Queue<string>(Directory.GetFileSystemEntries(input));
            }
            else
            {
                // No directory but must exist so it is a file
                unexplored = new Queue<string>();
                unexplored.Enqueue(input);
            }

            while(unexplored.Count > 0)
            {
                var file = unexplored.Dequeue();

                if (File.Exists(file))
                {
                    files.Add(file);
                }
                else if (recursive)
                {
                    foreach (var f in Directory.GetFileSystemEntries(file))
                    {
                        unexplored.Enqueue(f);
                    }
                }
            }

            return files;

        }
    }

}
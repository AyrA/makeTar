using System;
using System.IO;
using System.IO.Compression;

namespace makeTar
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var FS = File.Create(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Desktop\__empty.tar.gz")))
            {
                //We should be able to create standard .tar.gz by using the builtin gzipstream
                using (var GZ = new GZipStream(FS, CompressionLevel.Optimal))
                {
                    using (var TS = new TAR.TarWriter(GZ, false))
                    {
                        TS.AddDirectory(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Desktop\Images"));
                        TS.FinalizeStream();
                    }
                }
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif
        }
    }
}

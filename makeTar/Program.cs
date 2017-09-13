using System;
using System.IO;

namespace makeTar
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var FS = File.Create(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Desktop\__empty.tar")))
            {
                using (var TS = new TAR.TarStream(FS, false))
                {
                    TS.AddDirectory(Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Desktop\Images"));
                    TS.FinalizeStream();
                }
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif
        }
    }
}

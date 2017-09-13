using System;
using System.IO;

namespace makeTar
{
    class Program
    {
        static void Main(string[] args)
        {
            TAR.TarHeader H = new TAR.TarHeader();
            H.FileName = "test.txt";
            //H.FileSize = 100;
            H.LastModified = DateTime.Now;
            H.Type = TAR.TarLinkType.File;
            H.WriteUStarHeader = true;

            using (var FS = File.Create(@"C:\Users\Administrator\Desktop\__verify.tar"))
            {
                H.Write(FS);
                FS.Write(new byte[0x400], 0, 0x400);
                Console.WriteLine(FS.Length);
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif
        }
    }
}

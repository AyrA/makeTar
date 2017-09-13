using System;
using System.Collections.Generic;
using System.IO;

namespace makeTar.TAR
{
    public class TarStream : IDisposable
    {
        private Stream Output;
        private bool CloseOnDispose;


        public TarStream(Stream Output, bool CloseOnDispose = true)
        {
            this.Output = Output;
            this.CloseOnDispose = CloseOnDispose;
        }

        public void AddDirectory(string BaseDir)
        {
            var RealBase = Path.GetFullPath(BaseDir);
            var Dirs = new Stack<string>();
            Dirs.Push(RealBase);
            while (Dirs.Count > 0)
            {
                var Current = Dirs.Pop();
                foreach (var Dir in Directory.GetDirectories(Current))
                {
                    Dirs.Push(Dir);
                }
                foreach (var FileName in Directory.GetFiles(Current))
                {
#if DEBUG
                    Console.Error.WriteLine($"Adding {FileName}...");
#endif
                    try
                    {
                        var FI = new FileInfo(FileName);
                        using (var FS = FI.OpenRead())
                        {
                            var RelName = FileName
                                .Substring(RealBase.Length + 1)
                                .Replace('\\', '/');
                            var FileHeader = new TarHeader();
                            FileHeader.FileName = RelName;
                            FileHeader.LastModified = FI.LastWriteTimeUtc;
                            FileHeader.FileSize = FI.Length;
                            FileHeader.Write(Output);
                            WriteInBlocks(FS);
                        }
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("Error adding file to archive!");
                        Console.ResetColor();
                    }
                }
            }
        }

        public void FinalizeStream()
        {
            byte[] Data = TarHeader.GetBlock();
            Output.Write(Data, 0, Data.Length);
            Output.Write(Data, 0, Data.Length);
            Output.Flush();
        }

        private void WriteInBlocks(Stream Input)
        {
            //Copy in 10 MB Blocks
            Input.CopyTo(Output, 1000 * 1000 * 10);
            int Leftover = (int)(Input.Length % TarHeader.BLOCKSIZE);
            if (Leftover > 0)
            {
                Output.Write(new byte[TarHeader.BLOCKSIZE - Leftover], 0, TarHeader.BLOCKSIZE - Leftover);
            }
        }

        public void Dispose()
        {
            if (CloseOnDispose && Output != null)
            {
                Output.Close();
                Output.Dispose();
            }
            Output = null;
        }
    }
}

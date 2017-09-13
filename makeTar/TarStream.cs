using System;
using System.Collections.Generic;
using System.IO;

namespace makeTar.TAR
{
    /// <summary>
    /// TAR Writer
    /// </summary>
    public class TarWriter : IDisposable
    {
        /// <summary>
        /// Output Stream
        /// </summary>
        private Stream Output;
        /// <summary>
        /// Close Output on Dispose
        /// </summary>
        private bool CloseOnDispose;

        /// <summary>
        /// Creates a new TAR Stream
        /// </summary>
        /// <param name="Output">Output Stream</param>
        /// <param name="CloseOnDispose">Close Output on Dispose</param>
        public TarWriter(Stream Output, bool CloseOnDispose = true)
        {
            this.Output = Output;
            this.CloseOnDispose = CloseOnDispose;
        }

        /// <summary>
        /// Recursively adds a Directory
        /// </summary>
        /// <param name="BaseDir">Root Directory to Add</param>
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

        /// <summary>
        /// Finalizes the stream by writing two empty Blocks to it
        /// </summary>
        public void FinalizeStream()
        {
            byte[] Data = TarHeader.GetBlock();
            Output.Write(Data, 0, Data.Length);
            Output.Write(Data, 0, Data.Length);
            Output.Flush();
        }

        /// <summary>
        /// Writes a file to the output and ensures it's a multiple of <see cref="TarHeader.BLOCKSIZE"/>
        /// </summary>
        /// <param name="Input">Source Stream</param>
        /// <remarks>Source Stream must have Length and Position Property</remarks>
        private void WriteInBlocks(Stream Input)
        {
            long Start = Input.Position;
            //Copy in 10 MB Blocks
            Input.CopyTo(Output, 1000 * 1000 * 10);
            int Leftover = (int)((Input.Length - Start) % TarHeader.BLOCKSIZE);
            if (Leftover > 0)
            {
                Output.Write(new byte[TarHeader.BLOCKSIZE - Leftover], 0, TarHeader.BLOCKSIZE - Leftover);
            }
        }

        /// <summary>
        /// Removes Referenc on Output and closes it if requested in constructor
        /// </summary>
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

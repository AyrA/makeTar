using System;
using System.IO;
using System.Linq;
using System.Text;

namespace makeTar.TAR
{
    /// <summary>
    /// Possible entry Type
    /// </summary>
    public enum TarLinkType : int
    {
        /// <summary>
        /// File
        /// </summary>
        File_Alt = '\0',
        /// <summary>
        /// File
        /// </summary>
        File = '0',
        /// <summary>
        /// Hard Link
        /// </summary>
        HardLink = '1',
        /// <summary>
        /// Symbolic Link
        /// </summary>
        SymbolicLink = '2',
        /// <summary>
        /// Character device
        /// </summary>
        CharacterSpecial = '3',
        /// <summary>
        /// Block device
        /// </summary>
        BlockSpecial = '4',
        /// <summary>
        /// Directory
        /// </summary>
        Directory = '5',
        /// <summary>
        /// FIFO Stream
        /// </summary>
        FIFO = '6',
        /// <summary>
        /// File that must not be fragmented (!?)
        /// </summary>
        ContiguousFile = '7',
        /// <summary>
        /// Extended Header (!?)
        /// </summary>
        GlobalExtendedHeader = 'g',
        /// <summary>
        /// Extended Header (!?)
        /// </summary>
        ExtendedHeader = 'x'
    }

    /// <summary>
    /// File/Directory Permissions
    /// </summary>
    [Flags]
    public enum Permissions : int
    {
        /// <summary>
        /// No Access
        /// </summary>
        None = 0,
        /// <summary>
        /// Ececute/Browse
        /// </summary>
        Execute = 1,
        /// <summary>
        /// Write/Create/Delete
        /// </summary>
        Write = 2,
        /// <summary>
        /// Read
        /// </summary>
        Read = 4,
        /// <summary>
        /// All (int:7)
        /// </summary>
        All = Execute | Write | Read
    }

    /// <summary>
    /// Represents a TAR File Header
    /// </summary>
    public class TarHeader
    {
        /// <summary>
        /// Blocksize for TAR contents
        /// </summary>
        public const int BLOCKSIZE = 512;

        #region Original Header
        public string FileName
        { get; set; }
        public int FileMode
        { get; set; }
        public int OwnerId
        { get; set; }
        public int GroupId
        { get; set; }
        public long FileSize
        { get; set; }
        public DateTime LastModified
        { get; set; }
        public TarLinkType Type
        { get; set; }
        public string NameOfLinkedFile
        { get; set; }
        #endregion
        #region UStar Fields
        public bool WriteUStarHeader
        { get; set; }
        public string OwnerName
        { get; set; }
        public string GroupName
        { get; set; }
        public int DeviceMajorNumber
        { get; set; }
        public int DeviceMinorNumber
        { get; set; }
        public string FilenamePrefix
        { get; set; }
        #endregion

        /// <summary>
        /// Create TAR File header
        /// </summary>
        public TarHeader()
        {
            SetPerms();
            WriteUStarHeader = true;
            Type = TarLinkType.File;
        }

        /// <summary>
        /// Write Header to Stream
        /// </summary>
        /// <param name="Output">Output Stream</param>
        /// <param name="FlushOutput">Flush output stream after writing</param>
        public void Write(Stream Output, bool FlushOutput = false)
        {
            SplitFileName();
            using (var MS = new MemoryStream())
            {
                using (var BW = new BinaryWriter(MS, Encoding.ASCII, true))
                {
                    //File (0-100)
                    BW.Write(ToPaddedBytes(FileName, 99));
                    BW.Write((byte)0);

                    //Mode (100-108)
                    BW.Write(ToOctal(FileMode, 7));
                    BW.Write((byte)0);

                    //Owner User (108-116)
                    BW.Write(ToOctal(OwnerId, 7));
                    BW.Write((byte)0);

                    //Owner Group (116-124)
                    BW.Write(ToOctal(GroupId, 7));
                    BW.Write((byte)0);

                    //File Size (124-136)
                    BW.Write(ToOctal(FileSize, 11));
                    BW.Write((byte)0);

                    //Mode (136-148)
                    BW.Write(ToOctal((long)LastModified.ToUniversalTime().Subtract(LinuxTime).TotalSeconds, 11));
                    BW.Write((byte)0);

                    //Checksum (148-156, Template)
                    BW.Write(new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 });

                    //Type (156-157)
                    BW.Write((byte)Type);

                    //Linked File (127-257)
                    BW.Write(ToPaddedBytes(NameOfLinkedFile, 99));
                    BW.Write((byte)0);

                    if (WriteUStarHeader)
                    {
                        //Header
                        BW.Write(ToPaddedBytes("ustar", 6));
                        //Header Version
                        BW.Write(new byte[] { 0x30, 0x30 });

                        //Owner Name
                        BW.Write(ToPaddedBytes(OwnerName, 31));
                        BW.Write((byte)0);

                        //Group Name
                        BW.Write(ToPaddedBytes(GroupName, 31));
                        BW.Write((byte)0);

                        //Device Major Number
                        BW.Write(ToOctal(DeviceMajorNumber, 7));
                        BW.Write((byte)0);

                        //Device Minor Number
                        BW.Write(ToOctal(DeviceMinorNumber, 7));
                        BW.Write((byte)0);

                        //Filename Prefix
                        BW.Write(ToPaddedBytes(FilenamePrefix, 154));
                        BW.Write((byte)0);

                        //Padding
                        BW.Write(new byte[12]);
                    }
                    else
                    {
                        //Padding
                        BW.Write(new byte[0xFF]);
                    }
                    BW.Flush();
                }
                //Calculate checksum of Header and write to appropriate location
                var Checksum = MS.ToArray().Sum(m => m);
                MS.Seek(148, SeekOrigin.Begin);
                MS.Write(ToOctal(Checksum, 6), 0, 6);
                MS.WriteByte(0);

                //Copy Header to output in one step
                //this speeds up the process and prevents seeking of the Output
                MS.Seek(0, SeekOrigin.Begin);
                MS.CopyTo(Output);
                if (FlushOutput)
                {
                    Output.Flush();
                }
            }
        }

        /// <summary>
        /// Simplyfies Permissions
        /// </summary>
        /// <param name="Owner">User Permissions</param>
        /// <param name="Group">Group Permissions</param>
        /// <param name="Other">Other Permissions</param>
        public void SetPerms(Permissions Owner = Permissions.All, Permissions Group = Permissions.All, Permissions Other = Permissions.All)
        {
            FileMode = (1 << 15) + ((int)Owner << 6) + ((int)Group << 3) + (int)Other;
        }

        /// <summary>
        /// Splits a file name across the two header fields if it is too long
        /// </summary>
        private void SplitFileName()
        {
            if (Encoding.UTF8.GetByteCount(FileName) > 99)
            {
                int cutoff = 0;
                var Segments = FileName.Split('/');
                while (Encoding.UTF8.GetByteCount(FileName) > 99)
                {
                    ++cutoff;
                    FileName = string.Join("/", Segments.Skip(cutoff).ToArray());
                    FilenamePrefix = string.Join("/", Segments.Take(cutoff).ToArray());
                }
            }
        }

        /// <summary>
        /// Gets the Base linux time (1970-01-01 00:00:00 UTC)
        /// </summary>
        public static DateTime LinuxTime
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Converts a string to a 0 padded byte array
        /// </summary>
        /// <param name="Input">Input string</param>
        /// <param name="Length">Destination Length</param>
        /// <returns>Padded byte array</returns>
        /// <remarks>Accepts null and empty strings</remarks>
        public static byte[] ToPaddedBytes(string Input, int Length)
        {
            if (string.IsNullOrEmpty(Input))
            {
                return new byte[Length];
            }
            byte[] Data = Encoding.UTF8.GetBytes(Input);
            using (var MS = new MemoryStream(Length))
            {
                MS.Write(Data, 0, Data.Length);
                MS.Write(new byte[Length - Data.Length], 0, Length - Data.Length);
                return MS.ToArray();
            }
        }

        /// <summary>
        /// Converts an octal string into an integer
        /// </summary>
        /// <param name="Number">Octal string</param>
        /// <returns>Integer</returns>
        public static int FromOctal(string Number)
        {
            return Convert.ToInt32(Number.TrimStart('0'), 8);
        }
        
        /// <summary>
        /// Converts an integer to an Octal string as byte array
        /// </summary>
        /// <param name="Number">Number</param>
        /// <param name="MinLength">Minimum Length of byte array to return</param>
        /// <returns>Byte array representing string octal number</returns>
        /// <remarks>Byte array is padded on the left with '0'</remarks>
        public static byte[] ToOctal(long Number, int MinLength = 0)
        {
            var Result = Convert.ToString(Number, 8);
            return Encoding.ASCII.GetBytes(Result.Length > MinLength ? Result : Result.PadLeft(MinLength, '0'));
        }

        /// <summary>
        /// Gets an empty block
        /// </summary>
        /// <returns>Empty block</returns>
        public static byte[] GetBlock()
        {
            return new byte[BLOCKSIZE];
        }
    }
}

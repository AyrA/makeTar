using System;
using System.IO;
using System.Linq;
using System.Text;

namespace makeTar.TAR
{
    public enum TarLinkType : int
    {
        File_Alt = '\0',
        File = '0',
        HardLink = '1',
        SymbolicLink = '2',
        CharacterSpecial = '3',
        BlockSpecial = '4',
        Directory = '5',
        FIFO = '6',
        ContiguousFile = '7',
        GlobalExtendedHeader = 'g',
        ExtendedHeader = 'x'
    }

    [Flags]
    public enum Permissions : int
    {
        None = 0,
        Execute = 1,
        Write = 2,
        Read = 4,
        All = Execute | Write | Read
    }

    public class TarHeader
    {
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

        public TarHeader()
        {
            SetPerms();
            WriteUStarHeader = true;
            Type = TarLinkType.File;
        }

        public void Write(Stream Output)
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

                    BW.Flush();

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
                var Checksum = MS.ToArray().Sum(m => m);
                MS.Seek(148, SeekOrigin.Begin);
                MS.Write(ToOctal(Checksum, 6), 0, 6);
                MS.WriteByte(0);
                MS.Seek(0, SeekOrigin.Begin);
                MS.CopyTo(Output);
            }
        }

        public void SetPerms(Permissions User = Permissions.All, Permissions Owner = Permissions.All, Permissions Other = Permissions.All)
        {
            FileMode = (1 << 15) + ((int)User << 6) + ((int)Owner << 3) + (int)Other;
        }

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

        public static DateTime LinuxTime
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

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

        public static int FromOctal(string Number)
        {
            return Convert.ToInt32(Number.TrimStart('0'), 8);
        }

        public static byte[] ToOctal(long Number, int MinLength = 0)
        {
            var Result = Convert.ToString(Number, 8);
            return Encoding.ASCII.GetBytes(Result.Length > MinLength ? Result : Result.PadLeft(MinLength, '0'));
        }

        public static byte[] GetBlock()
        {
            return new byte[BLOCKSIZE];
        }
    }
}

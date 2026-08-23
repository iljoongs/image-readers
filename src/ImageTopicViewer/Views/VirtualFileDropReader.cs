using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using WpfIDataObject = System.Windows.IDataObject;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace ImageTopicViewer.Views;

/// <summary>
/// 웹 브라우저 등이 드래그드롭으로 제공하는 "가상 파일"(FileGroupDescriptor/FileContents)을 읽는다.
/// Chrome/Edge 등에서 이미지를 드래그하면 실제 로컬 파일이 아니라 이 형식으로 제공되므로
/// DataFormats.FileDrop/Bitmap만으로는 잡히지 않는다 (05-image-features.md의 "웹 브라우저 등에서 드롭한 경우").
/// </summary>
internal static class VirtualFileDropReader
{
    private const string FileGroupDescriptorFormat = "FileGroupDescriptorW";
    private const string FileContentsFormat = "FileContents";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FILEDESCRIPTOR
    {
        public uint dwFlags;
        public Guid clsid;
        public int sizelCx;
        public int sizelCy;
        public int pointlX;
        public int pointlY;
        public uint dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    [DllImport("ole32.dll")]
    private static extern void ReleaseStgMedium(ref STGMEDIUM medium);

    /// <summary>드래그 중(DragOver)에 쓰는 가벼운 확인. 내용은 읽지 않는다.</summary>
    public static bool HasVirtualFiles(WpfIDataObject data)
    {
        if (data is not ComIDataObject comData)
        {
            return false;
        }

        var formatEtc = CreateFormatEtc(FileGroupDescriptorFormat, TYMED.TYMED_HGLOBAL);
        return comData.QueryGetData(ref formatEtc) == 0; // S_OK
    }

    public static List<Stream> ReadImageStreams(WpfIDataObject data)
    {
        var result = new List<Stream>();

        if (data is not ComIDataObject comData)
        {
            return result;
        }

        var descriptorFormatEtc = CreateFormatEtc(FileGroupDescriptorFormat, TYMED.TYMED_HGLOBAL);
        if (comData.QueryGetData(ref descriptorFormatEtc) != 0)
        {
            return result;
        }

        comData.GetData(ref descriptorFormatEtc, out var descriptorMedium);
        try
        {
            var ptr = GlobalLock(descriptorMedium.unionmember);
            if (ptr == IntPtr.Zero)
            {
                return result;
            }

            int count;
            try
            {
                count = Marshal.ReadInt32(ptr);
            }
            finally
            {
                GlobalUnlock(descriptorMedium.unionmember);
            }

            for (var i = 0; i < count; i++)
            {
                var stream = ReadFileContents(comData, i);
                if (stream is not null)
                {
                    result.Add(stream);
                }
            }
        }
        finally
        {
            ReleaseStgMedium(ref descriptorMedium);
        }

        return result;
    }

    private static Stream? ReadFileContents(ComIDataObject comData, int index)
    {
        var contentsFormatEtc = CreateFormatEtc(FileContentsFormat, TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL);
        contentsFormatEtc.lindex = index;

        if (comData.QueryGetData(ref contentsFormatEtc) != 0)
        {
            return null;
        }

        comData.GetData(ref contentsFormatEtc, out var contentMedium);
        try
        {
            if (contentMedium.tymed == TYMED.TYMED_ISTREAM)
            {
                var comStream = (IStream)Marshal.GetObjectForIUnknown(contentMedium.unionmember);
                return ReadComStream(comStream);
            }

            if (contentMedium.tymed == TYMED.TYMED_HGLOBAL)
            {
                return ReadHGlobal(contentMedium.unionmember);
            }

            return null;
        }
        finally
        {
            ReleaseStgMedium(ref contentMedium);
        }
    }

    private static MemoryStream? ReadHGlobal(IntPtr hGlobal)
    {
        var size = (int)GlobalSize(hGlobal);
        var ptr = GlobalLock(hGlobal);
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            return new MemoryStream(bytes);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    private static MemoryStream ReadComStream(IStream comStream)
    {
        var result = new MemoryStream();
        var buffer = new byte[8192];
        var bytesReadPtr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            int bytesRead;
            do
            {
                comStream.Read(buffer, buffer.Length, bytesReadPtr);
                bytesRead = Marshal.ReadInt32(bytesReadPtr);
                if (bytesRead > 0)
                {
                    result.Write(buffer, 0, bytesRead);
                }
            }
            while (bytesRead == buffer.Length);
        }
        finally
        {
            Marshal.FreeHGlobal(bytesReadPtr);
        }

        result.Position = 0;
        return result;
    }

    private static FORMATETC CreateFormatEtc(string formatName, TYMED tymed)
    {
        return new FORMATETC
        {
            cfFormat = (short)RegisterClipboardFormat(formatName),
            ptd = IntPtr.Zero,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = tymed,
        };
    }
}

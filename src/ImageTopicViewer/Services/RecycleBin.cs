using System.IO;
using System.Runtime.InteropServices;

namespace ImageTopicViewer.Services;

/// <summary>
/// 파일/폴더를 영구 삭제 대신 Windows 휴지통으로 이동한다 (doc/04, doc/05 삭제 정책).
/// Microsoft.VisualBasic.FileIO 의존성 없이 shell32의 고전 SHFileOperation을 사용한다.
/// </summary>
internal static class RecycleBin
{
    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT fileOp);

    /// <summary>파일 또는 폴더 하나를 휴지통으로 이동한다.</summary>
    /// <exception cref="IOException">이동에 실패했을 때</exception>
    public static void Send(string path)
    {
        var fileOp = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + '\0' + '\0', // pFrom은 이중 널 종료 문자열이어야 한다.
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT,
        };

        var result = SHFileOperation(ref fileOp);
        if (result != 0)
        {
            throw new IOException($"휴지통으로 이동하지 못했습니다. (코드 {result})");
        }
    }
}

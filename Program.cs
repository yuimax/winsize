using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;


public class Win32Api
{
    // ウィンドウを列挙するためのコールバック関数のデリゲート
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}

class Program
{
    private static IntPtr _foundWindowHandle;
    private static Regex _windowTitleRegex;

    // Win32 API関数の定義
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // SetWindowPosに渡すフラグ
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_SHOWWINDOW = 0x0040;

    static void Main(string[] args)
    {
        // コマンドライン引数のチェック
        if (args.Length != 5)
        {
            Console.WriteLine("使い方: WinSize <正規表現パターン> <X> <Y> <幅> <高さ>");
            Console.WriteLine("例: WinSize \"^.*メモ帳$\" 100 100 800 600");
            return;
        }

        string regexPattern = args[0];
        int x = int.Parse(args[1]);
        int y = int.Parse(args[2]);
        int width = int.Parse(args[3]);
        int height = int.Parse(args[4]);

        try
        {
            _windowTitleRegex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            // ウィンドウを列挙して検索
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);

            if (_foundWindowHandle != IntPtr.Zero)
            {
                // ウィンドウの位置とサイズを変更
                SetWindowPos(_foundWindowHandle, IntPtr.Zero, x, y, width, height, SWP_SHOWWINDOW);
                Console.WriteLine($"ウィンドウが見つかり、位置とサイズを変更しました。");
            }
            else
            {
                Console.WriteLine("指定された正規表現に一致するウィンドウは見つかりませんでした。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
    }

    // EnumWindowsから呼び出されるコールバック関数
    private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd) || _foundWindowHandle != IntPtr.Zero)
        {
            return true; // 次のウィンドウへ
        }

        const int nChars = 256;
        StringBuilder sb = new StringBuilder(nChars);
        if (GetWindowText(hWnd, sb, nChars) > 0)
        {
            string title = sb.ToString();
            if (_windowTitleRegex.IsMatch(title))
            {
                _foundWindowHandle = hWnd;
                return false; // 最初に見つかったウィンドウで列挙を終了
            }
        }
        return true; // 次のウィンドウへ
    }

    // ウィンドウが可視状態であるかを確認する
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);
}
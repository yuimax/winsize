using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;


class Program
{
    private static IntPtr _foundWindowHandle;
    private static String _foundWindowTitie;
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

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]    
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    static void Main(string[] args)
    {
        // コマンドライン引数のチェック
        if (!(args.Length == 5 || args.Length == 1))
        {
            Console.WriteLine("Usage: WinSize <ウィンドウタイトル(正規表現)> [<X> <Y> <幅> <高さ>]");
            Console.WriteLine("       座標を指定しない場合、現在の座標を表示する");
            return;
        }

        string regexPattern = args[0];

        try
        {
            _windowTitleRegex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            // ウィンドウを列挙して検索
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);

            if (_foundWindowHandle != IntPtr.Zero)
            {
                if (args.Length == 1)
                {
                    // ウィンドウの位置とサイズを取得
                    RECT rect;
                    if (GetWindowRect(_foundWindowHandle, out rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        Console.WriteLine($"タイトル: {_foundWindowTitie}");
                        Console.WriteLine($"現在の座標: {rect.Left} {rect.Top} {width} {height}");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: GetWindorRect() failed.");
                    }
                    return;
                }
                else if (args.Length >= 5)
                {
                    // ウィンドウの位置とサイズを変更
                    int x = int.Parse(args[1]);
                    int y = int.Parse(args[2]);
                    int width = int.Parse(args[3]);
                    int height = int.Parse(args[4]);

                    SetWindowPos(_foundWindowHandle, IntPtr.Zero, x, y, width, height, SWP_SHOWWINDOW);
                    Console.WriteLine($"タイトル: {_foundWindowTitie}");
                    Console.WriteLine($"位置とサイズを変更しました。");
                }
                else
                {
                    Console.WriteLine($"ERROR: args.Length must be 1 or 5");
                }
            }
            else
            {
                Console.WriteLine("指定されたタイトルのウィンドウは見つかりませんでした。");
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
                _foundWindowTitie = title;
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
using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOC_INFO_1 pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DOC_INFO_1
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDataType;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Enter the text to print:");
        string inputText = Console.ReadLine();

        string printerName = SelectPrinter();
        if (string.IsNullOrEmpty(printerName))
        {
            Console.WriteLine("No printer selected.");
            return;
        }

        PrintTextUsingSpooler(inputText, printerName);
    }

    static string SelectPrinter()
    {
        Console.WriteLine("Available printers:");
        string[] printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
        for (int i = 0; i < printers.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {printers[i]}");
        }

        Console.WriteLine("Select a printer by number:");
        int selectedIndex = int.Parse(Console.ReadLine()) - 1;

        if (selectedIndex < 0 || selectedIndex >= printers.Length)
        {
            Console.WriteLine("Invalid selection.");
            return null;
        }

        return printers[selectedIndex];
    }

    static void PrintTextUsingSpooler(string text, string printerName)
    {
        IntPtr hPrinter = IntPtr.Zero;
        if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
        {
            DOC_INFO_1 docInfo = new DOC_INFO_1
            {
                pDocName = "Text Print Job",
                pDataType = "RAW"
            };

            if (StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                if (StartPagePrinter(hPrinter))
                {
                    try
                    {
                        byte[] setLeftMargin = new byte[] { 0x1D, 0x4C, 0x10, 0x00 }; 

                        byte[] moveToTop = new byte[] { 0x1B, 0x4A, 40 }; 

                        string wrappedText = WrapText(text, 45); 

                       
                        byte[] textBytes = Encoding.ASCII.GetBytes(wrappedText + "\n");

                        byte[] printData = new byte[setLeftMargin.Length + moveToTop.Length + textBytes.Length];
                        Array.Copy(setLeftMargin, 0, printData, 0, setLeftMargin.Length);
                        Array.Copy(moveToTop, 0, printData, setLeftMargin.Length, moveToTop.Length);
                        Array.Copy(textBytes, 0, printData, setLeftMargin.Length + moveToTop.Length, textBytes.Length);

                        IntPtr pBytes = Marshal.AllocHGlobal(printData.Length);
                        Marshal.Copy(printData, 0, pBytes, printData.Length);

                        int dwWritten = 0;
                        WritePrinter(hPrinter, pBytes, printData.Length, out dwWritten);

                        Marshal.FreeHGlobal(pBytes);
                        EndPagePrinter(hPrinter);

                        Console.WriteLine("Print job submitted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error during printing: " + ex.Message);
                    }
                }

                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        else
        {
            Console.WriteLine("Failed to open printer.");
        }
    }
    static string WrapText(string text, int lineWidth)
    {
        StringBuilder wrappedText = new StringBuilder();
        string[] words = text.Split(' ');

        int currentLineWidth = 0;
        foreach (string word in words)
        {
            if (currentLineWidth + word.Length > lineWidth)
            {
                wrappedText.Append("\n"); 
                currentLineWidth = 0;
            }

            wrappedText.Append(word + " ");
            currentLineWidth += word.Length + 1; 
        }

        return wrappedText.ToString();
    }
}

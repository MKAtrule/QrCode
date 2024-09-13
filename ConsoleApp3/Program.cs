using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using QRCoder;

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
        Console.WriteLine("Enter the command what you want to print 1-QrCode 2-Text");
        int input = int.Parse(Console.ReadLine());
        if (input == 1)
        {
            Console.WriteLine("Enter the text for the QR code:");
            string inputText = Console.ReadLine();

            HandleData(inputText);
            Console.WriteLine("QR code generated and saved as 'QRCode.png'.");

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
                return;
            }

            string selectedPrinter = printers[selectedIndex];

            PrintQRCodeUsingSpooler("QRCode.png", selectedPrinter);
        }
        

        static void HandleData(string model)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(model, QRCodeGenerator.ECCLevel.L);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeAsBitmap = qrCode.GetGraphic(20);

            string filePath = "QRCode.png";
            SaveBitmapToFile(qrCodeAsBitmap, filePath);
        }

        static void SaveBitmapToFile(Bitmap bitmap, string filePath)
        {
            bitmap.Save(filePath, ImageFormat.Png);
        }

        static void PrintQRCodeUsingSpooler(string filePath, string printerName)
        {
            IntPtr hPrinter = IntPtr.Zero;
            if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                DOC_INFO_1 docInfo = new DOC_INFO_1
                {
                    pDocName = "QRCode Print Job",
                    pDataType = "RAW"
                };

                if (StartDocPrinter(hPrinter, 1, ref docInfo))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        try
                        {
                            byte[] qrCodeBytes = File.ReadAllBytes(filePath);

                            IntPtr pBytes = Marshal.AllocHGlobal(qrCodeBytes.Length);
                            Marshal.Copy(qrCodeBytes, 0, pBytes, qrCodeBytes.Length);

                            int dwWritten = 0;
                            WritePrinter(hPrinter, pBytes, qrCodeBytes.Length, out dwWritten);

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
    }
}
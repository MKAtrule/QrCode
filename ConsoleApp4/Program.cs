using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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
                        Bitmap qrCodeBitmap = new Bitmap(filePath);

                        byte[] escPosData = ConvertBitmapToEscPosRaster(qrCodeBitmap);

                        IntPtr pBytes = Marshal.AllocHGlobal(escPosData.Length);
                        Marshal.Copy(escPosData, 0, pBytes, escPosData.Length);

                        int dwWritten = 0;
                        WritePrinter(hPrinter, pBytes, escPosData.Length, out dwWritten);

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

    static byte[] ConvertBitmapToEscPosRaster(Bitmap bitmap)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(new byte[] { 0x1B, 0x40 }, 0, 2);  
            ms.Write(new byte[] { 0x1B, 0x33, 0x00 }, 0, 3);  

            for (int y = 0; y < bitmap.Height; y += 24)
            {
                ms.Write(new byte[] { 0x1B, 0x2A, 33, (byte)(bitmap.Width % 256), (byte)(bitmap.Width / 256) }, 0, 5);

                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        byte slice = 0;
                        for (int b = 0; b < 8; b++)
                        {
                            int yPosition = y + k * 8 + b;
                            if (yPosition < bitmap.Height)
                            {
                                Color pixel = bitmap.GetPixel(x, yPosition);
                                if (pixel.GetBrightness() < 0.5)
                                {
                                    slice |= (byte)(1 << (7 - b));
                                }
                            }
                        }
                        ms.WriteByte(slice);
                    }
                }
                ms.Write(new byte[] { 0x0A }, 0, 1);  
            }

            ms.Write(new byte[] { 0x1B, 0x32 }, 0, 2);
            return ms.ToArray();
        }
    }
}

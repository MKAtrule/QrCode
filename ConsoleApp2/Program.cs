using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using QRCoder;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Enter the text for the QR code:");
        string inputText = Console.ReadLine();

     

        HandleData(inputText);

        Console.WriteLine("QR code generated and saved as 'QRCode.png'.");

        Console.WriteLine("Available printers:");
        string[] printers = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
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

        PrintQRCode("QRCode.png", selectedPrinter);
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

    static void PrintQRCode(string filePath, string printerName)
    {  
        PrintDocument printDoc = new PrintDocument
        {
            PrinterSettings = new PrinterSettings
            {
                PrinterName = printerName
            }
        };

        printDoc.PrintPage += (sender, e) =>
        {
            using (Bitmap bitmap = new Bitmap(filePath))
            {
                int size = Math.Min(e.MarginBounds.Width, e.MarginBounds.Height);

                Rectangle destRect = new Rectangle(
                    e.MarginBounds.Left,
                    e.MarginBounds.Top,
                     size,
                    size);

                e.Graphics.DrawImage(bitmap, destRect);

               
            }
        };

        try
        {
            printDoc.Print();
            Console.WriteLine("Printing starteid...");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while printing: " + ex.Message);
        }
    }
}


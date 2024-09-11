using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter the text to generate QR code: ");
        string input = Console.ReadLine();

        QRCodeGenerator qrGenerator = new QRCodeGenerator();

        QRCodeData qrCodeData = qrGenerator.CreateQrCode(input, QRCodeGenerator.ECCLevel.L);

        QRCode qrCode = new QRCode(qrCodeData);

        using (Bitmap qrCodeAsBitmap = qrCode.GetGraphic(20))
        {
            string filePath = "QRCode.png";

            qrCodeAsBitmap.Save(filePath, ImageFormat.Png);


            Console.WriteLine($"QR code has been saved to: {filePath}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true 
            });
        }
    }
}

using System;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text;
using CommonLibrary;

namespace Server
{
    internal class Program
    {

        static void FileServer(string ip)
        {
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            int port = 12346;

            //Створити TcpListener для прийому підключень клієнтів
            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("File server started. Waiting for a connection...");

            //Обробка підключення має включати логіку для відправки інформації про каталоги клієнту.
            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Client connected to file server");

                var root = TreeHelper.CreateTree(
                    withFiles: true,
                    serverFolderPath: @"C:\Users\kvvkv\source\repos");

                // Серіалізація на сервері

                string json = JsonConvert.SerializeObject(root);
                byte[] data = Encoding.UTF8.GetBytes(json);
                client.GetStream().Write(data, 0, data.Length);

                client.Close();
            }


        }

        static void Main(string[] args)
        {
            string ip = "0.0.0.0";

            Task.WaitAll(new Task[]
            {
                Task.Factory.StartNew(() => ScreenServer(ip)),
                Task.Factory.StartNew(() => FileServer(ip))
            });


        }

        static void ScreenServer(string ip)
        {
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            int port = 12345;

            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("Server started. Waiting for a connection...");

            using (Bitmap screenCapture = CaptureScreen())

            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Client connected");

                while (true)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {

                        using (Graphics graphics = Graphics.FromImage(screenCapture))
                        {
                            graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                        }

                        screenCapture.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);


                        byte[] imageBytes = memoryStream.ToArray();

                        // Send image size in bytes
                        byte[] imageSizeData = BitConverter.GetBytes(imageBytes.LongLength);
                        client.GetStream().Write(imageSizeData, 0, imageSizeData.Length);

                        client.GetStream().Write(imageBytes, 0, imageBytes.Length);
                        //client.GetStream().Flush();

                        Console.WriteLine("Send {0}", imageBytes.Length);


                        Task.Delay(100).Wait(); // delay before capturing the next frame

                    }
                }
            }
        }
        static Bitmap CaptureScreen()
        {
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(screenBounds.Width, screenBounds.Height);

            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
            }

            return screenshot;
        }
    }
}

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
            int port = 12347;

            //Створити TcpListener для прийому підключень клієнтів
            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("File server started. Waiting for a connection...");

            //while (true)
            //{
            //    using (TcpClient client = listener.AcceptTcpClient())
            //    {

            //        // receive full filename from client
            //        // if file exists
            //        //  send file to client
            //        // close connection

            //    }
            //}


            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    Console.WriteLine("Client connected to file server");

                    // Отримати повне ім'я файлу від клієнта
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileNameBuffer = new byte[1024];
                        int bytesRead = stream.Read(fileNameBuffer, 0, fileNameBuffer.Length);
                        string fileName = Encoding.UTF8.GetString(fileNameBuffer, 0, bytesRead);

                        // Перевірити наявність файлу
                        if (File.Exists(fileName))
                        {
                            // Якщо файл існує, відправити його клієнту
                            using (FileStream fileStream = File.OpenRead(fileName))
                            {
                                byte[] fileBuffer = new byte[1024];
                                int bytesReadFile;
                                while ((bytesReadFile = fileStream.Read(fileBuffer, 0, fileBuffer.Length)) > 0)
                                {
                                    stream.Write(fileBuffer, 0, bytesReadFile);
                                }
                            }
                        }
                        else
                        {
                            // Якщо файл не існує, можна відправити повідомлення про помилку
                            string errorMessage = "File not found.";
                            byte[] errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                            stream.Write(errorBuffer, 0, errorBuffer.Length);
                        }
                    }

                    client.Close();
                }
            }

        }
        static void TreeServer(string ip)
        {
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            int port = 12346;

            //Створити TcpListener для прийому підключень клієнтів
            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("Tree server started. Waiting for a connection...");

            //Обробка підключення має включати логіку для відправки інформації про каталоги клієнту.
            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    Console.WriteLine("Client connected to tree server");

                    var root = TreeHelper.CreateTree(
                        withFiles: true,
                        serverFolderPath: @"E:\!STUDY");
                    //var root = CreateTree(@"E:\!STUDY");

                    // Серіалізація на сервері

                    string json = JsonConvert.SerializeObject(root);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    client.GetStream().Write(data, 0, data.Length);
                    Console.WriteLine("Tree data sent");

                    client.Close();

                    Console.WriteLine("Client disconnected");
                }
            }
        }

        static void Main(string[] args)
        {
            string ip = "0.0.0.0";

            Task.WaitAll(new Task[]
            {
                Task.Factory.StartNew(() => ScreenServer(ip)),
                Task.Factory.StartNew(() => TreeServer(ip)),
                Task.Factory.StartNew(() => FileServer(ip)),
            });


        }

        static void ScreenServer(string ip)
        {
            IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
            int port = 12345;

            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("Screen server started. Waiting for a connection...");

            using (Bitmap screenCapture = CaptureScreen())

            using (TcpClient client = listener.AcceptTcpClient())
            {
                Console.WriteLine("Client connected to screen server");

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

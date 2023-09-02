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

                var root = CreateTree(@"C:\Users\kvvkv\source\repos");

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

        private static TreeNodeData CreateTree(string serverFolderPath)
        {

            var rootDir = new DirectoryInfo(serverFolderPath);

            var rootNode = new TreeNodeData
            {
                Name = rootDir.Name,
                FullName = rootDir.FullName,
            };

            PopulateTreeNodeData(rootNode, rootDir);
            
            return rootNode;    
        }
        //public static TreeNodeData ConvertToTreeNodeData(TreeNode treeNode)
        //{
        //    TreeNodeData dataNode = new TreeNodeData();
        //    dataNode.Text = treeNode.Text;

        //    foreach (TreeNode childNode in treeNode.Nodes)
        //    {
        //        dataNode.Nodes.Add(ConvertToTreeNodeData(childNode));
        //    }

        //    return dataNode;
        //}

        private static void PopulateTreeNodeData(TreeNodeData parentNode, DirectoryInfo directoryInfo)
        {
            try
            {
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    var directoryNode = new TreeNodeData()
                    {
                        Name = directory.Name,
                        FullName = directory.FullName,
                        IsDirectory = true,
                    };

                    parentNode.Nodes.Add(directoryNode);
                    PopulateTreeNodeData(directoryNode, directory);
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    var fileNode = new TreeNodeData()
                    {
                        Name = file.Name,
                        FullName= file.FullName,
                        IsDirectory = false,
                    };

                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Обробка помилок доступу
            }
        }

        //private static void PopulateTreeView(TreeNode parentNode, DirectoryInfo directoryInfo)
        //{
        //    try
        //    {
        //        foreach (var directory in directoryInfo.GetDirectories())
        //        {
        //            TreeNode directoryNode = new TreeNode(directory.Name);
        //            parentNode.Nodes.Add(directoryNode);
        //            PopulateTreeView(directoryNode, directory);
        //        }

        //        foreach (var file in directoryInfo.GetFiles())
        //        {
        //            TreeNode fileNode = new TreeNode(file.Name);
        //            parentNode.Nodes.Add(fileNode);
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // Обробка помилок доступу
        //    }
        //}
    }
}

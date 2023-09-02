using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Newtonsoft.Json;
using CommonLibrary;

namespace Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button_ConnectToIP_Click(object sender, EventArgs e)
        {
            string serverIp = textBox_IP.Text;
            int serverPort = 12345;

            //using (TcpClient client = new TcpClient(serverIp, serverPort))
            //{
            //    var stream = client.GetStream();
            //    while (true)
            //    {
            //        // Send image size in bytes
            //        byte[] imageSizeData = new byte[sizeof(long)];

            //        int read = stream.Read(imageSizeData, 0, imageSizeData.Length);
            //        long imageSize = BitConverter.ToInt64(imageSizeData, 0);
            //        stream.Read(imageSizeData, 0, imageSizeData.Length);

            //        byte[] buffer = new byte[imageSize];
            //        using (MemoryStream memoryStream = new MemoryStream())
            //        {
            //            long bytesReadTotal = 0;
            //            while (bytesReadTotal < imageSize)
            //            {
            //                int readBytes = stream.Read(buffer, (int)bytesReadTotal, (int)imageSize - (int)bytesReadTotal);
            //                if (readBytes == 0) { break; }
            //                bytesReadTotal += readBytes;    
            //            }

            //            memoryStream.Write(buffer, 0, (int)imageSize);


            //            Bitmap receivedImage = new Bitmap(memoryStream);

            //        }
            //    }
            //}

            Task.Run(() =>
            {

                using (TcpClient client = new TcpClient(serverIp, serverPort))
                {
                    var stream = client.GetStream();
                    while (true)
                    {
                        // Read image size in bytes
                        byte[] imageSizeData = new byte[sizeof(long)];
                        stream.Read(imageSizeData, 0, imageSizeData.Length);
                        long imageSize = BitConverter.ToInt64(imageSizeData, 0);

                        byte[] buffer = new byte[imageSize];
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            long bytesReadTotal = 0;
                            while (bytesReadTotal < imageSize)
                            {
                                int readBytes = stream.Read(buffer, (int)bytesReadTotal, (int)imageSize - (int)bytesReadTotal);
                                if (readBytes == 0) { break; }
                                bytesReadTotal += readBytes;
                            }

                            memoryStream.Write(buffer, 0, buffer.Length);
                            memoryStream.Seek(0, SeekOrigin.Begin); // Reset position before creating the image

                            Bitmap receivedImage = new Bitmap(System.Drawing.Image.FromStream(memoryStream)); // Create the image

                            // Update the PictureBox
                            UpdatePictureBoxImage(receivedImage);
                        }
                    }
                }
            });



        }

        private void UpdatePictureBoxImage(Bitmap image)
        {
            if (pictureBox1.InvokeRequired)
            {
                pictureBox1.Invoke((Action)(() =>
                {
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose();
                    }
                    pictureBox1.Image = image;

                    pictureBox1.Update();
                }
                ));

            }
            else
            {
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                pictureBox1.Image = image;

                pictureBox1.Update();
            }
        }

        private void button_CheckDir_Click(object sender, EventArgs e)
        {
            string serverIp = textBox_IP.Text;
            int serverPort = 12346;

            using (TcpClient client = new TcpClient(serverIp, serverPort))
            {
                var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int read;

                // Відправка запиту на отримання інформації про каталоги
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memoryStream.Write(buffer, 0, read);
                    }
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    // Отримайте json з сервера, наприклад, через мережу
                    var treeData = memoryStream.GetBuffer();
                    var json = Encoding.UTF8.GetString(treeData);

                    // Десеріалізуйте json назад у об'єкт TreeNodeData
                    var root = JsonConvert.DeserializeObject<TreeNodeData>(json);

                    // Створіть дерево з отриманих даних
                    TreeNode receivedTree = ConvertToTreeNode(root);

                    // Відобразіть дерево на клієнтському інтерфейсі
                    treeView1.Nodes.Add(receivedTree);


                }

            }
        }

        public static TreeNode ConvertToTreeNode(TreeNodeData data)
        {
            TreeNode treeNode = new TreeNode(data.Name);
            treeNode.Tag = data;

            if (data.Nodes != null)
            {
                foreach (TreeNodeData childData in data.Nodes)
                {
                    treeNode.Nodes.Add(ConvertToTreeNode(childData));
                }
            }

            return treeNode;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var data = e.Node.Tag as TreeNodeData;
            MessageBox.Show((data.IsDirectory ? "DIR:" : "FILE: ") + data.FullName);
        }
    }



}

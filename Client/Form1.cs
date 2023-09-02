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
            //MessageBox.Show((data.IsDirectory ? "DIR:" : "FILE: ") + data.FullName);
            toolStripStatusLabel1.Text = data.FullName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var root = TreeHelper.CreateTree(
                   withFiles: false,
                   serverFolderPath: @"C:\Users\kvvkv\source\repos");

                // Створіть дерево з отриманих даних
                TreeNode receivedTree = ConvertToTreeNode(root);

                // Відобразіть дерево на клієнтському інтерфейсі
                treeView2.Nodes.Add(receivedTree);
            });

        }

        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var data = e.Node.Tag as TreeNodeData;
            //MessageBox.Show((data.IsDirectory ? "DIR:" : "FILE: ") + data.FullName);
            toolStripStatusLabel2.Text = data.FullName;
        }


        private void ReceiveDirectory(string ip, int port, TreeNodeData root, string localDir)
        {
            var dirName = Path.Combine(localDir, root.Name);
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            if (root.Nodes != null)
            {
                foreach (TreeNodeData childData in root.Nodes)
                {
                    if (childData.IsDirectory)
                    {
                        //Dir
                        ReceiveDirectory(ip, port, childData, dirName);
                    }
                    else
                    {
                        //File
                        string localFile = Path.Combine(dirName, Path.GetFileName(childData.FullName));
                        ReceiveFile(ip, port, childData.FullName, localFile);
                    }
                }
            }
        }

        private void ReceiveFile(string serverIp, int serverPort, string serverFile, string localFile)
        {
            using (TcpClient client = new TcpClient(serverIp, serverPort))
            using (NetworkStream stream = client.GetStream())
            {
                // Відправити повне ім'я файлу на сервер
                byte[] fileNameBuffer = Encoding.UTF8.GetBytes(serverFile);
                stream.Write(fileNameBuffer, 0, fileNameBuffer.Length);

                // Отримати вміст файлу від сервера і зберегти його локально
                using (FileStream localFileStream = File.Create(localFile))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        localFileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        private void button_Receive_Click(object sender, EventArgs e)
        {

            string serverIp = textBox_IP.Text;
            int serverPort = 12347;


            var selectedNodeLeft = treeView1.SelectedNode;
            var selectedNodeRight = treeView2.SelectedNode;

            if (selectedNodeLeft == null || selectedNodeRight == null)
            {
                return;
            }

            var selectedNodeLeftData = selectedNodeLeft.Tag as TreeNodeData;
            var selectedNodeRightData = selectedNodeRight.Tag as TreeNodeData;

            if (selectedNodeLeftData.IsDirectory)
            {
                //Directory

                string localDir = selectedNodeRightData.FullName;
                ReceiveDirectory(serverIp, serverPort, selectedNodeLeftData, localDir);
                MessageBox.Show("Directory received");

            }
            else
            {
                //File 
                string serverFile = selectedNodeLeftData.FullName;
                string localDir = selectedNodeRightData.FullName;
                string localFile = Path.Combine(localDir, Path.GetFileName(serverFile));

                // Підключення до сервера
                ReceiveFile(serverIp, serverPort, serverFile, localFile);
                MessageBox.Show("File received");

            }




        }
    }



}

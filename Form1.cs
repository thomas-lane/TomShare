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
using System.Threading;

namespace TomShare {
    public partial class Form1 : Form {
        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();
        private OpenFileDialog openFile = new OpenFileDialog();
        private Thread sendThread;

        public Form1() {
            InitializeComponent();
            folderDialog.Description = "Select the directory to receive files in";
            folderDialog.ShowDialog();
            txtDirectory.Text = folderDialog.SelectedPath;
            openFile.Title = "Select file(s) to send";
            openFile.Multiselect = true;
            StartListener();
        }

        private void StartListener() {
            TcpListener listener = new TcpListener(System.Net.IPAddress.Any, 50123);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), listener);
        }

        public void AcceptClient(IAsyncResult ar) {
            TcpListener listener = (TcpListener) ar.AsyncState;

            TcpClient client = listener.EndAcceptTcpClient(ar);

            //BinaryReader reader = new BinaryReader(client.GetStream());
            NetworkStream stream = client.GetStream();

            string fileName = "";
            char readChar = ' ';
            while((readChar = (char) stream.ReadByte()) != '\n') {
                fileName += readChar;
            }

            FileStream file = File.Create(folderDialog.SelectedPath + '\\' + fileName);

            int readByte = 0;
            while((readByte = stream.ReadByte()) != -1) {
                file.WriteByte((byte) readByte);
            }

            file.Flush();

            file.Close();

            client.Close();
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), listener);
            MessageBox.Show("Received file: " + fileName);
        }

        private void SendFile(string address, string fileName) {
            TcpClient client = new TcpClient();

            client.Connect(address, 50123);

            string name = fileName;

            int index = name.Length - 1;

            while (name[index] != '\\')  {
                index--;
            }

            string nameFormatted = "";
            for (int i = index + 1; i < name.Length; i++) {
                nameFormatted += name[i];
            }

            BinaryWriter writer = new BinaryWriter(client.GetStream());
            writer.Write(nameFormatted.ToCharArray());
            writer.Write('\n');
            writer.Flush();

            FileStream file = File.Open(name, FileMode.Open);

            int read = -1;
            while ((read = file.ReadByte()) != -1) {
                writer.Write((byte) read);
            }

            writer.Flush();

            client.Close();
            file.Close();
        }

        private void BtnDirectory_Click(object sender, EventArgs e) {
            if (folderDialog.ShowDialog() == DialogResult.OK) txtDirectory.Text = folderDialog.SelectedPath;
        }

        private void BtnAdd_Click(object sender, EventArgs e) {
            if(openFile.ShowDialog() == DialogResult.OK) {
                foreach(string file in openFile.FileNames) {
                    lstFiles.Items.Add(file);
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e) {
            if(lstFiles.SelectedIndex != -1) lstFiles.Items.RemoveAt(lstFiles.SelectedIndex);
        }

        private void BtnClear_Click(object sender, EventArgs e) {
            lstFiles.Items.Clear();
        }

        private void BtnSend_Click(object sender, EventArgs e) {
            if(sendThread != null) {
                if (sendThread.IsAlive) {
                    MessageBox.Show("Already sending", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            sendThread = new Thread(SendFiles);
            sendThread.Start();
        }

        private void SendFiles() {
            string[] files = new string[lstFiles.Items.Count];
            lstFiles.Items.CopyTo(files, 0);
            foreach(string file in files) {
                SendFile(txtAddress.Text, file);
            }
        }
    }
}

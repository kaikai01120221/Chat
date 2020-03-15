﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Chat
{
    public enum ChatMode
    {
        Server,
        Client
    }
    public delegate void ConnectStateEventHandler(bool connected);
    public delegate void SettingEventHandler(string localName, string remoteName, string ip);
    public partial class Form1 : Form
    {
        public ChatMode mode = ChatMode.Server;

        public string localName = "local";
        public string remoteName = "remote";
        //public string ip = "127.0.0.1";
        public string ip = "192.168.1.2";
        public int port = 30;
        private ChatBase chat;

        private Queue<string> selectFileQueue = new Queue<string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (mode == ChatMode.Server)
            {
                chat = new ChatServer(ip, port);
                this.Text = "S-Chat";
            }
            else
            {
                chat = new ChatClient(ip, port);
                this.Text = "C-Chat";
            }

            chat.OnReceive += delegate (ChatType type, string msg)
            {
                if (type == ChatType.File)
                {

                }
                else if (type == ChatType.Str)
                {
                    this.charContentRichText.Invoke(new OnReceiveEventHandler(OnReceiveMsg), type, msg);
                }
            };
            chat.OnConnect += delegate ()
            {
                this.Invoke(new ConnectStateEventHandler(SetIconByConnectState), true);
            };
            chat.OnDisconnect += delegate (Exception ex)
            {
                this.Invoke(new ConnectStateEventHandler(SetIconByConnectState), false);
            };
            chat.Start();
        }

        public void OnReceiveMsg(ChatType type, string msg)
        {
            SetTextValue(false, msg);
        }

        public void SetIconByConnectState(bool connected)
        {
            if (connected)
            {
                this.Icon = Properties.Resources.connected;
            }
            else
            {
                this.Icon = Properties.Resources.noconnected;
            }
        }

        public void SetTextValue(bool isLocal, string msg)
        {
            string name = this.remoteName;
            this.charContentRichText.SelectionAlignment = HorizontalAlignment.Right;
            if (isLocal)
            {
                name = this.localName;
                this.charContentRichText.SelectionAlignment = HorizontalAlignment.Left;
            }
            this.charContentRichText.AppendText(name + " : \n" + msg + "\n");
            this.charContentRichText.ScrollToCaret();
        }

        private void selectFileBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dia = new OpenFileDialog();
            dia.Multiselect = true;
            dia.Title = "请选择要发送的文件";
            dia.ShowDialog();
            OnSelectFiles(dia.FileNames);
        }

        private void OnSelectFiles(Array paths)
        {
            foreach (string path in paths)
            {
                if (path != null && path != "")
                {
                    this.selectFileQueue.Enqueue(path);
                    string name = Path.GetFileName(path);
                    this.sendTextBox.AppendText("[" + name + "]");
                }
            }
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            while (this.selectFileQueue.Count > 0)
            {
                string path = this.selectFileQueue.Dequeue();
                chat.SendFile(path);
            }
            string msg = this.sendTextBox.Text;
            msg = msg.TrimEnd();
            if (msg != "")
            {
                bool ret = chat.Send(msg);
                if (ret)
                {
                    this.sendTextBox.Text = "";
                    SetTextValue(true, msg);
                }
            }
        }

        private void onSetting(string localName, string remoteName, string ip)
        {
            this.localName = localName;
            this.remoteName = remoteName;
            chat.ip = ip;
        }

        private void settingBtn_Click(object sender, EventArgs e)
        {
            Form2 settingForm = new Form2(new SettingEventHandler(onSetting), this.localName, this.remoteName, chat.ip);
            settingForm.StartPosition = FormStartPosition.CenterParent;
            settingForm.SetIpEditEnable(mode == ChatMode.Client);
            settingForm.ShowDialog();
        }

        private void sendTextBox_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void sendTextBox_DragDrop(object sender, DragEventArgs e)
        {
            Array paths = ((Array)e.Data.GetData(DataFormats.FileDrop));
            OnSelectFiles(paths);
        }

        private void sendTextBox_DragEnter(object sender, DragEventArgs e)
        {
            Array paths = ((Array)e.Data.GetData(DataFormats.FileDrop));
            bool canDrag = true;
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    canDrag = false;
                }
            }
            if (canDrag)
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 10)
            {
                e.Handled = true;
                sendBtn_Click(null, null);
            }
        }
    }
}

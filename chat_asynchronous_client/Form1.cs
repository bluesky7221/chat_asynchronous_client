using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace chat_asynchronous_client
{
    public partial class ChatClientForm : Form
    {
        delegate void SetTextDelegate(string text);
        public ChatClientForm()
        {
            InitializeComponent();
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        //Name_label ��ȭ�� ǥ�� ��
        //Name_textBox ��ȭ�� �Է�â
        //Enter_Btn ���� �õ� ��ư
        //ChatBox ��ȭâ
        //Chat_EnterBox ��ȭ �Է�â
        //ChatClientForm ��ü ��
        Socket mainSocket;
        IPAddress thisAddress = IPAddress.Loopback;
        int port = 2022;
        int MAX_BYTE = 4096;

        //���� �õ� ��ư�� ������ �� �Լ�
        private void Enter_Btn_Click(object sender, EventArgs e)
        {
            if (Enter_Btn.Text == "����")
            {
                //�̹� �������̶��
                if (mainSocket.Connected)
                {
                    MessageBox.Show("socket connected");
                    return;
                }

                Enter_Btn.Text = "������";

                //���� ���� �õ�
                mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                IPEndPoint endPoint = new IPEndPoint(thisAddress, port);
                try
                {
                    mainSocket.Connect(endPoint);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("socket connect error : " + ex.Message);
                }

                //���� ���
                AsyncObject obj = new AsyncObject(MAX_BYTE);
                obj.WorkingSocket = mainSocket;
                mainSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            else
            {
                Enter_Btn.Text = "����";

                //���� ���� �޽����� ������ ������
                string s = string.Format("Ŭ���̾�Ʈ (@ {0})�� �������� �Ǿ����ϴ�.", mainSocket.RemoteEndPoint);
                //���ڿ��� utf8 ������ ����Ʈ�� ��ȯ
                byte[] data = Encoding.UTF8.GetBytes("server" + '\x01' + s);

                mainSocket.Send(data);
                //���� Ŭ����
                mainSocket.Close();
            }  
        }
        //�����͸� ������ ���� �޴� �ݹ� �Լ�
        void DataReceived(IAsyncResult ar)
        {
            if (Enter_Btn.Text == "����")
            {
                return;
            }
            // BeginReceive���� �߰������� �Ѿ�� �����͸� AsyncObject �������� ��ȯ�Ѵ�
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // ������ ������ ������
            int received = obj.WorkingSocket.EndReceive(ar);

            // ���� �����Ͱ� ������(���������) ������
            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            // �ؽ�Ʈ�� ��ȯ�Ѵ�
            string text = Encoding.UTF8.GetString(obj.Buffer);

            // 0x01 �������� ¥����
            // tokens[0] - ���� ��� id
            // tokens[1] - ���� �޼���
            string[] tokens = text.Split('\x01');
            string id = tokens[0];
            string msg = tokens[1];
            //�ؽ�Ʈ �ڽ� �� ǥ��
            SetText(id + ": " + msg);
            SetText("\r\n");
            //���� ����ֱ�
            obj.ClearBuffer();
            //���� ���
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, MAX_BYTE, 0, DataReceived, obj);
        }
        //���� ä�� ������ Ǫ���ϴ� �Լ�
        private void Chat_EnterBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //enter Ű�� ������ ��
            if (e.KeyChar == 13)
            {
                //���� �۵����� �ƴϸ� ����
                if (!mainSocket.IsBound)
                {
                    MessageBox.Show("server is not working");
                    return;
                }
                string s = Chat_EnterBox.Text.Trim();
                //�� �� ����
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }
                //�� ���̵� ����
                string id = Name_textBox.Text.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                //���ڿ��� utf8 ������ ����Ʈ�� ��ȯ
                byte[] data = Encoding.UTF8.GetBytes(id + '\x01' + s);
                //������ ����
                mainSocket.Send(data);
                //���͹ڽ����� ���� ����
                Chat_EnterBox.Clear();
            }
        }

        public void SetText(string text)
        {
            if (this.ChatBox.InvokeRequired)
            {
                SetTextDelegate d = new SetTextDelegate(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.ChatBox.AppendText(text);
            }
        }

        public class AsyncObject
        {
            public byte[] Buffer;
            public Socket WorkingSocket;
            public readonly int BufferSize;
            public AsyncObject(int bufferSize)
            {
                BufferSize = bufferSize;
                Buffer = new byte[BufferSize];
            }

            public void ClearBuffer()
            {
                Array.Clear(Buffer, 0, BufferSize);
            }
        }

        private void ChatClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Enter_Btn.Text = "����";

            mainSocket.Close();
            Application.Exit();
        }
    }
}
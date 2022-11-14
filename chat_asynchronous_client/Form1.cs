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

        //Name_label 대화명 표시 라벨
        //Name_textBox 대화명 입력창
        //Enter_Btn 접속 시도 버튼
        //ChatBox 대화창
        //Chat_EnterBox 대화 입력창
        //ChatClientForm 전체 폼
        Socket mainSocket;
        IPAddress thisAddress = IPAddress.Loopback;
        int port = 2022;
        int MAX_BYTE = 4096;

        //접속 시도 버튼을 눌렀을 때 함수
        private void Enter_Btn_Click(object sender, EventArgs e)
        {
            if (Enter_Btn.Text == "입장")
            {
                //이미 연결중이라면
                if (mainSocket.Connected)
                {
                    MessageBox.Show("socket connected");
                    return;
                }

                Enter_Btn.Text = "나가기";

                //소켓 연결 시도
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

                //수신 대기
                AsyncObject obj = new AsyncObject(MAX_BYTE);
                obj.WorkingSocket = mainSocket;
                mainSocket.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
            }
            else
            {
                Enter_Btn.Text = "입장";

                //연결 해제 메시지를 서버에 보내기
                string s = string.Format("클라이언트 (@ {0})가 연결해제 되었습니다.", mainSocket.RemoteEndPoint);
                //문자열을 utf8 형식의 바이트로 변환
                byte[] data = Encoding.UTF8.GetBytes("server" + '\x01' + s);

                mainSocket.Send(data);
                //소켓 클로즈
                mainSocket.Close();
            }  
        }
        //데이터를 서버로 부터 받는 콜백 함수
        void DataReceived(IAsyncResult ar)
        {
            if (Enter_Btn.Text == "입장")
            {
                return;
            }
            // BeginReceive에서 추가적으로 넘어온 데이터를 AsyncObject 형식으로 변환한다
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            // 데이터 수신을 끝낸다
            int received = obj.WorkingSocket.EndReceive(ar);

            // 받은 데이터가 없으면(연결끊어짐) 끝낸다
            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            // 텍스트로 변환한다
            string text = Encoding.UTF8.GetString(obj.Buffer);

            // 0x01 기준으로 짜른다
            // tokens[0] - 보낸 사람 id
            // tokens[1] - 보낸 메세지
            string[] tokens = text.Split('\x01');
            string id = tokens[0];
            string msg = tokens[1];
            //텍스트 박스 글 표시
            SetText(id + ": " + msg);
            SetText("\r\n");
            //버퍼 비워주기
            obj.ClearBuffer();
            //수신 대기
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, MAX_BYTE, 0, DataReceived, obj);
        }
        //글을 채팅 서버에 푸시하는 함수
        private void Chat_EnterBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            //enter 키를 눌렀을 때
            if (e.KeyChar == 13)
            {
                //서버 작동중이 아니면 무시
                if (!mainSocket.IsBound)
                {
                    MessageBox.Show("server is not working");
                    return;
                }
                string s = Chat_EnterBox.Text.Trim();
                //빈 글 무시
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }
                //빈 아이디 무시
                string id = Name_textBox.Text.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                //문자열을 utf8 형식의 바이트로 변환
                byte[] data = Encoding.UTF8.GetBytes(id + '\x01' + s);
                //서버에 전송
                mainSocket.Send(data);
                //엔터박스에서 내용 삭제
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
            Enter_Btn.Text = "입장";

            mainSocket.Close();
            Application.Exit();
        }
    }
}
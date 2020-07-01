using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;

namespace ChatLab
{
    public partial class Form1 : Form
    {
        byte[] _messageBytes; //byte array of encoded message
        List<IPAddress> _friendsList = new List<IPAddress>(); //list of all ip addresses currently on send list
        readonly List<string> _names = new List<string>();
        readonly UdpClient client = new UdpClient(1666); //new UdpClient for send/receive functions
        IPAddress _sendToMe; //IPAddress which will be derived from user inputs
        
        public Form1()
        {
            InitializeComponent();          
        }

        //Occurs on form load
        //generates default inputs, formats columns
        //and invokes ReceiveMessage so IMs can be instantly received
        private void Form1_Load(object sender, EventArgs e)
        {
            //save me some test typing
            txtbxIP1.Text = "127";
            txtbxIP2.Text = "0";
            txtbxIP3.Text = "0";
            txtbxIP4.Text = "1";

            //format contact list
            ColumnHeader header = new ColumnHeader
            {
                Text = "Name",
                Width = (friendListView.Width - 10) / 2
            };
            friendListView.Columns.Add(header);
            ColumnHeader header2 = new ColumnHeader
            {
                Text = "IP Address",
                Width = (friendListView.Width - 10) / 2
            };
            friendListView.Columns.Add(header2);
            _friendsList.Add(new IPAddress(new byte[4] { 127, 0, 0, 1 }));
            _names.Add("Horse");
            DisplayIPS();

            //ensure IMs can be immediately rcvs
            _ = ReceiveMessage();
        }

        //occurs when user presses send button, compiles message from user input and 
        //sends to ip address from input
        private void BttnSend_Click(object sender, EventArgs e)
        {
            //parse an IPAddress from user inputs
            _sendToMe = new IPAddress(new byte[4] { byte.Parse(txtbxIP1.Text), byte.Parse(txtbxIP2.Text), byte.Parse(txtbxIP3.Text), byte.Parse(txtbxIP4.Text) });

            //if contact is not in contact list...
            if (!_friendsList.Contains(_sendToMe) && !_names.Contains(txtbxUserName.Text))
            {
                //add a new friend
                //_friendDic.Add(txtbxUserName.Text, _sendToMe);
                _friendsList.Add(_sendToMe);
                _names.Add(txtbxUserName.Text);
                DisplayIPS();
            }
            //generate byte array from user inputs
            GenerateMessage();
            //await new send/receives
            _ = ReceiveMessage();
            _ = SendMessage();
        }

        //private void GenerateMessage()
        //builds a message byte array from user supplied inputs
        //accepts - nothing
        //returns - nothing
        private void GenerateMessage()
        {
            string encodeMe; //final string message to be encoded
            string username; //string component representing username
            string message; //* * message
            string timeStamp; //* * timestamp

            //generate new byte array
            _messageBytes = new byte[20 + 19 + txtbxMessage.Text.Length + 1];

            //ensure user supplies themselves with a username
            if (nyName.Text == "")
            {
                MessageBox.Show("Error: Please enter username");
                return;
            }

            //reset message array (redundant)
            Array.Clear(_messageBytes, 0, _messageBytes.Length);

            //assign strings from user input
            username = nyName.Text;
            message = txtbxMessage.Text;
            timeStamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            //pad username for agreed upon formatting
            if (username.Length < 20)
            {
                for (int i = username.Length; i < 20; ++i)
                {
                    username += " ";
                }
            }

            //sanity checking
            encodeMe = username + timeStamp + message;
            Console.WriteLine(username + " : " + username.Length + " chars");
            Console.WriteLine(timeStamp + " : " + timeStamp.Length + " chars");
            Console.WriteLine(message + " : " + message.Length + " chars");
            Console.WriteLine(encodeMe + " (string to be encoded)");

            //encode the string as a byte array
            _messageBytes = Encoding.Unicode.GetBytes(encodeMe);

            //sanity checking
            Console.WriteLine(Encoding.Unicode.GetString(_messageBytes) + " (decoded bytes)");
        }

        //async Task ReceiveMessage()
        //asyncronous recieve, waits for a message to be received
        //accepts - nothing
        //returns - nothing
        async Task ReceiveMessage()
        {
            //wait for client to send message
            UdpReceiveResult x = await client.ReceiveAsync();

            byte[] newMsgBytes = new byte[x.Buffer.Length]; //byte array received from client

            //copy received buffer array to new array (redundant)
            for (int i = 0; i < x.Buffer.Length; ++i)
            {
                newMsgBytes[i] = x.Buffer[i];
            }

            //retrieve entire string from byte array
            string newMessageString = Encoding.Unicode.GetString(newMsgBytes);

            //regex to find instance of 2###-##-## ##:##:##
            Regex datex = new Regex(@"([2-5][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9])");

            //timestamp assumed formatted properly
            string timeStamp = newMessageString.Substring(20, 19);
            string name;
            string message;
            //if timestamp is formatted properly...
            if (datex.IsMatch(timeStamp))
            {
                //proceed as usual
                name = newMessageString.Substring(0, newMessageString.IndexOf(timeStamp));
                name = name.Trim();
                message = newMessageString.Substring(newMessageString.LastIndexOf(timeStamp) + timeStamp.Length);
                if (message.Length > 250)
                {
                    message = message.Substring(0, 250).ToString();
                }
            }
            else //otherwise...
            {   //test entire message, if successful...
                if (datex.IsMatch(newMessageString))
                {
                    //use timestamp to infer locations of name, message
                    MatchCollection mDates = datex.Matches(newMessageString);
                    timeStamp = mDates[0].ToString();
                    name = newMessageString.Substring(0, newMessageString.IndexOf(timeStamp));
                    name = name.Trim();
                    message = newMessageString.Substring(newMessageString.LastIndexOf(timeStamp) + timeStamp.Length);
                    if (message.Length > 250)
                    {
                        message = message.Substring(0, 250).ToString();
                    }
                }
                else//otherwise...
                {   //just try to find 2####-##-##
                    Regex datex2 = new Regex(@"([2-5][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9])");
                    //search entire message, if successful...
                    if (datex2.IsMatch(newMessageString))
                    {
                        //use timestamp to infer locations of name, message
                        MatchCollection mDates = datex2.Matches(newMessageString);
                        timeStamp = mDates[0].ToString();
                        name = newMessageString.Substring(0, newMessageString.IndexOf(timeStamp));
                        name = name.Trim();
                        message = newMessageString.Substring(newMessageString.LastIndexOf(timeStamp) + timeStamp.Length);
                        if (message.Length > 250)
                        {
                            message = message.Substring(0, 250).ToString();
                        }
                    }
                    else//otherwise...
                    {   //search for ##/##/##
                        Regex datex3 = new Regex(@"([0-9][0-9]-[0-9][0-9]-[0-9][0-9])");
                        //search entire message, if successful...
                        if (datex3.IsMatch(newMessageString))
                        {
                            //use timestamp to infer locations of name, message
                            MatchCollection mDates = datex3.Matches(newMessageString);
                            timeStamp = mDates[0].ToString();
                            name = newMessageString.Substring(0, newMessageString.IndexOf(timeStamp));
                            name = name.Trim();
                            message = newMessageString.Substring(newMessageString.LastIndexOf(timeStamp) + timeStamp.Length);
                            if (message.Length > 250)
                            {
                                message = message.Substring(0, 250).ToString();
                            }
                        }
                        else//otherwise...
                        {   //give up, use name (infered to be from index 0 -> first white space)
                            name = newMessageString.Substring(0, newMessageString.IndexOf(' '));
                            name = name.Trim();
                            //indicate unknown time of sending...
                            timeStamp = "unknown";
                            message = newMessageString.Substring(name.Length);
                            if (message.Length > 250)
                            {
                                message = message.Substring(0, 250).ToString();
                            }
                            //...but include time of receipt
                            message += " [Received at: " + DateTime.Now.ToString() + "]";
                        }
                    }
                }
            }

            //if address not included in friends list
            if (!_friendsList.Contains(x.RemoteEndPoint.Address))
            {
                //add to friends list
                _friendsList.Add(x.RemoteEndPoint.Address);
                _names.Add(name);
            }
            //otherwise
            else
            {
                //update contact username (in case they change it)
                int index = _friendsList.IndexOf(x.RemoteEndPoint.Address);
                if (_names[index] != name)
                    _names[index] = name;
            }
            //update contact display
            DisplayIPS();

            //add newline
            if (messageBoxMain.Text != "")
            {
                messageBoxMain.AppendText("\n");
            }

            //display new message in listview, color coded 
            messageBoxMain.SelectionStart = messageBoxMain.Text.Length;
            messageBoxMain.SelectionLength = 0;
            messageBoxMain.SelectionColor = Color.Green;
            messageBoxMain.AppendText($"[{timeStamp}]  ::  ");

            messageBoxMain.SelectionStart = messageBoxMain.Text.Length;
            messageBoxMain.SelectionLength = 0;
            messageBoxMain.SelectionColor = Color.Red;
            messageBoxMain.AppendText($"{name} Says : ");

            messageBoxMain.SelectionStart = messageBoxMain.Text.Length;
            messageBoxMain.SelectionLength = 0;
            messageBoxMain.SelectionColor = Color.Blue;
            messageBoxMain.AppendText(message);

            //call method, wait for new message
            _ = ReceiveMessage();
        }

        //async Task SendMessage()
        //asyncronous send, sends user generated message to each IP in friends list
        //accepts - nothing
        //receives - nothing
        async Task SendMessage()
        {
            //send message to each IP in friends list
            foreach(IPAddress addy in _friendsList)
            {
                await client.SendAsync(_messageBytes, _messageBytes.Length, addy.ToString(), 1666);
                Console.WriteLine(addy.ToString());
            }
        }

        //occurs when a new IP is added to friends list
        //updates DGV with new address
        public void DisplayIPS()
        {
            //reset IP list
            friendListView.Items.Clear();
            for (int i = 0; i < _names.Count; i++)
            {
                ListViewItem item = new ListViewItem(_names[i]);
                item.SubItems.Add(_friendsList[i].ToString());
                friendListView.Items.Add(item);
            }
        }

        //occurs when message is added to message display
        //ensures that messagebox scrolls to bottom of text
        private void MessageBoxMain_TextChanged(object sender, EventArgs e)
        {
            messageBoxMain.SelectionStart = messageBoxMain.Text.Length;
            messageBoxMain.ScrollToCaret();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientCsharpForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PInvokeNativePipeServer();
        }

        const int BUFFER_SIZE = 4096;  // 4 KB

        static string receiveMessage;

        void listenToSNMP()
        {
            bool bResult;
            /////////////////////////////////////////////////////////////////////
            // Create a named pipe.
            // 

            // Prepare the pipe name
            String strPipeName = String.Format(@"\\{0}\pipe\{1}",
                ".",                // Server name
                "ServerListenClient"        // Pipe name
                );

            // Prepare the security attributes

            IntPtr pSa = IntPtr.Zero;   // NULL
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();

            SECURITY_DESCRIPTOR sd;
            SecurityNative.InitializeSecurityDescriptor(out sd, 1);
            // DACL is set as NULL to allow all access to the object.
            SecurityNative.SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false);
            sa.lpSecurityDescriptor = Marshal.AllocHGlobal(Marshal.SizeOf(
                typeof(SECURITY_DESCRIPTOR)));
            Marshal.StructureToPtr(sd, sa.lpSecurityDescriptor, false);
            sa.bInheritHandle = false;              // Not inheritable
            sa.nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES));

            pSa = Marshal.AllocHGlobal(sa.nLength);
            Marshal.StructureToPtr(sa, pSa, false);

            // Create the named pipe.
            IntPtr hPipe = PipeNative.CreateNamedPipe(
                strPipeName,                        // The unique pipe name.
                PipeOpenMode.PIPE_ACCESS_DUPLEX,    // The pipe is bi-directional
                PipeMode.PIPE_TYPE_MESSAGE |        // Message type pipe 
                PipeMode.PIPE_READMODE_MESSAGE |    // Message-read mode 
                PipeMode.PIPE_WAIT,                 // Blocking mode is on
                PipeNative.PIPE_UNLIMITED_INSTANCES,// Max server instances
                BUFFER_SIZE,                        // Output buffer size
                BUFFER_SIZE,                        // Input buffer size
                PipeNative.NMPWAIT_USE_DEFAULT_WAIT,// Time-out interval
                pSa                                 // Pipe security attributes
                );

            if (hPipe.ToInt32() == PipeNative.INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Unable to create named pipe {0} w/err 0x{1:X}",
                    strPipeName, PipeNative.GetLastError());
                return;
            }
            Console.WriteLine("The named pipe, {0}, is created.", strPipeName);


            /////////////////////////////////////////////////////////////////////
            // Wait for the client to connect.
            // 

            Console.WriteLine("Waiting for the client's connection...");

            bool bConnected = PipeNative.ConnectNamedPipe(hPipe, IntPtr.Zero) ?
                true : PipeNative.GetLastError() == PipeNative.ERROR_PIPE_CONNECTED;

            if (!bConnected)
            {
                Console.WriteLine(
                    "Error occurred while connecting to the client: 0x{0:X}",
                    PipeNative.GetLastError());
                PipeNative.CloseHandle(hPipe);      // Close the pipe handle.
                return;
            }
            // A byte buffer of BUFFER_SIZE bytes. The buffer should be big 
            // enough for ONE request from a client.

            while (true)
            {
                string strMessage;
                byte[] bRequest = new byte[BUFFER_SIZE];    // Client -> Server
                uint cbBytesRead, cbRequestBytes;
                // Receive one message from the pipe.
                cbRequestBytes = BUFFER_SIZE;
                bResult = PipeNative.ReadFile(      // Read from the pipe.
                    hPipe,                          // Handle of the pipe
                    bRequest,                       // Buffer to receive data
                    cbRequestBytes,                 // Size of buffer in bytes
                    out cbBytesRead,                // Number of bytes read
                    IntPtr.Zero);                   // Not overlapped I/O

                if (!bResult/*Failed*/ || cbBytesRead == 0/*Finished*/)
                    Console.WriteLine("Read Failed or Finished!");

                // Unicode-encode the byte array and trim all the '\0' chars at 
                // the end.
                strMessage = Encoding.Unicode.GetString(bRequest).TrimEnd('\0');
                Console.WriteLine("Receives {0} bytes; Message: \"{1}\"",
                    cbBytesRead, strMessage);
            }

            PipeNative.FlushFileBuffers(hPipe);
            PipeNative.DisconnectNamedPipe(hPipe);
            PipeNative.CloseHandle(hPipe);
            Console.ReadKey();
        }
        void answerSNMP()
        {
            bool bResult;
            /////////////////////////////////////////////////////////////////////
            // Create a named pipe.
            // 

            // Prepare the pipe name
            String strAPipeName = String.Format(@"\\{0}\pipe\{1}",
                ".",                // Server name
                "ServerAnswerClient"        // Pipe name
                );

            // Prepare the security attributes

            IntPtr pSa = IntPtr.Zero;   // NULL
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();

            SECURITY_DESCRIPTOR sd;
            SecurityNative.InitializeSecurityDescriptor(out sd, 1);
            // DACL is set as NULL to allow all access to the object.
            SecurityNative.SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false);
            sa.lpSecurityDescriptor = Marshal.AllocHGlobal(Marshal.SizeOf(
                typeof(SECURITY_DESCRIPTOR)));
            Marshal.StructureToPtr(sd, sa.lpSecurityDescriptor, false);
            sa.bInheritHandle = false;              // Not inheritable
            sa.nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES));

            pSa = Marshal.AllocHGlobal(sa.nLength);
            Marshal.StructureToPtr(sa, pSa, false);

            // Create the named pipe.
            IntPtr aPipe = PipeNative.CreateNamedPipe(
                strAPipeName,                        // The unique pipe name.
                PipeOpenMode.PIPE_ACCESS_DUPLEX,    // The pipe is bi-directional
                PipeMode.PIPE_TYPE_MESSAGE |        // Message type pipe 
                PipeMode.PIPE_READMODE_MESSAGE |    // Message-read mode 
                PipeMode.PIPE_WAIT,                 // Blocking mode is on
                PipeNative.PIPE_UNLIMITED_INSTANCES,// Max server instances
                BUFFER_SIZE,                        // Output buffer size
                BUFFER_SIZE,                        // Input buffer size
                PipeNative.NMPWAIT_USE_DEFAULT_WAIT,// Time-out interval
                pSa                                 // Pipe security attributes
                );

            if (aPipe.ToInt32() == PipeNative.INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Unable to create named pipe {0} w/err 0x{1:X}",
                    strAPipeName, PipeNative.GetLastError());
                return;
            }
            Console.WriteLine("The named pipe, {0}, is created.", strAPipeName);


            /////////////////////////////////////////////////////////////////////
            // Wait for the client to connect.
            // 

            Console.WriteLine("Waiting for the client's connection...");

            bool bConnected = PipeNative.ConnectNamedPipe(aPipe, IntPtr.Zero) ?
                true : PipeNative.GetLastError() == PipeNative.ERROR_PIPE_CONNECTED;

            if (!bConnected)
            {
                Console.WriteLine(
                    "Error occurred while connecting to the client: 0x{0:X}",
                    PipeNative.GetLastError());
                PipeNative.CloseHandle(aPipe);      // Close the pipe handle.
                return;
            }

            string strMessage;
            string wrMessage = "Trap\0";

            byte[] bRequest = new byte[BUFFER_SIZE];// Client -> Server
            uint cbBytesRead, cbRequestBytes;
            byte[] bReply;                          // Server -> Client
            uint cbBytesWritten, cbReplyBytes;

            // Receive one message from the pipe.
            cbRequestBytes = BUFFER_SIZE;
            bResult = PipeNative.ReadFile(      // Read from the pipe.
                aPipe,                          // Handle of the pipe
                bRequest,                       // Buffer to receive data
                cbRequestBytes,                 // Size of buffer in bytes
                out cbBytesRead,                // Number of bytes read
                IntPtr.Zero);                   // Not overlapped I/O

            if (!bResult/*Failed*/ || cbBytesRead == 0/*Finished*/)
                Console.WriteLine("Read Failed or Finished!");

            // Unicode-encode the byte array and trim all the '\0' chars at 
            // the end.
            strMessage = Encoding.Unicode.GetString(bRequest).TrimEnd('\0');
            Console.WriteLine("Receives {0} bytes; Message: \"{1}\"",
                cbBytesRead, strMessage);
            AppendTextBox(strMessage);

            // Prepare the response.

            // '\0' is appended in the end because the client may be a native
            // C++ program.

            bReply = Encoding.Unicode.GetBytes(wrMessage);
            cbReplyBytes = (uint)bReply.Length;

            // Write the response to the pipe.
            while (true)
            {
                //Thread.Sleep(5000);
                bResult = PipeNative.WriteFile(     // Write to the pipe.
                    aPipe,                          // Handle of the pipe
                    bReply,                         // Buffer to write to 
                    cbReplyBytes,                   // Number of bytes to write 
                    out cbBytesWritten,             // Number of bytes written 
                    IntPtr.Zero);                   // Not overlapped I/O 

                if (!bResult/*Failed*/ || cbReplyBytes != cbBytesWritten/*Failed*/)
                {
                    Console.WriteLine("WriteFile failed w/err 0x{0:X}",
                        PipeNative.GetLastError());
                    MessageBox.Show($"WriteFile failed w/err 0x{PipeNative.GetLastError()}");
                    break;
                }

                Console.WriteLine("Sends {0} bytes; Message: \"{1}\"",
                    cbBytesWritten, wrMessage.TrimEnd('\0'));
                Thread.Sleep(100);
            }

            /////////////////////////////////////////////////////////////////////
            // Flush the pipe to allow the client to read the pipe's contents 
            // before disconnecting. Then disconnect the pipe, and close the 
            // handle to this pipe instance.
            // 

            PipeNative.FlushFileBuffers(aPipe);
            PipeNative.DisconnectNamedPipe(aPipe);
            PipeNative.CloseHandle(aPipe);
            //Console.ReadKey();
        }
        
        Thread producer;
        Thread consumer;
        void PInvokeNativePipeServer()
        {
            /////////////////////////////////////////////////////////////////////
            // Read client requests from the pipe and write the response.
            // 

            //Thread producer = new Thread(() => listenToSNMP());
            //Thread consumer = new Thread(() => answerSNMP());

            producer = new Thread(new ThreadStart(listenToSNMP));
            consumer = new Thread(new ThreadStart(answerSNMP));

            producer.Start();
            consumer.Start();

            //producer.Join();   // Join both threads with no timeout
            //consumer.Join();   // Run both until done.
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            producer.Abort();
            consumer.Abort();
        }
        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            txtReceive.Text += value;
        }
    }
}

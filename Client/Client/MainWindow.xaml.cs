using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket sender;
        private IPEndPoint remoteEP;
        private object _lock;
        private Thread receiver;

        public MainWindow()
        {
            InitializeComponent();
            Title = "Client";
            btn_close.IsEnabled = false;
            Width = Height = 600;
            _lock = new object();
            receiver = new Thread(Receive);
        }
        private void Receive()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(50);
                    string data = "";
                    byte[] bytes;
                    while (!data.Contains("<EOF>"))
                    {
                        bytes = new byte[1 << 10];
                        int bytesRec;
                        lock (_lock)
                        {
                            //ricevo i byte e l'intero è il numero di byte nel buffer
                            bytesRec = sender.Receive(bytes);
                        }
                        //converto in ascii
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    }
                    //aggiorno la label
                    Dispatcher.Invoke((Action)(() =>
                    {
                        lbl_recieved.Content = data.Substring(0, data.Length - 5);
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERRORE", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_send_Click(object o, RoutedEventArgs e)
        {
            try
            {
                //codifico una stringa
                byte[] msg = Encoding.ASCII.GetBytes(txt_msg.Text + "<EOF>");

                //spedisco i dati e ricevo la risposta
                int bytesSent = sender.Send(msg);
            }
            catch (ArgumentNullException ane)
            {
                MessageBox.Show(ane.Message, "ArgumentNullException",MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message, "SocketException", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unexpected exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartClient()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                remoteEP = new IPEndPoint(ipAddress, 11000);

                sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //mi connetto alla porta
                sender.Connect(remoteEP);

                lbl_state.Content = "Socket connected to " + sender.RemoteEndPoint.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btn_close_Click(object o, RoutedEventArgs e)
        {
            //chiudo la socket
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            btn_start.IsEnabled = true;
            btn_close.IsEnabled = false;
            lbl_state.Content = null;
        }

        private void btn_start_Click(object o, RoutedEventArgs e)
        {
            StartClient();
            btn_start.IsEnabled = false;
            btn_close.IsEnabled = true;
            receiver.Start();
        }
    }
}

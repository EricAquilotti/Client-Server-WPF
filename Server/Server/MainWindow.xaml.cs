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
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket handler;
        private IPEndPoint localEP;
        private Thread receiver;
        private object _lock;
        
        public MainWindow()
        {
            InitializeComponent();
            Title = "Server";
            _lock = new object();
            btn_close.IsEnabled = false;
            Width = Height = 600;


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
                            bytesRec = handler.Receive(bytes);
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
            catch(Exception ex)
            {
                MessageBox.Show("ERRORE", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartServer()
        {
            //il localhost è 127.0.0.1
            IPHostEntry host = Dns.GetHostEntry("localhost");

            IPAddress ipAddress = host.AddressList[0];

            localEP = new IPEndPoint(ipAddress, 11000);

            try
            {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //associo la socket all'endpoint
                listener.Bind(localEP);

                //può soddisfare 10 richieste alla volta
                listener.Listen(10);

                handler = listener.Accept();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERRORE", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_start_Click(object o, RoutedEventArgs e)
        {
            StartServer();
            receiver = new Thread(Receive);
            receiver.Start();
            btn_start.IsEnabled = false;
            btn_close.IsEnabled = true;
        }

        private void btn_close_Click(object o, RoutedEventArgs e)
        {
            //chiudo la socket
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            receiver.Abort();
            btn_start.IsEnabled = true;
            btn_close.IsEnabled = false;
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //codifico una stringa
                byte[] msg = Encoding.ASCII.GetBytes(txt_msg.Text + "<EOF>");

                //spedisco i dati e ricevo la risposta
                int bytesSent = handler.Send(msg);
            }
            catch (ArgumentNullException ane)
            {
                MessageBox.Show(ane.Message, "ArgumentNullException", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}

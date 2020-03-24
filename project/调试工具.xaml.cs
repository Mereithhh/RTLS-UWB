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
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using System.Drawing;

namespace 串口调试助手
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SerialPort comm = new SerialPort();
        public static int sw = 0;
        public static string van;

        public MainWindow()
        {

            InitializeComponent();
            Init_conf();

        }
        private void Init_conf()
        {
            //查询主机上面存在的串口
            foreach (string items in SerialPort.GetPortNames())
                comboBox_port.Items.Add(items);

            if (comboBox_port.Items.Count > 0)
                comboBox_port.SelectedIndex = 0;
            else
                comboBox_port.IsEnabled = false;

            comboBox_bo.SelectedIndex = 2;
            comboBox_jiaoyan.SelectedIndex = 0;
            comboBox_shujuwei.SelectedIndex = 0;
            comboBox_stop.SelectedIndex = 0;
            button_a5.IsEnabled = false;
            radioButton_gb.IsChecked = true;
            //向事件注册一个方法，当事件发生，自动调用Com_DataRev
            comm.DataReceived += new SerialDataReceivedEventHandler(Com_DataRev);
            

        }

        private void Com_DataRev(object sender, SerialDataReceivedEventArgs e)
        {



                //开辟缓冲区
                byte[] ReDatas = new byte[comm.BytesToRead];
                //读取数据
                comm.Read(ReDatas, 0, ReDatas.Length);
                //读取数组
                //送入后续的处理

                if (sw == 1)
                {
                    StringBuilder door = new StringBuilder();
                    for (int i = 0; i < ReDatas.Length; i++)
                        door.AppendFormat("{0:x2}" + " ", ReDatas[i]);
                    van = (door.ToString().ToUpper());
                }
                else if (sw == 0)
                {
                    van = BytesToString(ReDatas);
                }
                else if (sw == 3)
                {
                    van = new UnicodeEncoding().GetString(ReDatas);
                }
                else if (sw == 2)
                {
                    van = new UTF8Encoding().GetString(ReDatas);
                }
                else if (sw == 4)
                {
                    van = new ASCIIEncoding().GetString(ReDatas);
                }
                else
                {
                    van = BytesToString(ReDatas);
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox_rev.AppendText(van);
                    textBox_rev.ScrollToEnd();
                }));
                comm.DiscardInBuffer();
                // textBox_rev.AppendText("boy next door");
        }

        private string BytesToString(byte[] Bytes)
        { 
                Encoding FromEcoding = Encoding.GetEncoding("GB2312");
                Encoding ToEcoding = Encoding.GetEncoding("UTF-8");
                byte[] ToBytes = Encoding.Convert(FromEcoding, ToEcoding, Bytes);
            return ToEcoding.GetString(ToBytes);
        }

        //下面是串口开关
        

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_port.Items.Count <= 0)
            {
                MessageBox.Show("未发现可用串口设备，请检查硬件！");
                return;
            }

            if (comm.IsOpen == false)
            {
                comm.PortName = comboBox_port.SelectionBoxItem.ToString();
                comm.BaudRate = Convert.ToInt32(comboBox_bo.SelectionBoxItem.ToString());
                comm.Parity = (Parity)Convert.ToInt32(item_none.Tag.ToString());
                comm.DataBits = Convert.ToInt32(comboBox_shujuwei.SelectionBoxItem.ToString());
                comm.StopBits = (StopBits)Convert.ToInt32(comboBox_stop.SelectionBoxItem.ToString());
                try
                {
                    comm.Open();
                    button_a5.IsEnabled = true;
                    button1.Content = "关闭";
                    image.Source = new BitmapImage(new Uri(@"回头.png", UriKind.Relative));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "未能成功开启串口", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }          
            }
            else
            {
                try
                {
                    //close port
                    comm.Close();
                    button_a5.IsEnabled = false;
                    button1.Content = "开启";
                    image.Source = new BitmapImage(new Uri(@"扭头.png", UriKind.Relative));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "串口关闭错误！", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            comboBox_port.IsEnabled = !comm.IsOpen;
            comboBox_bo.IsEnabled = !comm.IsOpen;
            comboBox_shujuwei.IsEnabled = !comm.IsOpen;
            comboBox_jiaoyan.IsEnabled = !comm.IsOpen;
            comboBox_stop.IsEnabled = !comm.IsOpen;

        }

        private void Button_a5_click(object sender, RoutedEventArgs e)
        {
            if (textBox_send.Text.Length > 0)
                textBox_send.AppendText("\n");

            byte[] sendData ;

            if (radioButton_16.IsChecked == true)
            {
                sendData = StrToHexByte(textBox_send.Text.Trim());
            }
            else if (radioButton_an.IsChecked == true)
                sendData = Encoding.ASCII.GetBytes(textBox_send.Text.Trim());
            else if (radioButton_ut.IsChecked == true)
                sendData = Encoding.UTF8.GetBytes(textBox_send.Text.Trim());
            else
                sendData = Encoding.Unicode.GetBytes(textBox_send.Text.Trim());
            SSenddata(sendData);


        }

        public bool SSenddata(byte[] data)
        {
            if (comm.IsOpen==true)
            {
                try
                {
                    //把消息传递给串口
                    comm.Write(data, 0, data.Length);
                    return true;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "发送失败！", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("串口未开启", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
        private byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0) hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++) 
                returnBytes[i]=Convert.ToByte(hexString.Substring(i*2,2).Replace(" ",""),16);
            return returnBytes;
        }

        private void Radio_16_checked(object sender, RoutedEventArgs e)
        {
            sw = 1;
        }

        private void Radio_an_checked(object sender, RoutedEventArgs e)
        {
            sw = 4;
        }

        private void Radio_ut_checked(object sender, RoutedEventArgs e)
        {
            sw = 2;
        }

        private void Radio_un_checked(object sender, RoutedEventArgs e)
        {
            sw = 3;
        }

        private void Radio_gb_checked(object sender, RoutedEventArgs e)
        {
            sw = 0;
        }

        private void Button_clr_clicked(object sender, RoutedEventArgs e)
        {
            textBox_rev.Clear();
        }

        private void Button_a0_clicked(object sender, RoutedEventArgs e)
        {

            SSenddata(Encoding.ASCII.GetBytes("AT+SW=10010000\n"));
            RoutedEventArgs temp = new RoutedEventArgs();
            Button1_Click(null, temp);

        }

        private void Button_a1_clicked(object sender, RoutedEventArgs e)
        {
            SSenddata(Encoding.ASCII.GetBytes("AT+SW=10010010\n"));
            RoutedEventArgs temp = new RoutedEventArgs();
            Button1_Click(null, temp);
            System.Threading.Thread.Sleep(2000);
            Button1_Click(null, temp);
        }

        private void Button_a2_clicked(object sender, RoutedEventArgs e)
        {
            SSenddata(Encoding.ASCII.GetBytes("AT+SW=10010100\n"));
            RoutedEventArgs temp = new RoutedEventArgs();
            Button1_Click(null, temp);
            System.Threading.Thread.Sleep(2000);
            Button1_Click(null, temp);
        }

        private void Button_a3_clicked(object sender, RoutedEventArgs e)
        {
            SSenddata(Encoding.ASCII.GetBytes("AT+SW=10010110\n"));
            RoutedEventArgs temp = new RoutedEventArgs();
            Button1_Click(null, temp);
            System.Threading.Thread.Sleep(2000);
            Button1_Click(null, temp);
        }

        private void Button_a4_clicked(object sender, RoutedEventArgs e)
        {
            SSenddata(Encoding.ASCII.GetBytes("AT+SW=10000000\n"));
            RoutedEventArgs temp = new RoutedEventArgs();
            Button1_Click(null, temp);
            System.Threading.Thread.Sleep(2000);
            Button1_Click(null, temp);
        }




        private void Button_van_clicked(object sender, RoutedEventArgs e)
        {
            Window1 w1 = new Window1();
            w1.Show();
        }
    }
}

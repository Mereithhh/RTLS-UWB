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
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using Microsoft.VisualBasic;


namespace 串口调试助手
{
    /// <summary>
    /// Window2.xaml 的交互逻辑
    /// </summary>
    public partial class Window2 : Window
    {
        public static string van;
        private readonly SerialPort comm = new SerialPort();
        public static System.Timers.Timer t = new System.Timers.Timer(500);
        //全局变量

        public static double ddist0, ddist1, ddist2, ddist3;// = 19.16, dist1 = 15.07, dist2 = 17.13, dist3 = 16.02;
        public static double[] station0 = new double[3], station1 = new double[3], station2 = new double[3], station3 = new double[3];
        //设置的需要的参数
        public static double[,] para1 = new double[3, 3];
        public static double[] para2 = new double[3];
        public static double[,] station = new double[4, 3];
        public static double[] ddist = new double[4];
        public static double[] position_XYZ = new double[3];
        public static int time_without_comm=0;

        public double fix=0.45;
        public double bili = 50;   //83.333
        public double fix_z=0;
        public int flash_time = 500;
        public bool factory_mode = false;
        public int factory_t = 0;
        public bool full_data = false;
        public int full_data_flag = 0;

        //这老哥，能读取基站坐标，并转换成需要的坐标，画在图上面。
        public void Read_a()
        {
            bili = Convert.ToDouble(textBox.Text);
            if (a0_x.Text=="")
            {
                MessageBox.Show("请填入基站坐标！");
                return ; 
            }

            station0[0] = Convert.ToDouble(a0_x.Text);
            station0[1] = Convert.ToDouble(a0_y.Text);
            station0[2] = Convert.ToDouble(a0_z.Text);

            station1[0] = Convert.ToDouble(a1_x.Text);
            station1[1] = Convert.ToDouble(a1_y.Text);
            station1[2] = Convert.ToDouble(a1_z.Text);

            station2[0] = Convert.ToDouble(a2_x.Text);
            station2[1] = Convert.ToDouble(a2_y.Text);
            station2[2] = Convert.ToDouble(a2_z.Text);

            station3[0] = Convert.ToDouble(a3_x.Text);
            station3[1] = Convert.ToDouble(a3_y.Text);
            station3[2] = Convert.ToDouble(a3_z.Text);

            for (int i = 0; i < 3; i++)
            {
                station[0, i] = station0[i];
                station[1, i] = station1[i];
                station[2, i] = station2[i];
                station[3, i] = station3[i];
            }

            image_a0.Margin = Trans_local(station0);
            image_a1.Margin = Trans_local(station1);
            image_a2.Margin = Trans_local(station2);
            image_a3.Margin = Trans_local(station3);

            image_a0.Source = new BitmapImage(new Uri(@"A0.png", UriKind.Relative));
            image_a1.Source = new BitmapImage(new Uri(@"A1.png", UriKind.Relative));
            image_a2.Source = new BitmapImage(new Uri(@"A2.png", UriKind.Relative));
            image_a3.Source = new BitmapImage(new Uri(@"A3.png", UriKind.Relative));
        }



        public void Caculate_Para(double[] d, double[,] sta)
        {
            for (int i = 1; i < 4; i++)
            {
                para2[i - 1] = ddist[i] * ddist[i] - ddist[0] * ddist[0] - station[i, 0] * station[i, 0] - station[i, 1] * station[i, 1] - station[i, 2] * station[i, 2];
                for (int j = 0; j < 3; j++)
                    para1[i - 1, j] = -2 * station[i, j];
            }

        }
        public double[,] Trans(double[,] para)
        {
            double[,] para_T = new double[3, 3];
            //           para_T =(double[,])malloc(3 * sizeof(double[]));
            //           for (int i = 0; i < 3; i++)
            //              para_T[i] = (double*)malloc(3 * sizeof(double));
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    para_T[j, i] = para[i, j];
            return para_T;
        }
        //求解行列式的值
        public double Para_Abs(double[,] para)
        {
            double a;
            a = para[0, 0] * (para[1, 1] * para[2, 2] - para[1, 2] * para[2, 1])
             - para[0, 1] * (para[1, 0] * para[2, 2] - para[1, 2] * para[2, 0])
             + para[0, 2] * (para[1, 0] * para[2, 1] - para[1, 1] * para[2, 0]);
            return a;
        }
        //两个3x3的矩阵相乘
        public double[,] Multi(double[,] A, double[,] B)
        {
            double[,] C = new double[3, 3];
            //           C = (double[,])malloc(3 * sizeof(double*));
            //           for (int i = 0; i < 3; i++)
            //               C[i] = (double*)malloc(3 * sizeof(double));
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    C[i, j] = A[i, 0] * B[0, j] + A[i, 1] * B[1, j] + A[i, 2] * B[2, j];
            return C;
        }
        //求解3*3矩阵的伴随；
        public double[,] company(double[,] para)
        {
            double[,] para_company = new double[3, 3];
            // para_company = (double[,])malloc(3 * sizeof(double*));
            //for (int i = 0; i < 3; i++)
            //     para_company[i] = (double*)malloc(3 * sizeof(double));
            para_company[0, 0] = para[1, 1] * para[2, 2] - para[1, 2] * para[2, 1];
            para_company[0, 1] = -1 * (para[1, 0] * para[2, 2] - para[1, 2] * para[2, 0]);
            para_company[0, 2] = para[1, 0] * para[2, 1] - para[1, 1] * para[2, 0];
            para_company[1, 0] = -1 * (para[0, 1] * para[2, 2] - para[0, 2] * para[2, 1]);
            para_company[1, 1] = para[0, 0] * para[2, 2] - para[0, 2] * para[2, 0];
            para_company[1, 2] = -1 * (para[0, 0] * para[2, 1] - para[0, 1] * para[2, 0]);
            para_company[2, 0] = para[0, 1] * para[1, 2] - para[0, 2] * para[1, 1];
            para_company[2, 1] = -1 * (para[0, 0] * para[1, 2] - para[0, 2] * para[1, 0]);
            para_company[2, 2] = para[1, 1] * para[0, 0] - para[0, 1] * para[1, 0];
            para_company = Trans(para_company);
            return para_company;
        }
        //求解3*3矩阵的逆；
        public double[,] Ni(double[,] para)
        {
            double[,] para_ni = new double[3, 3];
            double[,] para_company = new double[3, 3];
            para_company = company(para);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    para_ni[i, j] = (1 / (Para_Abs(para))) * para_company[i, j];
            return para_ni;
        }

        public void Caculate_Position()
        {
            double[,] para1_T = Trans(para1);
            double[,] para1_ni = Ni(para1);
            double para1_abs = Para_Abs(para1);
            double[,] para1_company = company(para1);
            double[,] tool_xxx = new double[3, 3], tool_xx = new double[3, 3], tool_x = new double[3, 3];
            double[] tool_y = new double[3];
            tool_xxx = Multi(para1_T, para1);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    tool_xx[i, j] = tool_xxx[i, j];
            ;
            tool_x = Ni(tool_xx);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    tool_xx[i, j] = tool_x[i, j];
            for (int i = 0; i < 3; i++)
                tool_y[i] = para1_T[i, 0] * para2[0] + para1_T[i, 1] * para2[1] + para1_T[i, 2] * para2[2];
            for (int i = 0; i < 3; i++)
                position_XYZ[i] = tool_xx[i, 0] * tool_y[0] + tool_xx[i, 1] * tool_y[1] + tool_xx[i, 2] * tool_y[2];
            ;
        }

        //按照比例尺转换坐标到margin坐标
        public Thickness Trans_local(double[] res)
        {
            Thickness trs = new Thickness();
            //都是83.333:1  ,宽对应的就是left,高对应的是500-top
            trs.Left = bili*res[0];
            trs.Top = 500 - bili*res[1];
            return trs;
        }

        public Window2()
        {
            InitializeComponent();
            Init_conf();
            
            

        }

        private void Button_map_clicked(object sender, RoutedEventArgs e)
        {

            Window3 about = new Window3();
            about.Show();

        }

        private void Button_apply_clicked(object sender, RoutedEventArgs e)
        {
            if(comm.IsOpen==true)
            {
                MessageBox.Show("请先关闭串口再应用设置！");
            }
            Read_a();
        }

        private void Init_conf()
        {
            //查询主机上面存在的串口
            foreach (string items in SerialPort.GetPortNames())
                comboBox.Items.Add(items);

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
            else
            {
                comboBox.IsEnabled = false;
                //button_open.IsEnabled = false;
            }
            //向事件注册一个方法，当事件发生，自动调用Com_DataRev
            comm.DataReceived += new SerialDataReceivedEventHandler(Com_DataRev);
            Read_a();


        }


        private void Com_DataRev(object sender, SerialDataReceivedEventArgs e)
        {

            //开辟缓冲区
            byte[] ReDatas = new byte[comm.BytesToRead];
            //读取数据
            comm.Read(ReDatas, 0, ReDatas.Length);
            //读取数组
            //送入后续的处理
            van = BytesToString(ReDatas);
            //显示到右面的实时状态中


            //检测有没有出场化设置

            if (van.Substring(5,1)=="T")
            {
                //comm.Close();
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    RoutedEventArgs door = new RoutedEventArgs();
                    Button_open_clicked(null, door);
                }));

                MessageBox.Show("系统未初始化角色！请在调试工具下方的命令中选择进行初始化！");
                return;
            }

            if (factory_mode == false)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {                 
                    double toolman;
                    string[] get = van.Split(' ');
                     if (get.Length!=6)
                    {
                        if (factory_t  <=1) factory_t+=1;
                        else
                        {

                        comm.Close();
                        t.Enabled = false;
                        RoutedEventArgs door = new RoutedEventArgs();
                        Button_open_clicked(null, door);
                        MessageBox.Show("不是指定帧格式！请点击原厂兼容!");
                        return;
                        }

                    }

                    toolman = Convert.ToDouble(get[1]);
                    toolman /= 1000;
                    if (toolman==0)
                    {
                        dist0.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[0] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist0.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[0] = toolman;
                    }

                    toolman = Convert.ToDouble(get[2]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist1.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[1] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist1.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[1] = toolman;
                    }


                    toolman = Convert.ToDouble(get[3]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist2.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[2] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist2.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[2] = toolman;
                    }

                    toolman = Convert.ToDouble(get[4]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist3.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[3] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist3.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[3] = toolman;
                    }
                    //看看数据是不是全了
                    if (full_data_flag == 1)
                        full_data = true;
                    else
                        full_data = false;

                }));
                //拆分加载到ddist数组；

           

            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {

                    double toolman;
                    string[] get = van.Split(' ');
                    //MessageBox.Show(get.Length.ToString());
                    if (get.Length != 19)
                    {
                        if (factory_t <= 0) factory_t += 1;
                        else
                        {
                        RoutedEventArgs door = new RoutedEventArgs();
                        Button_open_clicked(null, door);
                        MessageBox.Show("不是指定帧格式！请关闭原厂兼容!");
                            return;
                        }

                    }

                    toolman = Trans_str16_double(get[2]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist0.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[0] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist0.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[0] = toolman;
                    }

                    toolman = Trans_str16_double(get[3]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist1.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[1] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist1.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[1] = toolman;
                    }


                    toolman = Trans_str16_double(get[4]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist2.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[2] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist2.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[2] = toolman;
                    }

                    toolman = Trans_str16_double(get[5]);
                    toolman /= 1000;
                    if (toolman == 0)
                    {
                        dist3.Content = "0";
                        full_data = false;
                        full_data_flag = 0;
                        ddist[3] = 0;
                    }
                    else
                    {
                        toolman -= fix;
                        dist3.Content = Convert.ToString(toolman);
                        full_data_flag = 1;
                        ddist[3] = toolman;
                    }
                    //看看数据是不是全了
                    if (full_data_flag == 1)
                        full_data = true;
                    else
                        full_data = false;
                }));
         

            }
          

            //清除缓存标志位
            comm.DiscardInBuffer();

            // textBox_rev.AppendText("boy next door");
        }

        public  double Trans_str16_double(string str16)
        {
            int[] toolman=new int [8];
            int temp=0;
            for(int i =0;i<8;i++)
            {
                toolman[i] = Convert.ToInt32( str16.Substring(7-i,1)  ,16);
                temp += toolman[i]* Convert.ToInt32( Math.Pow(16,i));
            }
            return Convert.ToDouble(temp);
        }

        private void Button_bili_clicked(object sender, RoutedEventArgs e)
        {
            bili = Convert.ToDouble(textBox.Text);
            Read_a();
        }

        private void Button_fix_clicked(object sender, RoutedEventArgs e)
        {
            fix = Convert.ToDouble(textBox_fix.Text);
        }

        private void Button_cal_dist_clicked(object sender, RoutedEventArgs e)
        {
            double[] should_dist=new double[4];  //建立应该距离
            double[] get_dist = new double[3];    //从文本框得到坐标
            //string[] text_dist = new string[4];
            double toolman;
            get_dist[0]= Convert.ToDouble(textBox_x.Text);
            get_dist[1] = Convert.ToDouble(textBox_y.Text);
            get_dist[2] = Convert.ToDouble(textBox_z.Text);

            toolman = (get_dist[0] - station0[0]) * (get_dist[0] - station0[0])+ (get_dist[1] - station0[1]) * (get_dist[1] - station0[1])+ (get_dist[2] - station0[2]) * (get_dist[2] - station0[2]);
            should_dist[0] = Math.Sqrt(toolman);

            toolman = (get_dist[0] - station1[0]) * (get_dist[0] - station1[0]) + (get_dist[1] - station1[1]) * (get_dist[1] - station1[1]) + (get_dist[2] - station1[2]) * (get_dist[2] - station1[2]);
            should_dist[1] = Math.Sqrt(toolman);

            toolman = (get_dist[0] - station2[0]) * (get_dist[0] - station2[0]) + (get_dist[1] - station2[1]) * (get_dist[1] - station2[1]) + (get_dist[2] - station2[2]) * (get_dist[2] - station2[2]);
            should_dist[2] = Math.Sqrt(toolman);

            toolman = (get_dist[0] - station3[0]) * (get_dist[0] - station3[0]) + (get_dist[1] - station3[1]) * (get_dist[1] - station3[1]) + (get_dist[2] - station3[2]) * (get_dist[2] - station3[2]);
            should_dist[3] = Math.Sqrt(toolman);

            MessageBox.Show("A0的距离:     "+ should_dist[0].ToString("F3")+"\r\n"+ "A1的距离:     " + should_dist[1].ToString("F3") + "\r\n"+ "A2的距离:     " + should_dist[2].ToString("F3") + "\r\n"+ "A3的距离:     " + should_dist[3].ToString("F3") + "\r\n", "应该和四个基站的距离");

        }

        private void Button_factory_clicked(object sender, RoutedEventArgs e)
        {
            if (comm.IsOpen==true)
            {
                MessageBox.Show("请先关闭串口再进行操作！");
                return;
            }
            if (!factory_mode)
            {
                factory_mode = true;
                button_factory.Background = new SolidColorBrush(Colors.LightGreen);
                t.Enabled = true;
            }
            else
            {
                factory_mode = false;
                button_factory.Background = new SolidColorBrush(Colors.LightGray);
                t.Enabled = true;
            }

        }

        private void Button_fix_z_clicked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_flash_clicked(object sender, RoutedEventArgs e)
        {
            flash_time = Convert.ToInt32(textBox_updatetime.ToString());
            t.Interval = flash_time;    //更改计时器的刷新时间
        }

        private string BytesToString(byte[] Bytes)
        {
            Encoding FromEcoding = Encoding.GetEncoding("GB2312");
            Encoding ToEcoding = Encoding.GetEncoding("UTF-8");
            byte[] ToBytes = Encoding.Convert(FromEcoding, ToEcoding, Bytes);
            return ToEcoding.GetString(ToBytes);
        }

        //打开串口调试助手
        private void Button_port_clicked(object sender, RoutedEventArgs e)
        {
            MainWindow port1 = new MainWindow();
            port1.Show();
        }

        private void Button_open_clicked(object sender, RoutedEventArgs e)
        {
            if (comboBox.Items.Count <= 0)
            {
                MessageBox.Show("未发现可用串口设备，请检查硬件！");
                return;
            }

            if (comm.IsOpen == false)
            {

                comm.PortName = comboBox.SelectionBoxItem.ToString();
                comm.BaudRate = 115200;
                comm.Parity = (Parity)0;
                comm.DataBits = 8;
                comm.StopBits = (StopBits)1;
                try
                {
                    comm.Open();
                    button_open.Content = "关闭";
                    Run();
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
                    button_open.Content = "开启";
                    t.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "串口关闭错误！", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void Button_clr_clicked(object sender, RoutedEventArgs e)
        {
            a0_x.Clear();
            a0_y.Clear();
            a0_z.Clear();
            a1_x.Clear();
            a1_y.Clear();
            a1_z.Clear();
            a2_x.Clear();
            a2_y.Clear();
            a2_z.Clear();
            a3_x.Clear();
            a3_y.Clear();
            a3_z.Clear();
            image_a0.Source = null;
            image_a1.Source = null;
            image_a2.Source = null;
            image_a3.Source = null;
            image_t0.Source = null;

        }

        //点击开启时候发生的事情
        //建立一个新的进程，每隔1s读取一次串口接收到的坐标，计算参数，算出时间坐标转换后显示到屏幕上
        public void Run()
        {
            //创建一个系统定时器
            t.Elapsed += new System.Timers.ElapsedEventHandler(Time_out);
            t.AutoReset = true;
            t.Enabled = true;
        }
        public void Time_out(object source, System.Timers.ElapsedEventArgs e)
        {
            //错误检查
            if (comm.IsOpen == false)
            {     
                t.Enabled = false;           
                return;
            }
            //先从ddist数组读取数据，然后计算出结果，再转换坐标，然后再显示到屏幕上
            if (full_data==true)
            {
                Caculate_Para(ddist, station);
                Caculate_Position();
                //转换坐标,显示位置
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    image_t0.Margin = Trans_local(position_XYZ);
                    image_t0.Source = new BitmapImage(new Uri(@"人.png", UriKind.Relative));
                    distf.Content = ("( " + position_XYZ[0].ToString("F2") + " , " + position_XYZ[1].ToString("F2") + " , " + position_XYZ[2].ToString("F2") + " )");


                }));
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    distf.Content = "  数据不全，无法计算";

                }));

            }







        }
    }
}

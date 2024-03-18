using System.IO;
using System.Windows;

namespace WpfApp2
{
#pragma warning disable CS8602 // 解引用可能出现空引用。

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 当前时间片
        /// 在执行前初始化
        /// </summary>
        private static int statTime;
        /// <summary>
        /// 所有进程
        /// </summary>
        private static readonly LinkedList<PCB> all = new();
        /// <summary>
        /// 就绪队列
        /// </summary>
        private static readonly LinkedList<PCB> ready = new();
        /// <summary>
        /// 后备队列
        /// </summary>
        private static readonly LinkedList<PCB> backup = new();
        /// <summary>
        /// 输入队列
        /// </summary>
        private static readonly LinkedList<PCB> input = new();
        /// <summary>
        /// 输出队列
        /// </summary>
        private static readonly LinkedList<PCB> output = new();
        /// <summary>
        /// 等待队列
        /// </summary>
        private static readonly LinkedList<PCB> wait = new();
        /// <summary>
        /// 缓存队列，用于在队列之间操作而不引发异常
        /// </summary>
        private static readonly LinkedList<PCB> temp = new();
        /// <summary>
        /// 单个时间片的长度
        /// 默认500ms
        /// </summary>
        private static int tickTime = 500;
        /// <summary>
        /// 线程启停事件
        /// </summary>
        private static readonly AutoResetEvent autoResetEvent = new(false);
        /// <summary>
        /// 执行线程
        /// </summary>
        private static Thread? thread;
        /// <summary>
        /// 执行线程是否执行
        /// </summary>
        private static bool isRunning = false;
        public MainWindow()
        {
            InitializeComponent();
            this.Exec.IsEnabled = false;
            this.Stop.IsEnabled = false;

            thread = new Thread(Run);
            thread.Start();
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// 最小化按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 读取需要执行的进程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // 清空日志
            Logger.CreateNewLogger();
            // 停止运行
            isRunning = false;
            // 还没开始执行，先让停止按钮无法按下
            this.Stop.IsEnabled = false;
            // 时间片设置
            statTime = 0;

            // 清理所有的链表
            all.Clear();
            ready.Clear();
            backup.Clear();
            input.Clear();
            output.Clear();
            wait.Clear();

            // 用来存读取到的一行
            string? line;

            // 使用StreamReader类的异常处理
            try
            {
                StreamReader streamReader = new("./processes.txt");
                line = streamReader.ReadLine();
                // 读取进程，直到没有进程
                while (line != null)
                {
                    char c = line.ElementAt(0);
                    int num = Int32.Parse(line[1..]);
                    if (c == 'P')
                    {
                        PCB pcb = new()
                        {
                            pName = line
                        };
                        line = streamReader.ReadLine();
                        // 读取一个进程的指令，直到停止
                        while (line != null)
                        {
                            c = line.ElementAt(0);
                            num = Int32.Parse(line[1..]);
                            Instruction instruction = new()
                            {
                                iName = (InstructionEnum)c,
                                iRuntime = num,
                                iRemainTime = num
                            };
                            pcb.instructionList.AddLast(instruction);
                            if (c == 'H') break;
                            line = streamReader.ReadLine();
                        }
                        pcb.currentInstruction = pcb.instructionList.First();
                        all.AddLast(pcb);
                        backup.AddLast(pcb);
                    }
                    line = streamReader.ReadLine();
                }
                streamReader.Close();
                this.Exec.IsEnabled = true;
            }
            catch
            {
                // 出问题就把程序关了
                Logger.Write("An error occured when using class StreamReader");
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 尝试添加到执行队列，失败则添加到后备队列
        /// 目前功能作废了，只是添加到后备队列，但是减少代码改动，将其余部分注释掉了
        /// </summary>
        /// <param name="p"></param>
        private static void TryAddToReady(PCB p)
        {
            backup.AddLast(p);
            //if (ready.Count < listSize)
            //{
            //    ready.AddLast(backup.First());
            //    backup.RemoveFirst();

            //}
        }

        /// <summary>
        /// 将存在于list中的temp进程取出
        /// </summary>
        /// <param name="list"></param>
        /// <param name="temp"></param>
        private static void RemoveListFromTemp(LinkedList<PCB> list, LinkedList<PCB> temp)
        {
            foreach (var item in temp)
            {
                list.Remove(item);
            }
            temp.Clear();
        }

        /// <summary>
        /// 线程尝试执行的方法
        /// </summary>
        private void ThreadMethord()
        {
            // 使用dispatcher.invoke，委托到主线程，否则可能会有竞争
            Dispatcher.Invoke(new Action(delegate
            {
                Logger.Write();
                statTime++;
                Logger.Write("stat time:" + statTime.ToString() + Environment.NewLine, false);

                // 后备队列
                foreach (var item in backup)
                {
                    ready.AddLast(item);
                    temp.AddLast(item);
                    Logger.Write("Move " + item.pName + " from backup to ready");
                }
                RemoveListFromTemp(backup, temp);

                // 就绪队列
                bool flag = false;
                PCB? tPCB = null;
                foreach (var item in ready)
                {
                    item.currentInstruction = item.instructionList.First();
                    if (item.currentInstruction.iName == InstructionEnum.Compute)
                    {
                        if (flag) continue;
                        flag = true;

                        item.currentInstruction.iRemainTime--;
                        if (item.currentInstruction.iRemainTime <= 0)
                        {
                            item.NextInstruction();
                        }

                        if (ready.Count != 1)
                        {
                            tPCB = item;
                            temp.AddLast(item);
                        }
                    }
                    else if (item.currentInstruction.iName == InstructionEnum.Input)
                    {
                        input.AddLast(item);
                        temp.AddLast(item);
                        Logger.Write("Add " + item.pName + " to input");
                    }
                    else if (item.currentInstruction.iName == InstructionEnum.Output)
                    {
                        output.AddLast(item);
                        temp.AddLast(item);
                        Logger.Write("Add " + item.pName + " to output");
                    }
                    else if (item.currentInstruction.iName == InstructionEnum.Wait)
                    {
                        wait.AddLast(item);
                        temp.AddLast(item);
                        Logger.Write("Move " + item.pName + " from ready to wait");
                    }
                    else
                    {
                        // halt
                        temp.AddLast(item);
                        all.Remove(item);
                        Logger.Write(item.pName + " finished");
                    }
                }
                RemoveListFromTemp(ready, temp);
                if (tPCB != null)
                {
                    backup.AddLast(tPCB);
                }

                // 输入队列
                foreach (var item in input)
                {
                    item.currentInstruction.iRemainTime--;
                    if (item.currentInstruction.iRemainTime <= 0)
                    {
                        item.NextInstruction();
                        backup.AddLast(item);
                        temp.AddLast(item);
                        if (ready.Contains(item))
                        {
                            Logger.Write("Move " + item.pName + " from input to ready");
                        }
                        else
                        {
                            Logger.Write("Move " + item.pName + " from input to backup");
                        }
                    }
                }
                RemoveListFromTemp(input, temp);

                // 输出队列
                foreach (var item in output)
                {
                    item.currentInstruction.iRemainTime--;
                    if (item.currentInstruction.iRemainTime <= 0)
                    {
                        item.NextInstruction();
                        backup.AddLast(item);
                        temp.AddLast(item);
                        if (ready.Contains(item))
                        {
                            Logger.Write("Move " + item.pName + " from output to ready");
                        }
                        else
                        {
                            Logger.Write("Move " + item.pName + " from output to backup");
                        }
                    }
                }
                RemoveListFromTemp(output, temp);

                // 等待队列
                foreach (var item in wait)
                {
                    if (item.currentInstruction.iName == InstructionEnum.Wait)
                    {
                        item.currentInstruction.iRemainTime--;
                        if (item.currentInstruction.iRemainTime <= 0)
                        {
                            item.NextInstruction();
                            backup.AddLast(item);
                            //TryAddToReady(item);
                            temp.AddLast(item);
                            if (ready.Contains(item))
                            {
                                Logger.Write("Move " + item.pName + " from wait to ready");
                            }
                            else
                            {
                                Logger.Write("Move " + item.pName + " from wait to backup");
                            }
                        }
                    }
                }
                RemoveListFromTemp(wait, temp);
                
                Logger.Write("ready:", false);
                Logger.Write(ready);
                Logger.Write("backup:", false);
                Logger.Write(backup);
                Logger.Write("input:", false);
                Logger.Write(input);
                Logger.Write("output:", false);
                Logger.Write(output);
                Logger.Write("wait:", false);
                Logger.Write(wait);

                if (all.Count == 0)
                {
                    this.Stop.IsEnabled = false;
                    this.TickTime.IsEnabled = true;
                    isRunning = false;
                    Logger.Write("ALL FINISHED!");
                }

                // 显示
                string rText = "";
                string bText = "";
                string iText = "";
                string oText = "";
                string wText = "";
                foreach(var item in ready)
                {
                    rText += item.pName + Environment.NewLine;
                }
                foreach(var item in backup)
                {
                    bText += item.pName + Environment.NewLine;
                }
                foreach(var item in input)
                {
                    iText += item.pName + Environment.NewLine;
                }
                foreach (var item in output)
                {
                    oText += item.pName + Environment.NewLine;
                }
                foreach (var item in wait)
                {
                    wText += item.pName + Environment.NewLine;
                }
                this.Ready.Text = rText;
                this.Backup.Text = bText;
                this.Input.Text = iText;
                this.Output.Text = oText;
                this.Wait.Text = wText;
            }));
        }

        /// <summary>
        /// 常驻线程
        /// </summary>
        private void Run()
        {
            // 先停一下，等待按下执行
            autoResetEvent.WaitOne();

            // 常驻（死循环）
            while (true)
            {
                if (isRunning) ThreadMethord();
                

                // sleep
                Thread.Sleep(tickTime);
            }
        }

        /// <summary>
        /// 执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // 改变按钮和输入状态
            this.Exec.IsEnabled = false;
            this.Stop.IsEnabled = true;
            this.TickTime.IsEnabled = false;

            // 尝试将时间片框中的字符转换成数字存入tickTime
            if (!Int32.TryParse(this.TickTime.Text, out tickTime))
            {
                tickTime = 500;
            }

            // 改变运行状态
            isRunning = true;
            autoResetEvent.Set();
        }

        /// <summary>
        /// 停止按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // 改变按钮和输入状态
            this.Stop.IsEnabled = false;
            this.Exec.IsEnabled = true;
            this.TickTime.IsEnabled = true;

            // 改变运行状态
            isRunning = false;
            autoResetEvent.Reset();
        }
    }
#pragma warning restore CS8602 // 解引用可能出现空引用。
}
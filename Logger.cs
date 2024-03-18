using System.IO;

namespace WpfApp2
{
#pragma warning disable CS8602 // 解引用可能出现空引用。
    public static class Logger
    {
        public static void CreateNewLogger()
        {
            StreamWriter streamWriter = new("./log.txt", false);
            streamWriter.WriteLine("[" + System.DateTime.Now.ToString() + "] " + "Created a new log");
            streamWriter.Close();
        }
        public static void Write()
        {
            StreamWriter streamWriter = new("./log.txt", true);
            streamWriter.WriteLine("=============================================");
            streamWriter.Close();
        }
        public static void Write(string msg, bool flag = true)
        {
            StreamWriter streamWriter = new("./log.txt", true);
            if (flag) streamWriter.WriteLine("[" + System.DateTime.Now.ToString() + "] " + msg);
            else streamWriter.Write(msg);
            streamWriter.Close();
        }
        public static void Write(LinkedList<PCB> list)
        {
            StreamWriter streamWriter = new("./log.txt", true);
            streamWriter.WriteLineAsync("-----------------");
            foreach (PCB p in list)
            {
                streamWriter.WriteLine(
                    "Process：" + p.pName + Environment.NewLine +
                    "Current instruction：" + p.currentInstruction.iName + Environment.NewLine +
                    "Need time：" + p.currentInstruction.iRuntime + Environment.NewLine +
                    "Remain time：" + p.currentInstruction.iRemainTime
                    );
            }
            streamWriter.Close();
        }
    }
#pragma warning restore CS8602 // 解引用可能出现空引用。
}

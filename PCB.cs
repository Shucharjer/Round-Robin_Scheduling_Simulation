namespace WpfApp2
{
    /// <summary>
    /// 指令类型枚举
    /// </summary>
    public enum InstructionEnum { Compute = 'C', Input = 'I', Output = 'O', Halt = 'H', Wait = 'W' };

    /// <summary>
    /// 指令结构体
    /// </summary>
    public class Instruction
    {
        public InstructionEnum iName;
        public double iRuntime;
        public double iRemainTime;
    }

    /// <summary>
    /// 进程控制块
    /// </summary>
    public class PCB
    {
        /// <summary>
        /// 进程名
        /// </summary>
        public string? pName;
        /// <summary>
        /// 指令列表
        /// </summary>
        public LinkedList<Instruction> instructionList;
        /// <summary>
        /// 当前指令
        /// </summary>
        public Instruction? currentInstruction;
        public PCB()
        {
            instructionList = new LinkedList<Instruction>();
        }
        /// <summary>
        /// 下一个指令
        /// </summary>
        public void NextInstruction()
        {
            instructionList.RemoveFirst();
            currentInstruction = instructionList.First();
        }
        /// <summary>
        /// 移动到链表
        /// </summary>
        /// <param name="stat">当前链表</param>
        /// <param name="another">要移动到的链表</param>
        public void MoveToList(LinkedList<PCB> stat, LinkedList<PCB> another)
        {
            another.AddLast(this);
            stat.Remove(this);
        }
    }
}

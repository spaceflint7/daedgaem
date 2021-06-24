
namespace com.spaceflint.x86
{
    public partial class Machine : IMachine
    {

        // --------------------------------------------------------------------
        // constructor

        public Machine ()
        {
            cpu = new Cpu();
            InstructionsPerSecond = 150000;
        }

        // --------------------------------------------------------------------
        // Init

        void IMachine.Init (IShell _shell)
        {
            Shell = _shell;

            kst = new Kst(this);
            cga = new Cga(this);
            dos = new Dos(this, InitObject);
        }

        // --------------------------------------------------------------------
        // Run

        void IMachine.Run ()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            LastRunCount = cpu.Run(InstructionsPerSecond);
            LastRunTime = watch.Elapsed.TotalSeconds;
        }

        // --------------------------------------------------------------------
        // Stop

        public void Stop ()
        {
            cpu.Signal_STOP();
        }

        // --------------------------------------------------------------------
        // HitPoint

        public void HitPoint (int addr, System.Action callback)
        {
            cpu.HitPointAction = callback;
            cpu.HitPointAddress = addr;
        }

        // --------------------------------------------------------------------
        // Dispose

        public void Dispose ()
        {
        }

        // --------------------------------------------------------------------
        // object properties

        public Cpu Cpu => cpu;
        public IShell Shell { get; private set; }

        public object InitObject { set; private get; }

        public int InstructionsPerSecond { get; set; }

        public double LastRunTime { get; private set; }
        public long LastRunCount  { get; private set; }

        // --------------------------------------------------------------------

        [java.attr.RetainType] private Cpu cpu;

        [java.attr.RetainType] private Kst kst;
        [java.attr.RetainType] private Cga cga;
        [java.attr.RetainType] private Dos dos;

   }
}

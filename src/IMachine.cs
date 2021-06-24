
namespace com.spaceflint
{

    public interface IMachine : System.IDisposable
    {

        void Init (IShell shell);

        void Run ();

        void Stop ();

        void HitPoint (int addr, System.Action callback);

        object InitObject { set; }

        int InstructionsPerSecond { get; set; }

        double LastRunTime { get; }

        long LastRunCount { get; }

    }

}

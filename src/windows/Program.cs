
using System.Windows.Forms;

namespace com.spaceflint
{
    public class Program
    {

        // --------------------------------------------------------------------
        // program entrypoint

        public static void Main(string[] args)
        {
            var pgmName = args.Length >= 1 ? args[0] : "dos/int20.com";

            IMachine machine = new com.spaceflint.x86.Machine();
            machine.InitObject = System.IO.File.ReadAllBytes(pgmName);

            Application.Run(new ShellForm((320 * 2), (240 * 2), machine));
        }

    }
}

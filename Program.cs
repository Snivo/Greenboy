using GreenboyV2.Cpu;
using System;

namespace GreenboyV2
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            RenderWindow rw = new RenderWindow();
            rw.Run();
        }
    }
}

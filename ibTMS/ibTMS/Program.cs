using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IBApi;

namespace ibTMS
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(10, 10);

            // TmesisAPIService Control
            IBTMSAPIService ws = new IBTMSAPIService();
            ws.Run();

            IBProcess.IniIBProcess();


            Console.WriteLine("Server Running... Press a key to quit.");
            Console.ReadKey();

            ws.Stop();
        }
    }
}

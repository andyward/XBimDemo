using System;
using System.IO;

namespace Xbim.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string model = null;
            if(args.Length>0)
            {
                model = args[0];
            }
            var bimProcessor = new XbimTester(model);

            if (!File.Exists(bimProcessor.XbimModel))
            {
                bimProcessor.ProcessModel();
            }
            else
            {
                bimProcessor.ProcessExisting();
            }

            

            XbimDiagnostics.DumpVersions();

            Console.WriteLine("Enter to continue");
            Console.ReadLine();
        }

        
    }
}

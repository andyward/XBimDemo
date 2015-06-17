using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}

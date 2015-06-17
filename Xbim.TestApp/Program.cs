using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var bimProcessor = new XbimTester();

            bimProcessor.ProcessModel();
        }
    }
}

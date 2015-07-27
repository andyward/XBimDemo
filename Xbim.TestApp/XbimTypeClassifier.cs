using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;

namespace Xbim.TestApp
{
    public static class XbimTypeClassifier
    {
        public static string GetDomain(this Type type)
        {
            var components = type.FullName.Split('.');
            return components[components.Length - 2].Humanize(LetterCasing.Title);
        }
    }
}

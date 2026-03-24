using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dignus.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GlobalCommandAttribute(string name) : Attribute
    {
        public string CommandName { get; private set; } = name;
    }
}

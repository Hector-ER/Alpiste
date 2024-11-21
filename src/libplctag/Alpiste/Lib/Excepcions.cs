using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alpiste.Lib
{
    internal class Excepcions
    {
    }

    public class NoNameException: Exception
    {
        public NoNameException() : this("System tag name is empty or missing!") { }
        NoNameException(String message) : base(message) { }
        
    }
}

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

    public class CannotInitializeTagException: Exception
    {
        public CannotInitializeTagException() : this("System tag name is empty or missing!") { }
        public CannotInitializeTagException(String message) : base(message) { }
    }

    public class NoNameException: CannotInitializeTagException
    {
        public NoNameException() : this("System tag name is empty or missing!") { }
        NoNameException(String message) : base(message) { }    
    }
    public class TimeoutBelow0Exception: CannotInitializeTagException
    {
        public TimeoutBelow0Exception() : this("Timeout must not be negative!") { }
        TimeoutBelow0Exception(String message) : base(message) { }
    }
    public class ProtocolNotImplementedException : CannotInitializeTagException
    {
        public ProtocolNotImplementedException() : this("Protocol not implemented!") { }
        ProtocolNotImplementedException(String message) : base(message) { }
    }

}

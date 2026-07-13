using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Exceptions
{
    public class NetworkUnavailableException : Exception
    {
        public NetworkUnavailableException() { }

        public NetworkUnavailableException(string message) : base(message) 
        { 
        
        }
    }
}

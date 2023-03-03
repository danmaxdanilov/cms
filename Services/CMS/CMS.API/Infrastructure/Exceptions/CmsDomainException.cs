using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.API.Infrastructure.Exceptions
{
    public class CMSDomainException : Exception
    {
        public CMSDomainException()
        { }

        public CMSDomainException(string message)
            : base(message)
        { }

        public CMSDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}

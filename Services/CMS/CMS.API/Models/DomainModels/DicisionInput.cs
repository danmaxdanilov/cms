using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.API.Models.DomainModels
{
    public class DicisionInput
    {
        public string SenderType { get; set; }
        public int ObjectId { get; set; }
        public int ResultId { get; set; }
        public string CurrentCondition { get; set; }
    }
}

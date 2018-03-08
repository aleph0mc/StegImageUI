using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StegImageUI.ViewModels
{
    public class StegModel
    {
        public int SecurityLevel { get; set; }
        public int TotalSize { get; set; }
        public BitArray ContentFull { get; set; }
        public bool IsFile { get; set; }
        public int ContentSize { get; set; }
        public BitArray Content { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamGoo
{
    // Define a class to receive parsed values
    class Options
    {
        public string AdapterAddress { get; set; }

        public string OutputAdapterAddress { get; set; }

        public string MulticastAddress { get; set; }

        public int MulticastGroup { get; set; }

        public string OutputMulticastAddress { get; set; }

        public int OutputMulticastGroup { get; set; }

        public int GooFactor { get; set; }

        public int GooPause { get; set; }

        public int GooDuration { get; set; }

        public int GooType { get; set; }

        public bool Quiet { get; set; }

        public bool Verbose { get; set; }

        public string RecordFile { get; set; }

        public int WarmupTime { get; set; }
       
    }

}

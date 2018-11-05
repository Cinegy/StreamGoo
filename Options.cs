using CommandLine;

namespace StreamGoo
{
    internal class Options
    {
        [Option('a', "adapter", Required = false,
            HelpText = "IP address of the adapter to listen for specified multicast (has a random guess if left blank).")]
        public string AdapterAddress { get; set; }

        [Option('b', "outputadapter", Required = false,
            HelpText = "IP address of the adapter to write the goo'd stream to (has a random guess if left blank).")]
        public string OutputAdapterAddress { get; set; }

        [Option('m', "multicastaddress", Required = true,
            HelpText = "Input multicast address to read from.")]
        public string MulticastAddress { get; set; }

        [Option('g', "mulicastgroup", Required = true,
            HelpText = "Input multicast group port to read from.")]
        public int MulticastGroup { get; set; }

        [Option('n', "outputaddress", Required = true,
            HelpText = "Output address to write goo'd stream to.")]
        public string OutputMulticastAddress { get; set; }

        [Option('h', "outputport", Required = true,
            HelpText = "Output multicast group or UDP port to write goo'd stream to.")]
        public int OutputMulticastGroup { get; set; }

        [Option('f', "goofactor", Required = false, Default = 0,
            HelpText = "Controllable level of Gooeyness to insert into stream (chances in 10,000 of inserting a drop of scum).")]
        public int GooFactor { get; set; }

        [Option('p', "goopause", Required = false, Default = 0,
            HelpText = "How long to sleep between Goos (milliseconds)")]
        public int GooPause { get; set; }

        [Option('d', "gooduration", Required = false, Default = 1000,
            HelpText = "How long to sleep between Goos (millseconds)")]
        public int GooDuration { get; set; }

        [Option('t', "gootype", Required = false, Default = -1,
            HelpText = "Force a specific goo type rather than changing each run")]
        public int GooType { get; set; }

        [Option('q', "quiet", Required = false, Default = false,
            HelpText = "Run in quiet mode - print nothing to console.")]
        public bool Quiet { get; set; }

        [Option('v', "verbose", Required = false, Default = false,
            HelpText = "Run in verbose mode.")]
        public bool Verbose { get; set; }

        [Option('r', "record", Required = false,
            HelpText = "Record output stream to a specified file.")]
        public string RecordFile { get; set; }

        [Option('w', "warmup", Required = false, Default = 10000,
            HelpText = "Default normal, un-goo'd startup period (milliseconds) before starting goo")]
        public int WarmupTime { get; set; }
        
    }
}
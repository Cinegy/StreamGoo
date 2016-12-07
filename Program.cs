/*   Copyright 2016 Cinegy GmbH

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace StreamGoo
{
    /// <summary>
    /// StreamGoo - the simple RTP echo tool designed to screw with TS data to make
    /// life harder for recievers...
    /// 
    /// If GooFactor is set to 0, acts a quite a nice efficient little RTP relay :-)
    /// 
    /// Originally created by Lewis (Goo by Nik V), so direct complaints his way...
    /// </summary>
    class Program
    {
        private enum ExitCodes
        {
            NullOutputWriter = 100,
            UnknownError = 2000
        }

        private static bool _receiving;
        private static UdpClient _outputClient;
        private static bool _packetsStarted;
        private static bool _suppressOutput;
        private static Options _options;
        private static readonly Random Random = new Random();
        private static TimeSpan _gooStarted = new TimeSpan(0);
        private static int _gooType;
        private static long _gooDurationTicks;
        private static BinaryWriter _tsFileBinaryWriter;
        private static StreamWriter _logFileStreamWriter;
        private static long _recordedByteCounter;

        static void Main(string[] args)
        {
            _options = new Options();

            //while porting around, statically configure operations in code (until porting or writing equiv for commandlineparser package)

            _options.MulticastAddress = "239.1.1.1";
            _options.AdapterAddress = "10.10.10.1";
            _options.MulticastGroup = 1234;
            _options.GooDuration = 1000;
            _options.GooFactor = 0;
            _options.GooPause = 1000;
            _options.GooType = 0;
            _options.OutputMulticastGroup = 1234;
            _options.OutputMulticastAddress = "239.1.1.2";
            _options.OutputAdapterAddress = "10.10.10.1";

            if (_options.MulticastAddress != null)
            {
                _suppressOutput = _options.Quiet;

                PrintToConsole("Cinegy StreamGoo TS Testing Tool");
                PrintToConsole("Corrupting your Transport Streams since 2015\n");

                _gooDurationTicks = new TimeSpan(0, 0, 0, 0, _options.GooDuration).Ticks;
                _gooType = _options.GooType > -1 ? _options.GooType : Random.Next(0, 5);

                _gooStarted = DateTime.Now.AddMilliseconds(_options.WarmupTime).TimeOfDay;

                _outputClient = PrepareOutputSink(_options.OutputMulticastAddress, _options.OutputMulticastGroup);

                if (!string.IsNullOrEmpty(_options.RecordFile))
                    PrepareOutputFiles(_options.RecordFile);

                ListenToNetwork(_options.MulticastAddress, _options.MulticastGroup);

                Console.WriteLine("\nHit any key to stop gooeyness, then again to quit");

                var doExit = false;
                var origGooFactor = _options.GooFactor;

                while (!doExit)
                {
                    var keypress = Console.ReadKey();

                    if (keypress.KeyChar == 'q')
                    {
                        doExit = true;
                    }

                    if (_options.GooFactor == 0)
                    {
                        _options.GooFactor = origGooFactor;
                        PrintToConsole("Setting goo factor back to original value...");
                    }
                    else
                    {
                        _options.GooFactor = 0;
                        PrintToConsole("Pausing all goo - hit q to quit, any other key to start goo again");
                    }
                }

                PrintToConsole("Terminating StreamGoo");
                _receiving = false;
            }
            else
            {
                //if arguments are screwed up, this will print to screen (via the CommandLine library conventions) - then this waits for exit
                PrintToConsole("Press enter to exit");
                Console.ReadLine();
            }
        }
        private static void PrepareOutputFiles(string fileName)
        {
            var file = Path.GetFileNameWithoutExtension(fileName);

            if (file == null) return;

            file = file.Replace("%T", DateTime.Now.ToString("HHmm"));
            file = file.Replace("%D", DateTime.Now.ToString("dd.MM.yy"));

            Array.ForEach(Path.GetInvalidFileNameChars(),
                c => file = file.Replace(c.ToString(), String.Empty));

            var path = Path.GetPathRoot(fileName);

            var fs = new FileStream(path + file + ".ts", FileMode.OpenOrCreate);

            _tsFileBinaryWriter = new BinaryWriter(fs);

            fs = new FileStream(path + file + ".txt", FileMode.OpenOrCreate);

            _logFileStreamWriter = new StreamWriter(fs);
        }

        private static UdpClient PrepareOutputSink(string multicastAddress, int multicastGroup)
        {
            _receiving = true;

            var outputIp = _options.OutputAdapterAddress != null ? IPAddress.Parse(_options.OutputAdapterAddress) : IPAddress.Any;
            PrintToConsole($"Outputting multicast data to {multicastAddress}:{multicastGroup} via adapter {outputIp}");

            var client = new UdpClient { ExclusiveAddressUse = false };
            var localEp = new IPEndPoint(outputIp, multicastGroup);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(localEp);

            var parsedMcastAddr = IPAddress.Parse(multicastAddress);
            client.Connect(parsedMcastAddr, multicastGroup);

            return client;
        }

        private static async Task ListenToNetwork(string multicastAddress, int multicastGroup)
        {
            _receiving = true;

            var inputIp = _options.AdapterAddress != null ? IPAddress.Parse(_options.AdapterAddress) : IPAddress.Any;

            PrintToConsole($"Looking for multicast {multicastAddress}:{multicastGroup} via adapter {inputIp}");

            var client = new UdpClient { ExclusiveAddressUse = false };
            var localEp = new IPEndPoint(inputIp, multicastGroup);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.ReceiveBufferSize = 1024 * 256;
            client.Client.Bind(localEp);

            var parsedMcastAddr = IPAddress.Parse(multicastAddress);
            client.JoinMulticastGroup(parsedMcastAddr);

            while (_receiving)
            {
                var receivedResults = await client.ReceiveAsync().ConfigureAwait(false);
        
                ProcessReceivedData(receivedResults.Buffer);
            }

        }

        private static void ProcessReceivedData(byte[] data)
        {
            if (data == null) return;

            if (!_packetsStarted)
            {
                PrintToConsole("Started receiving packets...");
                _packetsStarted = true;
            }

            try
            {
                if (_outputClient != null)
                {
                    InsertGoo(ref data);

                    if (data == null) return;

                    _outputClient.Send(data, data.Length);

                    if (_tsFileBinaryWriter == null) return;

                    //todo: add RTP header skipping to this call
                    const int headerLength = 12;
                    _tsFileBinaryWriter.Write(data, headerLength, data.Length - headerLength);
                    _recordedByteCounter += data.Length + headerLength;
                }
                else
                {
                    PrintToConsole("Writing to null output client...");
                    Console.WriteLine("\nHit any key to quit");
                    Console.ReadKey();
                    Environment.Exit((int)ExitCodes.NullOutputWriter);
                }

            }
            catch (Exception ex)
            {
                PrintToConsole($@"Unhandled exception withing network receiver: {ex.Message}");
                Console.WriteLine("\nHit any key to quit");
                Console.ReadKey();
                Environment.Exit((int)ExitCodes.UnknownError);
            }
        }

        private static void InsertGoo(ref byte[] data)
        {
            return;
            var gooFactor = _options.GooFactor;

            if (gooFactor == 0) return;

            if (_gooStarted.Ticks == 0)
            {
                _gooStarted = DateTime.Now.TimeOfDay;
                PrintToConsole($"Starting new Goo period for {_options.GooDuration} milliseconds");
            }

            //see if we reached a pause time
            var ticks = DateTime.Now.TimeOfDay.Ticks - _gooStarted.Ticks;
            if ((ticks) > _gooDurationTicks)
            {
                _gooStarted = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 0, 0, 0, _options.GooPause));
                _gooType = _options.GooType > -1 ? _options.GooType : Random.Next(0, 5);
                PrintToConsole($"Pausing Goo for {_options.GooPause} milliseconds, next goo type: {_gooType} ");
            }

            //see if we have reached a goo time
            if ((DateTime.Now.TimeOfDay.Ticks - _gooStarted.Ticks) < 0)
            {
                return;
            }

            var corruptOrNot = Random.Next(0, 1000);

            if (corruptOrNot >= gooFactor) return;

            var randomNumber = Random.Next(0, data.Length);
            var oldval = data[randomNumber];

            switch (_gooType)
            {
                case 0: //single bit error
                    var bitToAdd = Random.Next(0, 7);
                    data[randomNumber] = (byte)(data[randomNumber] | (byte)bitToAdd);
                    PrintToConsole(
                        $@"Adding a little goo (single bit error) - Pos: {randomNumber}, Old: {oldval}, New: {data[
                            randomNumber]}", true);
                    return;
                case 1: //single byte increment
                    PrintToConsole(
                        $@"Adding a little goo (single byte increment) - Pos: {randomNumber}, Old: {oldval}, New: {++
                            data[randomNumber]}", true);
                    return;
                case 2: //zero whole packet
                    data = Enumerable.Repeat((byte)0x0, data.Length).ToArray();
                    PrintToConsole(@"Adding a little goo to your stream (zero whole packet)");
                    return;
                case 3: //random value filling whole packet
                    data = Enumerable.Repeat((byte)Random.Next(0, 255), data.Length).ToArray();
                    PrintToConsole($@"Adding a little goo to your stream (write {data[randomNumber]} to whole packet)");
                    return;
                case 4: //null whole packet
                    data = null;
                    PrintToConsole(@"Adding a little goo to your stream (discarding packet)");
                    return;
            }
        }

        private static void PrintToConsole(string message, bool verbose = false)
        {
            if (_logFileStreamWriter != null && _logFileStreamWriter.BaseStream.CanWrite)
            {
                _logFileStreamWriter.WriteLine("{0} (byte region: {1}) - {2}", DateTime.Now.ToString("HH:mm:ss"), _recordedByteCounter, message);
                _logFileStreamWriter.Flush();
            }

            if (_suppressOutput)
                return;

            if ((!_options.Verbose) && verbose)
                return;

            Console.WriteLine(message);
        }

    }


}

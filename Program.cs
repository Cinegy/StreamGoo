﻿/*   Copyright 2015-2022 Cinegy GmbH

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
using System.Threading;
using CommandLine;
using StreamGoo.Helpers;

namespace StreamGoo
{
    /// <summary>
    /// StreamGoo - the simple RTP echo tool designed to screw with TS data to make
    /// life harder for receivers...
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
        private static readonly Random Random = new();
        private static TimeSpan _gooStarted = new(0);
        private static int _gooType;
        private static long _gooDurationTicks;
        private static BinaryWriter _tsFileBinaryWriter;
        private static StreamWriter _logFileStreamWriter;
        private static long _recordedByteCounter;
        private static byte[] _outOfOrderPacketBuffer;
        
        private const byte SyncByte = 0x47;
        private const int TsPacketSize = 188;

        private static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);

            return result.MapResult(Run, _ => CheckArgumentErrors());
        }

        private static int CheckArgumentErrors()
        {
            //will print using library the appropriate help - now pause the console for the viewer
            Console.WriteLine("Hit enter to quit");
            Console.ReadLine();
            return -1;
        }

        private static int Run(Options opts)
        {
            _options = opts;
            _suppressOutput = _options.Quiet;

            PrintToConsole($"{Product.Name}: {Product.Version} (Built: {Product.BuildTime})");
            PrintToConsole($"Corrupting your Transport Streams since 2015...\n");
            _gooDurationTicks = new TimeSpan(0, 0, 0, 0, _options.GooDuration).Ticks;
            _gooType = _options.GooType > -1 ? _options.GooType : Random.Next(0, 5);

            _gooStarted = DateTime.Now.AddMilliseconds(_options.WarmupTime).TimeOfDay;

            _outputClient = PrepareOutputSink(_options.OutputMulticastAddress, _options.OutputMulticastGroup);

            if (!string.IsNullOrEmpty(_options.RecordFile))
                PrepareOutputFiles(_options.RecordFile);

            StartListeningToNetwork(_options.MulticastAddress, _options.MulticastGroup);

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
                    PrintToConsole("Setting goo factor back to original value...",true);
                }
                else
                {
                    _options.GooFactor = 0;
                    PrintToConsole("Pausing all goo - hit q to quit, any other key to start goo again",true);
                }
            }

            PrintToConsole("Terminating StreamGoo");
            _receiving = false;

            return 0;
        }

        

        private static void StartListeningToNetwork(string multicastAddress, int multicastGroup)
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
            client.JoinMulticastGroup(parsedMcastAddr, inputIp);

            var ts = new ThreadStart(delegate
            {
                ReceivingNetworkWorkerThread(client, localEp);
            });

            var receiverThread = new Thread(ts) { Priority = ThreadPriority.Highest };

            receiverThread.Start();
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

        private static void ReceivingNetworkWorkerThread(UdpClient client, IPEndPoint localEp)
        {
            while (_receiving)
            {
                var data = client.Receive(ref localEp);
                if (data == null) continue;
                if (!_packetsStarted)
                {
                    PrintToConsole("Started receiving packets...", true);
                    _packetsStarted = true;
                }
                try
                {
                    if (_outputClient != null)
                    {
                        InsertGoo(ref data);
                        if (data != null)
                        {
                            _outputClient.Send(data, data.Length);
                            //check to see if any out-of-order packets are waiting to be sent
                            if (_outOfOrderPacketBuffer != null)
                            {
                                _outputClient.Send(_outOfOrderPacketBuffer, _outOfOrderPacketBuffer.Length);
                                _outOfOrderPacketBuffer = null;
                            }

                            if (_tsFileBinaryWriter != null)
                            {
                                //todo: add RTP header skipping to this call
                                const int headerLength = 12;
                                _tsFileBinaryWriter.Write(data, headerLength, data.Length - headerLength);
                                _recordedByteCounter += data.Length + headerLength;
                            }
                        }
                    }
                    else
                    {
                        PrintToConsole("Writing to null output client...",true);
                        Console.WriteLine("\nHit any key to quit");
                        Console.ReadKey();
                        Environment.Exit((int)ExitCodes.NullOutputWriter);
                    }

                }
                catch (Exception ex)
                {
                    PrintToConsole($@"Unhandled exception withing network receiver: {ex.Message}",true);
                    Console.WriteLine("\nHit any key to quit");
                    Console.ReadKey();
                    Environment.Exit((int)ExitCodes.UnknownError);
                }
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

            var path = Path.GetFullPath(fileName);

            var fs = new FileStream(path + file + ".ts", FileMode.OpenOrCreate);

            _tsFileBinaryWriter = new BinaryWriter(fs);

            fs = new FileStream(path + file + ".txt", FileMode.OpenOrCreate);

            _logFileStreamWriter = new StreamWriter(fs);
        }

        private static void InsertGoo(ref byte[] data)
        {
            var gooFactor = _options.GooFactor;

            if (gooFactor == 0) return;

            if (_gooStarted.Ticks == 0)
            {
                _gooStarted = DateTime.Now.TimeOfDay;
                PrintToConsole($"Starting new Goo period for {_options.GooDuration} milliseconds",true);
            }

            //see if we reached a pause time
            var ticks = DateTime.Now.TimeOfDay.Ticks - _gooStarted.Ticks;
            if (ticks > _gooDurationTicks)
            {
                _gooStarted = DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 0, 0, 0, _options.GooPause));
                _gooType = _options.GooType > -1 ? _options.GooType : Random.Next(0, 5);
                PrintToConsole($"Pausing Goo for {_options.GooPause} milliseconds, next goo type: {_gooType} ", true);
            }

            //see if we have reached a goo time
            if (DateTime.Now.TimeOfDay.Ticks - _gooStarted.Ticks < 0)
            {
                return;
            }

            var corruptOrNot = Random.Next(0, 10000);

            if (corruptOrNot >= gooFactor) return;

            var randomNumber = Random.Next(0, data.Length);
            var oldval = data[randomNumber];

            switch (_gooType)
            {
                case 0: //single bit error
                    var pow = Random.Next(0, 8);
                    var bitToAdd = 1 << pow;
                    data[randomNumber] = (byte)(data[randomNumber] + (byte)bitToAdd);
                    PrintToConsole(
                        $@"Adding a little goo (single bit error) - Pos: {randomNumber}, Old: {oldval} + {bitToAdd}, New: {data[
                            randomNumber]}",true);
                    return;
                case 1: //single byte increment
                    PrintToConsole(
                        $@"Adding a little goo (single byte increment) - Pos: {randomNumber}, Old: {oldval}, New: {++
                            data[randomNumber]}", true);
                    return;
                case 2: //zero whole packet
                    data = Enumerable.Repeat((byte)0x0, data.Length).ToArray();
                    PrintToConsole(@"Adding a little goo to your stream (zero whole packet)", true);
                    return;
                case 3: //random value filling whole packet
                    data = Enumerable.Repeat((byte)Random.Next(0, 255), data.Length).ToArray();
                    PrintToConsole($@"Adding a little goo to your stream (write {data[randomNumber]} to whole packet)", true);
                    return;
                case 4: //null whole packet
                    data = null;
                    PrintToConsole(@"Adding a little goo to your stream (discarding packet)", true);
                    return;
                case 5: //out of order packet
                    _outOfOrderPacketBuffer = data;
                    data = null;
                    PrintToConsole(@"Adding a little goo to your stream (out of order packet)", true);
                    return;
                case 6: //jitter - have a little snooze
                    var timeToSleep = Random.Next(0, 80);
                    PrintToConsole($"Adding a little goo to your stream (add jitter - sleep approx. {timeToSleep} ms)", true);
                    Thread.Sleep(timeToSleep);
                    return;
                case 7: //transport error indicator randomly set
                    PrintToConsole($"Adding a little goo to your stream (add Transport Error Indicator)", true);
                    SetTeiFlag(ref data);
                    return;
            }
        }

        private static void SetTeiFlag(ref byte[] data)
        {
            var start = FindSync(ref data, 0);
            data[start + 1] = (byte)(data[start + 1] + 0x80);
        }

        private static int FindSync(ref byte[] tsData, int offset)
        {
            if (tsData == null) throw new ArgumentNullException(nameof(tsData));
            
            for (var i = offset; i < tsData.Length; i++)
            {
                //check to see if we found a sync byte
                if (tsData[i] != SyncByte) continue;
                if (i + 1 * TsPacketSize < tsData.Length && tsData[i + 1 * TsPacketSize] != SyncByte) continue;
                if (i + 2 * TsPacketSize < tsData.Length && tsData[i + 2 * TsPacketSize] != SyncByte) continue;
                if (i + 3 * TsPacketSize < tsData.Length && tsData[i + 3 * TsPacketSize] != SyncByte) continue;
                if (i + 4 * TsPacketSize < tsData.Length && tsData[i + 4 * TsPacketSize] != SyncByte) continue;
                // seems to be ok
                return i;
            }
            return -1;
        }


        private static void PrintToConsole(string message, bool timeStampConsole = false, bool verbose = false)
        {
            if (_logFileStreamWriter != null && _logFileStreamWriter.BaseStream.CanWrite)
            {
                _logFileStreamWriter.WriteLine("{0:HH:mm:ss} (byte region: {1}) - {2}", DateTime.Now, _recordedByteCounter, message);
                _logFileStreamWriter.Flush();
            }

            if (_suppressOutput)
                return;

            if (!_options.Verbose && verbose)
                return;

            Console.WriteLine(timeStampConsole ? $"({DateTime.UtcNow:HH:mm:ss}): {message}" : message);
        }

    }
    
}

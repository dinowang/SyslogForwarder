﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;

namespace SyslogForwarder
{
    class Program
    {
        static ListenerConfig _listener;
        static ConverterConfig _converter;
        static ForwarderConfig _forwarder;
        static Thread _listenerThread;
        static ConcurrentQueue<EventData> _waitHandles = new ConcurrentQueue<EventData>();

        static Program()
        {
            var config = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", false)
                                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json", optional: true)
                                .AddEnvironmentVariables()
                                .Build();

            _listener = config.GetSection("Listener").Get<ListenerConfig>();
            _converter = config.GetSection("Converter").Get<ConverterConfig>();
            _forwarder = config.GetSection("Forwarder").Get<ForwarderConfig>();
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Start receiver...");
            _listenerThread = new Thread(Receiver);
            _listenerThread.Start();

            Console.WriteLine("Start forwarder...");
            var ehConnection = new EventHubsConnectionStringBuilder(_forwarder.EventHub.ConnectionString)
            {
                EntityPath = _forwarder.EventHub.EventHubName
            };
            var ehClient = EventHubClient.Create(ehConnection);

            var events = new List<EventData>(2048);
            while (true)
            {
                while (!_waitHandles.IsEmpty)
                {
                    if (ehClient.IsClosed)
                    {
                        ehClient = EventHubClient.Create(ehConnection);
                    }

                    Console.WriteLine($"Preparing forwarding batch...");

                    events.Clear();
                    while (_waitHandles.TryDequeue(out var eventData))
                    {
                        events.Add(eventData);
                    }

                    if (events.Any())
                    {
                        Console.WriteLine($"Forward {events.Count} event(s)...");
                        await ehClient.SendAsync(events);
                    }
                }

                await Task.Delay(_forwarder.IterationInterval);
            }
        }

        static async void Receiver()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, _listener.Port);
            var udpListener = new UdpClient(endpoint);

            while (true)
            {
                var receivedBuffer = await udpListener.ReceiveAsync();

                // var receivedText = Encoding.ASCII.GetString(receivedBuffer.Buffer);
                // Console.Write($"Received: {receivedText}");

                var eventData = new EventData(receivedBuffer.Buffer);
                _waitHandles.Enqueue(eventData);
            }
        }
    }
}

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NStore.Aggregates;
using NStore.InMemory;
using NStore.Raw;
using NStore.Sample.Support;

namespace NStore.Sample.Projections
{
    public class AppProjections : IStoreObserver
    {
        public long Position { get; set; } = 0;
        public RoomsOnSaleProjection Rooms { get; }
        public ConfirmedBookingsProjection Bookings { get; }

        private readonly IList<IProjection> _projections = new List<IProjection>();
        private readonly IReporter _reporter = new ColoredConsoleReporter("projections", ConsoleColor.Yellow);

        private readonly IDictionary<Type, long> _metrics = new ConcurrentDictionary<Type, long>();
        private long _fillersCount = 0;
        private long _dispatchedCount = 0;
        private bool _catchingUp = false;

        public AppProjections(INetworkSimulator network)
        {
            Rooms = new RoomsOnSaleProjection(new ColoredConsoleReporter("rooms on sale", ConsoleColor.Red), network);
            Bookings = new ConfirmedBookingsProjection(
                new ColoredConsoleReporter("confirmed bookings", ConsoleColor.Cyan), network);
            Setup();
        }

        public ScanCallbackResult Observe(
            long storeIndex,
            string streamId,
            long partitionIndex,
            object payload)
        {
            if (storeIndex != Position + 1)
            {
                // * * * * * * * * * * * * * * * * * * * * * * * * *
                // * WARNING: ˌɛsəˈtɛrɪk/ stuff can be done here   *
                // * * * * * * * * * * * * * * * * * * * * * * * * *


                // * * * * * * * * * * * * * * * * * * * * * * * * *
                // * Or just sit down and watch basic stuff @ work *
                // * * * * * * * * * * * * * * * * * * * * * * * * *
                if (!_catchingUp)
                {
                    _reporter.Report(
                        $"!!!!!!!!!!!!!!!! Projection out of sequence {storeIndex} => wait next poll !!!!!!!!!!!!!!!!");
                    _catchingUp = true;
                }

                // * * * * * * * * * * * * * * * * * * * * * * * * * *
                // * Add a timeout to stop if out of sequence (crash)*
                // * * * * * * * * * * * * * * * * * * * * * * * * * *

                return ScanCallbackResult.Stop;
            }

            _catchingUp = false;

            Position = storeIndex;

            Changeset changes = (Changeset) payload;
            StoreMetrics(changes);

            // skip fillers
            if (payload == null)
            {
                return ScanCallbackResult.Continue;
            }

            _dispatchedCount++;
            var sw = new Stopwatch();
            sw.Start();
            Task.WaitAll
            (
                _projections.Select(p => p.Project(changes)).ToArray()
            );

            _reporter.Report($"dispatched changeset #{storeIndex} took {sw.ElapsedMilliseconds}ms");
            return ScanCallbackResult.Continue;
        }

        private void StoreMetrics(Changeset changes)
        {
            if (changes == null)
            {
                this._fillersCount++;
                return;
            }

            foreach (var e in changes.Events)
            {
                var k = e.GetType();
                _metrics.TryGetValue(k, out long value);
                _metrics[k] = value + 1;
            }
        }

        private void Setup()
        {
            _projections.Add(Rooms);
            _projections.Add(Bookings);
        }

        public void DumpMetrics()
        {
            _reporter.Report("Events:");
            foreach (var k in _metrics.OrderByDescending(x => x.Value))
            {
                _reporter.Report($"  {k.Key.Name} => {k.Value}");
            }

            _reporter.Report("Changesets:");
            _reporter.Report($"  Dispatched => {_dispatchedCount}");
            _reporter.Report($"  Fillers    => {_fillersCount}");
        }
    }
}
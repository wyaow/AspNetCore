// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SocketTransportOptions
    {
        /// <summary>
        /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        public long? MaxReadBufferSize { get; set; } = PipeOptions.Default.PauseWriterThreshold;

        public long? MaxWriteBufferSize { get; set; } = PipeOptions.Default.PauseWriterThreshold;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.MemoryPoolFactory.Create;
    }
}

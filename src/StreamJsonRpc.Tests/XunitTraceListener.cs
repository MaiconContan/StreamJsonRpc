﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit.Abstractions;

internal class XunitTraceListener : TraceListener
{
    private readonly ITestOutputHelper logger;
    private readonly StringBuilder lineInProgress = new StringBuilder();
    private bool disposed;

    internal XunitTraceListener(ITestOutputHelper logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool IsThreadSafe => false;

    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
    {
        // Convert all data messages to JSON representations.
        if (data is JsonRpcMessage message)
        {
            this.TraceEvent(eventCache, source, eventType, id, "{0}{1}{2}", (JsonRpc.TraceEvents)id, Environment.NewLine, JToken.FromObject(message).ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }

    public override void Write(string message) => this.lineInProgress.Append(message);

    public override void WriteLine(string message)
    {
        if (!this.disposed)
        {
            this.logger.WriteLine(this.lineInProgress.ToString() + message);
            this.lineInProgress.Clear();
        }
    }

    protected override void Dispose(bool disposing)
    {
        this.disposed = true;
        base.Dispose(disposing);
    }
}

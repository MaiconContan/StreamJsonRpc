﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Verifies the <see cref="JsonRpc"/> class's functionality as a JSON-RPC 2.0 *server* (i.e. the one receiving requests, and sending results)
/// against various client messages.
/// </summary>
public class JsonRpcServerInteropTests : InteropTestBase
{
    private readonly Server server;
    private readonly JsonRpc serverRpc;

    public JsonRpcServerInteropTests(ITestOutputHelper logger)
        : base(logger)
    {
        this.server = new Server();
        this.serverRpc = new JsonRpc(this.messageHandler, this.server);
        this.serverRpc.StartListening();
    }

    [Fact]
    public async Task ServerAcceptsNumberForMessageId()
    {
        dynamic response = await this.RequestAsync(new
        {
            jsonrpc = "2.0",
            method = "EchoInt",
            @params = new[] { 5 },
            id = 1,
        });

        Assert.Equal(5, (int)response.result);
        Assert.Equal(1, (int)response.id);
    }

    [Fact]
    public async Task ServerAcceptsStringForMessageId()
    {
        dynamic response = await this.RequestAsync(new
        {
            jsonrpc = "2.0",
            method = "EchoInt",
            @params = new[] { 5 },
            id = "abc",
        });

        Assert.Equal(5, (int)response.result);
        Assert.Equal("abc", (string)response.id);
    }

    [Fact]
    public async Task ServerAcceptsNumberForProgressId()
    {
        dynamic response = await this.RequestAsync(new
        {
            jsonrpc = "2.0",
            method = "EchoSuccessWithProgressParam",
            @params = new[] { 5 },
            id = "abc",
        });

        // If the response is returned without error it means the server succeded on using the token to create the JsonProgress instance
        Assert.Equal("Success!", (string)response.result);
    }

    [Fact]
    public async Task ServerAcceptsStringForProgressId()
    {
        dynamic response = await this.RequestAsync(new
        {
            jsonrpc = "2.0",
            method = "EchoSuccessWithProgressParam",
            @params = new[] { "Token" },
            id = "abc",
        });

        // If the response is returned without error it means the server succeded on using the token to create the JsonProgress instance
        Assert.Equal("Success!", (string)response.result);
    }

    [Fact]
    public async Task ServerAlwaysReturnsResultEvenIfNull()
    {
        var response = await this.RequestAsync(new
        {
            jsonrpc = "2.0",
            method = "EchoString",
            @params = new object?[] { null },
            id = 1,
        });

        // Assert that result is specified, but that its value is null.
        Assert.NotNull(response["result"]);
        Assert.Null(response.Value<string>("result"));
    }

#pragma warning disable CA1801 // Review unused parameters
    private class Server
    {
        public int EchoInt(int value) => value;

        public string EchoString(string value) => value;

        public string EchoSuccessWithProgressParam(IProgress<int> progress) => "Success!";
    }
}

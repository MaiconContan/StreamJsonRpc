﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit;
using Xunit.Abstractions;

public class JsonRpcJsonHeadersTests : JsonRpcTests
{
    public JsonRpcJsonHeadersTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    [Fact]
    public async Task CustomJsonConvertersAreNotAppliedToBaseMessage()
    {
        var clientMessageFormatter = (JsonMessageFormatter)this.clientMessageFormatter;
        var serverMessageFormatter = (JsonMessageFormatter)this.serverMessageFormatter;

        // This test works because it encodes any string value, such that if the json-rpc "method" property
        // were serialized using the same serializer as parameters, the invocation would fail because the server-side
        // doesn't find the method with the mangled name.

        // Test with the converter only on the client side.
        clientMessageFormatter.JsonSerializer.Converters.Add(new StringBase64Converter());
        string result = await this.clientRpc.InvokeAsync<string>(nameof(this.server.ExpectEncodedA), "a");
        Assert.Equal("a", result);

        // Test with the converter on both sides.
        serverMessageFormatter.JsonSerializer.Converters.Add(new StringBase64Converter());
        result = await this.clientRpc.InvokeAsync<string>(nameof(this.server.RepeatString), "a");
        Assert.Equal("a", result);

        // Test with the converter only on the server side.
        clientMessageFormatter.JsonSerializer.Converters.Clear();
        result = await this.clientRpc.InvokeAsync<string>(nameof(this.server.AsyncMethod), "YQ==");
        Assert.Equal("YSE=", result); // a!
    }

    [Fact]
    public async Task CanInvokeServerMethodWithParameterPassedAsObject()
    {
        string result1 = await this.clientRpc.InvokeWithParameterObjectAsync<string>(nameof(Server.TestParameter), new { test = "test" });
        Assert.Equal("object {" + Environment.NewLine + "  \"test\": \"test\"" + Environment.NewLine + "}", result1);
    }

    [Fact]
    public async Task InvokeWithParameterObjectAsync_AndCancel()
    {
        using (var cts = new CancellationTokenSource())
        {
            var invokeTask = this.clientRpc.InvokeWithParameterObjectAsync<string>(nameof(Server.AsyncMethodWithJTokenAndCancellation), new { b = "a" }, cts.Token);
            await Task.WhenAny(invokeTask, this.server.ServerMethodReached.WaitAsync(this.TimeoutToken));
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invokeTask);
        }
    }

    [Fact]
    public async Task InvokeWithParameterObjectAsync_NoResult_AndCancel()
    {
        using (var cts = new CancellationTokenSource())
        {
            var invokeTask = this.clientRpc.InvokeWithParameterObjectAsync(nameof(Server.AsyncMethodWithJTokenAndCancellation), new { b = "a" }, cts.Token);
            await Task.WhenAny(invokeTask, this.server.ServerMethodReached.WaitAsync(this.TimeoutToken));
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invokeTask);
        }
    }

    [Fact]
    public async Task InvokeWithParameterObjectAsync_AndComplete()
    {
        using (var cts = new CancellationTokenSource())
        {
            var invokeTask = this.clientRpc.InvokeWithParameterObjectAsync<string>(nameof(Server.AsyncMethodWithJTokenAndCancellation), new { b = "a" }, cts.Token);
            this.server.AllowServerMethodToReturn.Set();
            string result = await invokeTask;
            Assert.Equal(@"{""b"":""a""}!", result);
        }
    }

    [Fact]
    public async Task InvokeWithParameterObject_WithRenamingAttributes()
    {
        var param = new ParamsObjectWithCustomNames { TheArgument = "hello" };
        string result = await this.clientRpc.InvokeWithParameterObjectAsync<string>(nameof(Server.ServerMethod), param, this.TimeoutToken);
        Assert.Equal(param.TheArgument + "!", result);
    }

    [Fact]
    public async Task CanInvokeServerMethodWithParameterPassedAsArray()
    {
        string result1 = await this.clientRpc.InvokeAsync<string>(nameof(Server.TestParameter), "test");
        Assert.Equal("object test", result1);
    }

    [Fact]
    public async Task InvokeWithCancellationAsync_AndCancel()
    {
        using (var cts = new CancellationTokenSource())
        {
            var invokeTask = this.clientRpc.InvokeWithCancellationAsync<string>(nameof(Server.AsyncMethodWithJTokenAndCancellation), new[] { "a" }, cts.Token);
            await Task.WhenAny(invokeTask, this.server.ServerMethodReached.WaitAsync(this.TimeoutToken));
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invokeTask);
        }
    }

    [Fact]
    public async Task InvokeWithCancellationAsync_AndComplete()
    {
        using (var cts = new CancellationTokenSource())
        {
            var invokeTask = this.clientRpc.InvokeWithCancellationAsync<string>(nameof(Server.AsyncMethodWithJTokenAndCancellation), new[] { "a" }, cts.Token);
            this.server.AllowServerMethodToReturn.Set();
            string result = await invokeTask;
            Assert.Equal(@"""a""!", result);
        }
    }

    [Fact]
    public async Task CanPassAndCallPrivateMethodsObjects()
    {
        var result = await this.clientRpc.InvokeAsync<Foo>(nameof(Server.MethodThatAcceptsFoo), new Foo { Bar = "bar", Bazz = 1000 });
        Assert.NotNull(result);
        Assert.Equal("bar!", result.Bar);
        Assert.Equal(1001, result.Bazz);

        // anonymous types are not supported when TypeHandling is set to "Object" or "Auto".
        if (!this.IsTypeNameHandlingEnabled)
        {
            result = await this.clientRpc.InvokeAsync<Foo>(nameof(Server.MethodThatAcceptsFoo), new { Bar = "bar", Bazz = 1000 });
            Assert.NotNull(result);
            Assert.Equal("bar!", result.Bar);
            Assert.Equal(1001, result.Bazz);
        }
    }

    [Fact]
    public async Task Completion_FaultsOnFatalError()
    {
        Task completion = this.serverRpc.Completion;
        byte[] invalidMessage = Encoding.UTF8.GetBytes("A\n\n");
        await this.clientStream.WriteAsync(invalidMessage, 0, invalidMessage.Length).WithCancellation(this.TimeoutToken);
        await this.clientStream.FlushAsync().WithCancellation(this.TimeoutToken);
        await Assert.ThrowsAsync<BadRpcHeaderException>(() => completion).WithCancellation(this.TimeoutToken);
        Assert.Same(completion, this.serverRpc.Completion);
    }

    [Fact]
    public async Task ExceptionControllingErrorData()
    {
        var exception = await Assert.ThrowsAsync<RemoteInvocationException>(() => this.clientRpc.InvokeAsync(nameof(Server.ThrowLocalRpcException)));

        // C# dynamic is infamously unstable. If this test ends up being unstable, yet the dump clearly shows the fields are there even though the exception claims it isn't,
        // that's consistent with the instability I've seen before. Switching to using JToken APIs will fix the instability, but it relies on using the JsonMessageFormatter.
        dynamic? data = exception.ErrorData;
        dynamic myCustomData = data!.myCustomData;
        string actual = myCustomData;
        Assert.Equal("hi", actual);
    }

    [Fact]
    public async Task CanPassExceptionFromServer_ErrorData()
    {
        RemoteInvocationException exception = await Assert.ThrowsAnyAsync<RemoteInvocationException>(() => this.clientRpc.InvokeAsync(nameof(Server.MethodThatThrowsUnauthorizedAccessException)));
        Assert.Equal((int)JsonRpcErrorCode.InvocationError, exception.ErrorCode);

        var errorDataJToken = (JToken?)exception.ErrorData;
        Assert.NotNull(errorDataJToken);
        var errorData = errorDataJToken!.ToObject<CommonErrorData>(new JsonSerializer());
        Assert.NotNull(errorData?.StackTrace);
        Assert.StrictEqual(COR_E_UNAUTHORIZEDACCESS, errorData?.HResult);
    }

    protected override void InitializeFormattersAndHandlers(bool controlledFlushingClient)
    {
        this.clientMessageFormatter = new JsonMessageFormatter
        {
            JsonSerializer =
            {
                Converters =
                {
                    new UnserializableTypeConverter(),
                    new TypeThrowsWhenDeserializedConverter(),
                },
            },
        };
        this.serverMessageFormatter = new JsonMessageFormatter
        {
            JsonSerializer =
            {
                Converters =
                {
                    new UnserializableTypeConverter(),
                    new TypeThrowsWhenDeserializedConverter(),
                },
            },
        };

        this.serverMessageHandler = new HeaderDelimitedMessageHandler(this.serverStream, this.serverStream, this.serverMessageFormatter);
        this.clientMessageHandler = controlledFlushingClient
            ? new DelayedFlushingHandler(this.clientStream, this.clientMessageFormatter)
            : new HeaderDelimitedMessageHandler(this.clientStream, this.clientStream, this.clientMessageFormatter);
    }

    [DataContract]
    public class ParamsObjectWithCustomNames
    {
        [DataMember(Name = "argument")]
        public string? TheArgument { get; set; }
    }

    protected class UnserializableTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(CustomSerializedType);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return new CustomSerializedType
            {
                Value = (string?)reader.Value,
            };
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(((CustomSerializedType?)value)!.Value);
        }
    }

    protected class TypeThrowsWhenDeserializedConverter : JsonConverter<TypeThrowsWhenDeserialized>
    {
        public override TypeThrowsWhenDeserialized ReadJson(JsonReader reader, Type objectType, TypeThrowsWhenDeserialized? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw CreateExceptionToBeThrownByDeserializer();
        }

        public override void WriteJson(JsonWriter writer, TypeThrowsWhenDeserialized? value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }

    protected class DelayedFlushingHandler : HeaderDelimitedMessageHandler, IControlledFlushHandler
    {
        public DelayedFlushingHandler(Stream stream, IJsonRpcMessageFormatter formatter)
            : base(stream, formatter)
        {
        }

        public AsyncAutoResetEvent FlushEntered { get; } = new AsyncAutoResetEvent();

        public AsyncManualResetEvent AllowFlushAsyncExit { get; } = new AsyncManualResetEvent();

        protected override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            this.FlushEntered.Set();
            await this.AllowFlushAsyncExit.WaitAsync();
            await base.FlushAsync(cancellationToken);
        }
    }

    private class StringBase64Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String((string)reader.Value!));
            return decoded;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var stringValue = (string?)value;
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringValue!));
            writer.WriteValue(encoded);
        }
    }
}

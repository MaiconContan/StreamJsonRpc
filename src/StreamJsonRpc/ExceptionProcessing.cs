﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using StreamJsonRpc.Protocol;

namespace StreamJsonRpc;

/// <summary>
/// Enumerates the exception handling behaviors that are built into the <see cref="JsonRpc"/> class.
/// </summary>
/// <seealso cref="JsonRpc.ExceptionStrategy"/>
public enum ExceptionProcessing
{
    /// <summary>
    /// Exceptions thrown by the server are serialized as the simple <see cref="Protocol.CommonErrorData"/> class
    /// and the default error code is <see cref="JsonRpcErrorCode.InvocationError"/>.
    /// The client experiences a <see cref="RemoteInvocationException"/> whose <see cref="RemoteInvocationException.DeserializedErrorData"/> property
    /// is the deserialized <see cref="Protocol.CommonErrorData"/>.
    /// </summary>
    CommonErrorData,

    /// <summary>
    /// Exceptions thrown by the server are serialized via the <see cref="System.Runtime.Serialization.ISerializable"/> mechanism and captures more detail,
    /// using the error code <see cref="JsonRpcErrorCode.InvocationErrorWithException"/>.
    /// These are deserialized with the original exception types as inner exceptions of the <see cref="RemoteInvocationException"/> thrown at the client.
    /// The <see cref="RemoteInvocationException.DeserializedErrorData"/> is also set to an instance of <see cref="Protocol.CommonErrorData"/> that resembles the inner exception tree.
    /// </summary>
    ISerializable,
}

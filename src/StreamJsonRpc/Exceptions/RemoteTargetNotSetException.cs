﻿using System;

namespace StreamJsonRpc
{
    /// <summary>
    /// Remote RPC exception that indicates that the server has no target object.
    /// </summary>
    /// <seealso cref="RemoteRpcException" />
#if NET45
    [System.Serializable]
#endif
    public class RemoteTargetNotSetException : RemoteRpcException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteTargetNotSetException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        internal RemoteTargetNotSetException(string message) : base(message)
        {
        }

#if NET45
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteTargetNotSetException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected RemoteTargetNotSetException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
#endif

    }
}

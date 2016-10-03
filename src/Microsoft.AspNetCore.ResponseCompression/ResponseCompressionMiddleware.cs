﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Enable HTTP response compression.
    /// </summary>
    public class ResponseCompressionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IResponseCompressionProvider _provider;

        private readonly bool _enableHttps;

        /// <summary>
        /// Initialize the Response Compression middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="provider"></param>
        /// <param name="options"></param>
        public ResponseCompressionMiddleware(RequestDelegate next, IResponseCompressionProvider provider, IOptions<ResponseCompressionOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _next = next;
            _provider = provider;
            _enableHttps = options.Value.EnableHttps;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            ICompressionProvider compressionProvider = null;

            if (!context.Request.IsHttps || _enableHttps)
            {
                compressionProvider = _provider.GetCompressionProvider(context);
            }

            if (compressionProvider == null)
            {
                await _next(context);
                return;
            }

            var bodyStream = context.Response.Body;

            using (var bodyWrapperStream = new BodyWrapperStream(context.Response, bodyStream, _provider, compressionProvider))
            {
                context.Response.Body = bodyWrapperStream;

                try
                {
                    await _next(context);
                }
                finally
                {
                    context.Response.Body = bodyStream;
                }
            }
        }
    }
}
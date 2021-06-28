// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    public class GetCredentialRequest
    {
        public JToken Params { get; init; }

        public JToken Response { get; init; }
    }
}
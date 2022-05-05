﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class AbstractVirtualCharService
    {
        private struct SourceTextTextInfo : ITextInfo<SourceText>
        {
            public char Get(SourceText text, int index) => text[index];
            public int Length(SourceText text) => text.Length;
        }
    }
}

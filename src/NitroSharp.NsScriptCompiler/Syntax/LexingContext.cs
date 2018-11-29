﻿using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.NsScriptNew.Text;
using System;

namespace NitroSharp.NsScriptNew
{
    public readonly struct LexingContext
    {
        private readonly Lexer _lexer;
        private readonly SourceText _sourceText;

        internal LexingContext(Lexer lexer)
        {
            _lexer = lexer;
            _sourceText = lexer.SourceText;
        }

        public SourceText SourceText => _sourceText;
        public DiagnosticBag Diagnostics => _lexer.Diagnostics;

        public ReadOnlySpan<char> GetText(in SyntaxToken token)
        {
            return _sourceText.GetSlice(token.TextSpan);
        }

        public ReadOnlySpan<char> GetValueText(in SyntaxToken token)
        {
            return _sourceText.GetSlice(token.GetValueSpan());
        }
    }
}
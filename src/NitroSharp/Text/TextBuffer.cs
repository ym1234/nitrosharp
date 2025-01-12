﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Syntax.PXml;
using Veldrid;

namespace NitroSharp.Text
{
    internal sealed class TextBuffer
    {
        private static readonly PXmlFlattener s_pxmlFlattener = new();

        private TextBuffer(
            ImmutableArray<TextBufferSegment> segments,
            VoiceSegment? voice,
            uint textLength)
        {
            Segments = segments;
            Voice = voice;
            TextLength = textLength;
        }

        public ImmutableArray<TextBufferSegment> Segments { get; }
        public VoiceSegment? Voice { get; }
        public uint TextLength { get; }
        public bool IsEmpty => Segments.Length == 0;

        public static TextBuffer FromPXmlString(string pxmlString, FontConfiguration fontConfig)
        {
            PXmlContent root = Parsing.ParsePXmlString(pxmlString);
            return s_pxmlFlattener.FlattenPXmlContent(root, fontConfig);
        }

        public TextSegment? AssertSingleTextSegment()
        {
            return Segments.Length == 1 && Segments[0] is TextSegment ts
                ? ts : null;
        }

        private sealed class PXmlFlattener : PXmlSyntaxVisitor
        {
            private struct TextRunData
            {
                public string? Text;
                public string? RubyText;
                public int? FontSize;
                public RgbaFloat? Color;
                public RgbaFloat? OutlineColor;
                public bool Italic;
            }

            private FontConfiguration? _fontConfig;
            private readonly ImmutableArray<TextBufferSegment>.Builder _segments;
            private readonly ImmutableArray<TextRun>.Builder _textRuns;
            private TextRunData _textRunData;
            private uint _textLength;
            private VoiceSegment? _voice;

            public PXmlFlattener()
            {
                _segments = ImmutableArray.CreateBuilder<TextBufferSegment>(4);
                _textRuns = ImmutableArray.CreateBuilder<TextRun>(1);
            }

            public TextBuffer FlattenPXmlContent(PXmlNode pxmlRoot, FontConfiguration fontConfig)
            {
                _fontConfig = fontConfig;
                _segments.Clear();
                _textLength = 0;
                _voice = null;
                Visit(pxmlRoot);
                FinalizeTextRun();
                FinalizeTextSegment();
                return new TextBuffer(_segments.ToImmutable(), _voice, _textLength);
            }

            public override void VisitContent(PXmlContent content)
            {
                foreach (PXmlNode child in content.Children)
                {
                    Visit(child);
                    FinalizeTextRun();
                }
            }

            public override void VisitFontElement(FontElement fontElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.FontSize = fontElement.Size;
                if (fontElement.Color.HasValue)
                {
                    NsColor color = fontElement.Color.Value;
                    _textRunData.Color = color.ToRgbaFloat();
                }
                if (fontElement.OutlineColor.HasValue)
                {
                    NsColor outlineColor = fontElement.OutlineColor.Value;
                    _textRunData.OutlineColor = outlineColor.ToRgbaFloat();
                }

                Visit(fontElement.Content);
                _textRunData = oldData;
            }

            public override void VisitSpanElement(SpanElement spanElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.FontSize = spanElement.Size;
                Visit(spanElement.Content);
                _textRunData = oldData;
            }

            public override void VisitText(PXmlText text)
            {
                if (text.Text.Length > 0)
                {
                    _textRunData.Text = text.Text;
                }
            }

            public override void VisitLinebreakElement(LinebreakElement linebreakElement)
            {
                _textRunData.Text = "\n";
            }

            public override void VisitItalicElement(ItalicElement italicElement)
            {
                bool oldValue = _textRunData.Italic;
                _textRunData.Italic = true;
                VisitContent(italicElement.Content);
                _textRunData.Italic = oldValue;
            }

            public override void VisitRubyElement(RubyElement rubyElement)
            {
                TextRunData oldData = _textRunData;
                _textRunData.RubyText = rubyElement.RubyText;
                Visit(rubyElement.RubyBase);
                _textRunData = oldData;
            }

            public override void VisitHaltElement(HaltElement haltElement)
            {
                FinalizeTextSegment();
                _segments.Add(new MarkerSegment(MarkerKind.Halt));
            }

            public override void VisitNoLinebreaksElement(NoLinebreaksElement element)
            {
                FinalizeTextSegment();
                _segments.Add(new MarkerSegment(MarkerKind.NoLinebreaks));
            }

            public override void VisitVoiceElement(VoiceElement node)
            {
                FinalizeTextSegment();
                _voice = new VoiceSegment(node.CharacterName, node.FileName, node.Action);
                _segments.Add(_voice);
            }

            private void FinalizeTextRun()
            {
                Debug.Assert(_fontConfig is not null);
                ref readonly TextRunData data = ref _textRunData;
                if (data.Text is null) { return; }
                FontFaceKey font = _fontConfig.DefaultFont;
                if (data.Italic && _fontConfig.ItalicFont.HasValue)
                {
                    font = _fontConfig.ItalicFont.Value;
                }

                PtFontSize fontSize = data.FontSize.HasValue
                    ? new PtFontSize(data.FontSize.Value)
                    : _fontConfig.DefaultFontSize;

                RgbaFloat color = data.Color ?? new RgbaFloat(_fontConfig.DefaultTextColor);
                RgbaFloat? outlineColor = data.OutlineColor ?? _fontConfig.DefaultOutlineColor?.ToRgbaFloat();

                TextRun textRun;
                if (data.RubyText is null)
                {
                    textRun = TextRun.Regular(
                        data.Text.AsMemory(),
                        font, fontSize,
                        color, outlineColor
                    );
                }
                else
                {
                    textRun = TextRun.WithRubyText(
                        data.Text.AsMemory(),
                        data.RubyText.AsMemory(),
                        font, fontSize,
                        color, outlineColor
                    );
                }
                _textRuns.Add(textRun);
                _textRunData.RubyText = null;
                _textRunData.Text = null;
                //_textRunData = default;
            }

            private void FinalizeTextSegment()
            {
                if (_textRuns.Count > 0)
                {
                    _segments.Add(new TextSegment(_textRuns.ToImmutable()));
                    _textRuns.Clear();
                }
            }
        }
    }

    internal enum TextBufferSegmentKind
    {
        Text,
        Voice,
        Marker
    }

    internal abstract class TextBufferSegment
    {
        public abstract TextBufferSegmentKind SegmentKind { get; }
    }

    internal sealed class TextSegment : TextBufferSegment
    {
        public TextSegment(ImmutableArray<TextRun> textRuns)
        {
            TextRuns = textRuns;
        }

        public ImmutableArray<TextRun> TextRuns { get; }
        public override TextBufferSegmentKind SegmentKind => TextBufferSegmentKind.Text;
    }

    internal sealed class VoiceSegment : TextBufferSegment
    {
        public VoiceSegment(string characterName, string fileName, NsVoiceAction action)
        {
            CharacterName = characterName;
            FileName = fileName;
            Action = action;
        }

        public string CharacterName { get; }
        public string FileName { get; }
        public NsVoiceAction Action { get; }

        public override TextBufferSegmentKind SegmentKind => TextBufferSegmentKind.Voice;
    }

    internal enum MarkerKind
    {
        Halt,
        NoLinebreaks
    }

    internal sealed class MarkerSegment : TextBufferSegment
    {
        public MarkerSegment(MarkerKind kind)
        {
            MarkerKind = kind;
        }

        public MarkerKind MarkerKind { get; }
        public override TextBufferSegmentKind SegmentKind => TextBufferSegmentKind.Marker;
    }
}

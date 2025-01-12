﻿using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax.PXml
{
    public class PXmlSyntaxVisitor
    {
        public void Visit(PXmlNode node)
        {
            node.Accept(this);
        }

        public void VisitArray(ImmutableArray<PXmlNode> list)
        {
            foreach (PXmlNode node in list)
            {
                Visit(node);
            }
        }

        public virtual void VisitContent(PXmlContent content)
        {
        }

        public virtual void VisitText(PXmlText text)
        {
        }

        public virtual void VisitVoiceElement(VoiceElement voiceElement)
        {
        }

        public virtual void VisitFontElement(FontElement fontElement)
        {
        }

        public virtual void VisitSpanElement(SpanElement spanElement)
        {
        }

        public virtual void VisitRubyElement(RubyElement rubyElement)
        {
        }

        public virtual void VisitHaltElement(HaltElement haltElement)
        {
        }

        public virtual void VisitNoLinebreaksElement(NoLinebreaksElement noLinebreaksElement)
        {
        }

        public virtual void VisitItalicElement(ItalicElement italicElement)
        {
        }

        public virtual void VisitLinebreakElement(LinebreakElement linebreakElement)
        {
        }
    }
}

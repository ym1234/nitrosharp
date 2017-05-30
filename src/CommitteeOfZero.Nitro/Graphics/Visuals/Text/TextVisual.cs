﻿using CommitteeOfZero.Nitro.Foundation;
using System.Drawing;

namespace CommitteeOfZero.Nitro.Graphics
{
    public sealed class TextVisual : Visual
    {
        public TextVisual(string text, SizeF layoutBounds, RgbaValueF color, int priority)
            : base(color, priority)
        {
            Text = text;
            LayoutBounds = layoutBounds;
        }

        public TextVisual(string text, SizeF layoutBounds)
            : this(text, layoutBounds, RgbaValueF.White, 0)
        {
        }

        public TextVisual(string text, float width, float height)
            : this(text, new SizeF(width, height))
        {
        }

        public string Text { get; }
        public SizeF LayoutBounds { get; }
        public TextRange VisibleRegion { get; set; }
        public TextRange AnimatedRegion { get; set; }
        public float AnimatedOpacity { get; set; }

        public override void Render(ICanvas canvas)
        {
            canvas.DrawText(this);
        }

        public override void Free(ICanvas canvas)
        {
            canvas.Free(this);
        }

        public override SizeF Measure() => LayoutBounds;
    }
}

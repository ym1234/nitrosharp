﻿using System;
using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class RectangleRenderer : GameSystem
    {
        private readonly QuadBatcher _quadBatcher;

        public RectangleRenderer(RenderContext renderContext)
        {
            _quadBatcher = renderContext.QuadBatcher;
        }

        public void ProcessRectangles(Rectangles rectangles)
        {
            TransformProcessor.ProcessTransforms(rectangles);
            ProcessRectangles(rectangles.Bounds.Enumerate(),
                rectangles.Colors.Enumerate(),
                rectangles.RenderPriorities.Enumerate(),
                rectangles.TransformMatrices.Enumerate());
        }

        public void ProcessRectangles(
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<RgbaFloat> colors,
            ReadOnlySpan<int> priorities,
            ReadOnlySpan<Matrix4x4> transforms)
        {
            QuadBatcher quadBatcher = _quadBatcher;
            for (int i = 0; i < bounds.Length; i++)
            {
                SizeF size = bounds[i];
                ref readonly RgbaFloat color = ref colors[i];
                if (color.A > 0)
                {
                    quadBatcher.SetTransform(transforms[i]);

                    RgbaFloat c = color;
                    quadBatcher.FillRectangle(0, 0, size.Width, size.Height, ref c, priorities[i]);
                }
            }
        }
    }
}

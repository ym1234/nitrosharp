﻿using System;
using System.Numerics;

namespace NitroSharp.Animation
{
    internal sealed class RotateAnimation : Vector3Animation<Transform>
    {
        public RotateAnimation(Transform transform, Vector3 srcRotation, Vector3 dstRotation,
            TimeSpan duration, TimingFunction timingFunction = TimingFunction.Linear)
            : base(transform, (t, v) => t.Rotation = v, srcRotation, dstRotation, duration, timingFunction)
        {
        }
    }
}

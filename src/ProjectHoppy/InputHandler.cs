﻿using HoppyFramework;
using HoppyFramework.Input;

namespace ProjectHoppy
{
    public class InputHandler : GameSystem
    {
        private readonly N2System _n2system;

        public InputHandler(N2System n2system)
        {
            _n2system = n2system;
        }

        public override void Update(float deltaMilliseconds)
        {
            if (Mouse.IsButtonDownThisFrame(MouseButton.Left))
            {
                _n2system.Interpreter.ResumeThread(_n2system.CurrentThread);
            }
        }
    }
}
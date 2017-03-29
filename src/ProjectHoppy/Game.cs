﻿using SciAdvNet.MediaLayer.Audio;
using SciAdvNet.MediaLayer.Audio.XAudio;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Graphics.DirectX;
using SciAdvNet.MediaLayer.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public abstract class Game
    {
        private volatile bool _running;
        private readonly Stopwatch _gameTimer = new Stopwatch();

        private readonly List<Action> _startupTasks;
        private bool _allowNewStartupTasks;

        public RenderContext RenderContext { get; private set; }
        public AudioEngine AudioEngine { get; private set; }
        public Window Window { get; private set; }
        public EntityManager Entities { get; }
        public SystemManager Systems { get; }

        public Game()
        {
            Entities = CreateEntityManager();
            Systems = new SystemManager(Entities);

            _startupTasks = new List<Action>();
            _allowNewStartupTasks = true;
        }

        public virtual EntityManager CreateEntityManager() => new EntityManager(_gameTimer);

        public void AddStartupTask(Action action)
        {
            if (!_allowNewStartupTasks)
            {
                throw new InvalidOperationException();
            }

            _startupTasks.Add(action);
        }

        private void InitializeGraphics()
        {
            Window = new GameWindow();
            Window.WindowState = WindowState.Normal;
            RenderContext = new DXRenderContext(Window);
            AudioEngine = new XAudio2AudioEngine(16, 44100, 2);

            OnInitialized();
        }

        public virtual void OnInitialized()
        {
        }

        public virtual void Update(float deltaMilliseconds)
        {
            Systems.Update(deltaMilliseconds);
        }

        public void EnterLoop()
        {
            _running = true;
            _gameTimer.Start();

            float prevFrameTicks = 0.0f;
            while (_running && Window.Exists)
            {
                long currentFrameTicks = _gameTimer.ElapsedTicks;
                float deltaMilliseconds = (currentFrameTicks - prevFrameTicks) / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                Window.ProcessEvents();
                Update(deltaMilliseconds);
            }

            Shutdown();
        }

        public void Run()
        {
            Task.WhenAll(_startupTasks.Select(x => Task.Run(x))).Wait();
            InitializeGraphics();
            EnterLoop();
        }

        public virtual void Shutdown()
        {
            Systems.Dispose();
            RenderContext.Dispose();
        }
    }
}

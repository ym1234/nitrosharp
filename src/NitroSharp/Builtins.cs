﻿using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class Builtins : BuiltInFunctions
    {
        private readonly GameContext _ctx;
        private readonly RenderContext _renderCtx;
        private readonly World _world;

        public Builtins(GameContext context)
        {
            _ctx = context;
            _renderCtx = context.RenderContext;
            _world = _ctx.World;
        }

        private Entity? Get(in EntityPath entityPath)
            => _world.Get(CurrentThread.Id, entityPath);

        private SmallList<Entity> Query(EntityQuery query)
        {
            SmallList<Entity> results = _world.Query(CurrentThread.Id, query);
            if (results.Count == 0)
            {
                EmptyResults(query);
            }
            return results;
        }

        private QueryResultsEnumerable<T> Query<T>(EntityQuery query) where T : Entity
        {
            QueryResultsEnumerable<T> results = _world.Query<T>(CurrentThread.Id, query);
            if (results.IsEmpty)
            {
                EmptyResults(query);
            }
            return results;
        }

        private void EmptyResults(EntityQuery query) { }
            //=> Console.WriteLine($"Query '{query.Value}' yielded no results.");

        private bool ResolvePath(in EntityPath path, out ResolvedEntityPath resolvedPath)
        {
            return _world.ResolvePath(CurrentThread.Id, path, out resolvedPath);
        }

        public override void Exit()
        {
            _ctx.ShutdownSignal.Cancel();
        }

        public override ConstantValue FormatString(string format, object[] args)
        {
            return ConstantValue.String(CRuntime.sprintf(format, args));
        }

        public override void CreateEntity(in EntityPath path)
        {
            if (ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new SimpleEntity(resolvedPath));
            }
        }

        public override void Request(EntityQuery query, NsEntityAction action)
        {
            foreach (Entity entity in Query(query))
            {
                switch (entity, action)
                {
                    case (RenderItem2D ri, NsEntityAction.SetAdditiveBlend):
                        ri.BlendMode = BlendMode.Additive;
                        break;
                    case (RenderItem2D ri, NsEntityAction.SetReverseSubtractiveBlend):
                        ri.BlendMode = BlendMode.ReverseSubtractive;
                        break;
                    case (RenderItem2D ri, NsEntityAction.SetMultiplicativeBlend):
                        ri.BlendMode = BlendMode.Multiplicative;
                        break;
                    case (RenderItem2D ri, NsEntityAction.EnableFiltering):
                        ri.FilterMode = FilterMode.Linear;
                        break;
                    case (VmThread thread, NsEntityAction.Start):
                        thread.Restart();
                        break;
                    case (VmThread thread, NsEntityAction.Resume):
                        thread.Resume();
                        break;
                    case (VmThread thread, NsEntityAction.Pause):
                        thread.Suspend();
                        break;
                    case (VmThread thread, NsEntityAction.Stop):
                        thread.Suspend();
                        break;
                    case (RenderItem ri, NsEntityAction.Enable):
                        if (ri.Parent is Choice)
                        {
                            ri.Reveal();
                        }
                        else
                        {
                            _world.EnableEntity(ri);
                        }
                        break;
                    case (RenderItem ri, NsEntityAction.Disable):
                        if (ri.Parent is Choice)
                        {
                            ri.Hide();
                        }
                        else
                        {
                            _world.DisableEntity(ri);
                        }
                        break;
                    case (_, NsEntityAction.Enable):
                        _world.EnableEntity(entity);
                        break;
                    case (_, NsEntityAction.Disable):
                        _world.DisableEntity(entity);
                        break;
                    case (_, NsEntityAction.DestroyWhenIdle):
                        _world.DestroyWhenIdle(entity);
                        break;
                    case (_, NsEntityAction.Lock):
                        entity.Lock();
                        break;
                    case (_, NsEntityAction.Unlock):
                        entity.Unlock();
                        break;
                }
            }
        }

        public override void SetAlias(in EntityPath entityPath, in EntityPath alias)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.SetAlias(resolvedPath.Id, alias);
            }
        }

        public override void DestroyEntities(EntityQuery query)
        {
            foreach (Entity entity in Query(query))
            {
                if (!entity.IsLocked)
                {
                    _world.DestroyEntity(entity);
                }
            }
        }

        public override void CreateThread(in EntityPath entityPath, string target)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new VmThread(resolvedPath, _ctx.VM, target));
            }
        }

        public override void Delay(TimeSpan delay)
        {
            if (delay > TimeSpan.Zero)
            {
                _ctx.Wait(CurrentThread, WaitCondition.None, delay);
            }
        }

        public override void WaitAction(EntityQuery query, TimeSpan? timeout)
        {
            _ctx.Wait(CurrentThread, WaitCondition.EntityIdle, timeout, query);
        }

        public override void WaitMove(EntityQuery query)
        {
            _ctx.Wait(CurrentThread, WaitCondition.MoveCompleted, timeout: null, query);
        }

        public override void WaitForInput()
        {
            _ctx.Wait(CurrentThread, WaitCondition.UserInput);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            _ctx.Wait(CurrentThread, WaitCondition.UserInput, timeout);
        }

        public override void MoveCursor(int x, int y)
        {
            _ctx.Window.SetMousePosition(new Vector2(x, y));
        }
    }
}
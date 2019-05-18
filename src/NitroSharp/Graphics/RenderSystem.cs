﻿using System;
using System.Numerics;
using NitroSharp.Graphics.Renderers;
using NitroSharp.Media;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics.Systems
{
    internal sealed class RenderSystem : IDisposable
    {
        private const ushort MainBucketSize = 512;

        private readonly World _world;
        private readonly GraphicsDevice _gd;
        private readonly Swapchain _swapchain;

        private readonly CommandList _cl;

        private readonly ResourceLayout _viewProjectionLayout;
        private readonly ResourceSet _viewProjectionSet;
        private readonly DeviceBuffer _viewProjectionBuffer;

        private readonly Texture _whiteTexture;
        private readonly RenderBucket<RenderItemKey> _mainBucket;
        private readonly QuadGeometryStream _quadGeometryStream;
        private readonly QuadBatcher _quadBatcher;
        private readonly SpriteRenderer _spriteRenderer;
        private readonly RectangleRenderer _quadRenderer;
        private readonly TextRenderer _textRenderer;
        private readonly VideoRenderer _videoRenderer;

        public RenderContext RenderContext { get; }

        private readonly ResourceSetCache _resourceSetCache;
        private readonly ShaderLibrary _shaderLibrary;
        private readonly TexturePool _texturePool;

        public RenderSystem(
            World world,
            GraphicsDevice device,
            Swapchain swapchain,
            ContentManager content,
            FontService fontService,
            Configuration gameConfiguration)
        {
            _world = world;
            _gd = device;
            _swapchain = swapchain;

            DesignResolution = new SizeF(gameConfiguration.WindowWidth, gameConfiguration.WindowHeight);

            ResourceFactory factory = _gd.ResourceFactory;
            _cl = factory.CreateCommandList();
            _cl.Name = "Main";

            _resourceSetCache = new ResourceSetCache(_gd.ResourceFactory);
            _shaderLibrary = new ShaderLibrary(_gd);
            _texturePool = new TexturePool(_gd, PixelFormat.R8_G8_B8_A8_UNorm);

            _viewProjectionLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                )
            ));

            var projection = Matrix4x4.CreateOrthographicOffCenter(
                left: 0, right: DesignResolution.Width,
                bottom: DesignResolution.Height, top: 0,
                zNearPlane: 0.0f, zFarPlane: -1.0f
            );

            _viewProjectionBuffer = _gd.CreateStaticBuffer(ref projection, BufferUsage.UniformBuffer);
            _viewProjectionSet = factory.CreateResourceSet(
                new ResourceSetDescription(
                    _viewProjectionLayout,
                    _viewProjectionBuffer
                )
            );
            var viewProjection = new ViewProjection(
                _viewProjectionLayout,
                _viewProjectionSet,
                _viewProjectionBuffer
            );

            _mainBucket = new RenderBucket<RenderItemKey>(MainBucketSize);
            _quadGeometryStream = new QuadGeometryStream(device);
          
            _whiteTexture = CreateWhiteTexture();

            var context = new RenderContext
            {
                Device = device,
                ResourceFactory = factory,
                MainSwapchain = swapchain,
                MainFramebuffer = swapchain.Framebuffer,
                MainCommandList = _cl,
                ShaderLibrary = _shaderLibrary,
                TexturePool = _texturePool,
                ResourceSetCache = _resourceSetCache,
                FontService = fontService,
                ViewProjection = viewProjection,
                MainBucket = _mainBucket,
                QuadGeometryStream = _quadGeometryStream,
                WhiteTexture = _whiteTexture,
                DesignResolution = new Size((uint)DesignResolution.Width, (uint)DesignResolution.Height)
            };

            context.QuadBatcher = context.CreateQuadBatcher(_mainBucket, _swapchain.Framebuffer);
            _quadBatcher = context.QuadBatcher;

            _spriteRenderer = new SpriteRenderer(world, context, content);
            _quadRenderer = new RectangleRenderer(world, context);
            _textRenderer = new TextRenderer(world, context);
            _videoRenderer = new VideoRenderer(world, context, content);

            RenderContext = context;
        }

        private SizeF DesignResolution { get; }

        private Texture CreateWhiteTexture()
        {
            Texture stagingWhite = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            MappedResourceView<RgbaByte> pixels = _gd.Map<RgbaByte>(stagingWhite, MapMode.Write);
            pixels[0] = RgbaByte.White;
            _gd.Unmap(stagingWhite);

            Texture texture = _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                 width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            _cl.Begin();
            _cl.CopyTexture(stagingWhite, texture);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.DisposeWhenIdle(stagingWhite);
            return texture;
        }

        public void ProcessTransforms()
        {
            TransformProcessor.ProcessTransforms(_world, _world.Sprites);
        }

        public void RenderFrame()
        {
            _cl.Begin();
            _cl.SetFramebuffer(_swapchain.Framebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);

            _mainBucket.Begin();
            _quadGeometryStream.Begin();

            _spriteRenderer.ProcessSprites();
            _quadRenderer.ProcessRectangles(_world.Rectangles);
            _textRenderer.ProcessTextLayouts();
            _videoRenderer.ProcessVideoClips();

            _quadGeometryStream.End(_cl);
            _mainBucket.End(_cl);

            _cl.End();
            _gd.SubmitCommands(_cl);
        }

        public void Present()
        {
            _gd.SwapBuffers(_swapchain);
        }

        public void Dispose()
        {
            _textRenderer.Dispose();
            _videoRenderer.Dispose();

            _quadBatcher.Dispose();
            _quadGeometryStream.Dispose();
            _resourceSetCache.Dispose();
            _texturePool.Dispose();
            _shaderLibrary.Dispose();
            _whiteTexture.Dispose();

            _viewProjectionSet.Dispose();
            _viewProjectionBuffer.Dispose();
            _viewProjectionLayout.Dispose();

            _cl.Dispose();
        }
    }
}

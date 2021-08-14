using System;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Jint;
using JSPool;
using Wyam.Common.JavaScript;
using IJavaScriptEngine = Wyam.Common.JavaScript.IJavaScriptEngine;

namespace Wyam.Core.JavaScript
{
    internal class JavaScriptEnginePool : IJavaScriptEnginePool
    {
        private readonly JsPool<PooledJavaScriptEngine, IJavaScriptEngine> _pool;
        private bool _disposed = false;

        public JavaScriptEnginePool(
                Action<IJavaScriptEngine> initializer,
                int startEngines,
                int maxEngines,
                int maxUsagesPerEngine,
                TimeSpan engineTimeout)
        {
            // First we need to check if the JsEngineSwitcher has been configured. We'll do this
            // by checking the DefaultEngineName being set. If that's there we can safely assume
            // its been configured somehow (maybe via a configuration file). If not we'll wire up
            // Jint as the default engine.
            if (string.IsNullOrWhiteSpace(JsEngineSwitcher.Current.DefaultEngineName))
            {
                JsEngineSwitcher.Current.EngineFactories.Add(new JintJsEngineFactory());
                JsEngineSwitcher.Current.DefaultEngineName = JintJsEngine.EngineName;
            }

            _pool = new JsPool<PooledJavaScriptEngine, IJavaScriptEngine>(new JsPoolConfig<IJavaScriptEngine>
            {
                EngineFactory = () => new JavaScriptEngine(JsEngineSwitcher.Current.CreateDefaultEngine()),
                Initializer = x => initializer?.Invoke(x),
                StartEngines = startEngines,
                MaxEngines = maxEngines,
                MaxUsagesPerEngine = maxUsagesPerEngine,
                GetEngineTimeout = engineTimeout
            });
        }

        /// <inheritdoc />
        public IJavaScriptEngine GetEngine(TimeSpan? timeout = null) => new PooledJavaScriptEngine(_pool.GetEngine(timeout));

        /// <inheritdoc />
        public int EngineCount => _pool.EngineCount;

        /// <inheritdoc />
        public int AvailableEngineCount => _pool.AvailableEngineCount;

        /// <inheritdoc />
        public void DisposeEngine(IJavaScriptEngine engine, bool repopulateEngines = true)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (!(engine is PooledJavaScriptEngine pooledEngine))
            {
                throw new ArgumentException("The specified engine was not from a pool");
            }

            _pool.DisposeEngine(pooledEngine);
        }

        public void Recycle() => _pool.Recycle();

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JsPool));
            }
        }

        public void Dispose()
        {
            CheckDisposed();
            _pool.Dispose();
            _disposed = true;
        }
    }
}

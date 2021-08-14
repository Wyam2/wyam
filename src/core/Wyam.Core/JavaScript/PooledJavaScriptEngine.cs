using System;
using System.Reflection;
using System.Text;
using JSPool;
using Wyam.Common.JavaScript;

namespace Wyam.Core.JavaScript
{
    /// <summary>
    /// Wraps a <see cref="JavaScriptEngine"/> but overrides the
    /// dispose behavior so that instead of disposing the
    /// underlying engine, it returns the engine to the pool.
    /// </summary>
    internal class PooledJavaScriptEngine : PooledObject<IJavaScriptEngine>, IJavaScriptEngine
    {
        private bool _disposed = false;

        public PooledJavaScriptEngine()
        {
        }

        public PooledJavaScriptEngine(IJavaScriptEngine engine)
        {
            Engine = engine;
        }

        internal IJavaScriptEngine Engine { get; }

        /// <summary>
        /// Callback for returning the engine to the pool.
        /// </summary>
        internal Action ReturnEngineToPool { private get; set; }

        /// <summary>
        /// Increase engine usage count by one.
        /// </summary>
        internal void IncreaseUsageCount()
        {
            UsageCount++;
        }

        /// <summary>
        /// Gets the number of times this engine has been used.
        /// </summary>
        public int UsageCount { get; private set; } = 0;

        /// <inheritdoc />
        public string Name
        {
            get
            {
                CheckDisposed();
                return Engine.Name;
            }
        }

        /// <inheritdoc />
        public string Version
        {
            get
            {
                CheckDisposed();
                return Engine.Version;
            }
        }

        /// <inheritdoc />
        public bool SupportsScriptPrecompilation
        {
            get
            {
                CheckDisposed();
                return Engine.SupportsScriptPrecompilation;
            }
        }

        /// <inheritdoc />
        public bool SupportsScriptInterruption
        {
            get
            {
                CheckDisposed();
                return Engine.SupportsScriptInterruption;
            }
        }

        /// <inheritdoc />
        public bool SupportsGarbageCollection
        {
            get
            {
                CheckDisposed();
                return Engine.SupportsGarbageCollection;
            }
        }

        /// <inheritdoc />
        public IPrecompiledJavaScript Precompile(string code)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public IPrecompiledJavaScript Precompile(string code, string documentName)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public IPrecompiledJavaScript PrecompileFile(string path, Encoding encoding = null)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public IPrecompiledJavaScript PrecompileResource(string resourceName, Type type)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public IPrecompiledJavaScript PrecompileResource(string resourceName, Assembly assembly)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public object Evaluate(string expression)
        {
            CheckDisposed();
            return Engine.Evaluate(expression);
        }

        /// <inheritdoc />
        public object Evaluate(string expression, string documentName)
        {
            CheckDisposed();
            return Engine.Evaluate(expression, documentName);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression)
        {
            CheckDisposed();
            return Engine.Evaluate<T>(expression);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression, string documentName)
        {
            CheckDisposed();
            return Engine.Evaluate<T>(expression, documentName);
        }

        /// <inheritdoc />
        public void Execute(string code)
        {
            CheckDisposed();
            Engine.Execute(code);
        }

        /// <inheritdoc />
        public void Execute(string code, string documentName)
        {
            CheckDisposed();
            Engine.Execute(code, documentName);
        }

        /// <inheritdoc />
        public void Execute(IPrecompiledJavaScript precompiledScript)
        {
            throw new NotImplementedException("Currently not used in Wyam");
        }

        /// <inheritdoc />
        public void ExecuteFile(string path, Encoding encoding = null)
        {
            CheckDisposed();
            Engine.ExecuteFile(path, encoding);
        }

        /// <inheritdoc />
        public void ExecuteResource(string resourceName, Type type)
        {
            CheckDisposed();
            Engine.ExecuteResource(resourceName, type);
        }

        /// <inheritdoc />
        public void ExecuteResource(string resourceName, Assembly assembly)
        {
            CheckDisposed();
            Engine.ExecuteResource(resourceName, assembly);
        }

        /// <inheritdoc />
        public object CallFunction(string functionName, params object[] args)
        {
            CheckDisposed();
            return Engine.CallFunction(functionName, args);
        }

        /// <inheritdoc />
        public T CallFunction<T>(string functionName, params object[] args)
        {
            CheckDisposed();
            return Engine.CallFunction<T>(functionName, args);
        }

        /// <inheritdoc />
        public bool HasVariable(string variableName)
        {
            CheckDisposed();
            return Engine.HasVariable(variableName);
        }

        /// <inheritdoc />
        public object GetVariableValue(string variableName)
        {
            CheckDisposed();
            return Engine.GetVariableValue(variableName);
        }

        /// <inheritdoc />
        public T GetVariableValue<T>(string variableName)
        {
            CheckDisposed();
            return Engine.GetVariableValue<T>(variableName);
        }

        /// <inheritdoc />
        public void SetVariableValue(string variableName, object value)
        {
            CheckDisposed();
            Engine.SetVariableValue(variableName, value);
        }

        /// <inheritdoc />
        public void RemoveVariable(string variableName)
        {
            CheckDisposed();
            Engine.RemoveVariable(variableName);
        }

        /// <inheritdoc />
        public void EmbedHostObject(string itemName, object value)
        {
            CheckDisposed();
            Engine.EmbedHostObject(itemName, value);
        }

        /// <inheritdoc />
        public void EmbedHostType(string itemName, Type type)
        {
            CheckDisposed();
            Engine.EmbedHostType(itemName, type);
        }

        /// <inheritdoc />
        public void Interrupt()
        {
            CheckDisposed();

            Engine.Interrupt();
        }

        /// <inheritdoc />
        public void CollectGarbage()
        {
            CheckDisposed();

            Engine.CollectGarbage();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PooledJavaScriptEngine));
            }
        }

        /// <summary>
        /// Returns this engine to the pool.
        /// </summary>
        public new void Dispose()
        {
            CheckDisposed();
            ReturnEngineToPool();
            _disposed = true;
        }
    }
}
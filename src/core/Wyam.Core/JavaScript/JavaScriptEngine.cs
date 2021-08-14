using System;
using System.Reflection;
using System.Text;
using JavaScriptEngineSwitcher.Core;
using Wyam.Common.JavaScript;

namespace Wyam.Core.JavaScript
{
    /// <inheritdoc />
    internal class JavaScriptEngine : IJavaScriptEngine
    {
        private readonly IJsEngine _engine;
        private bool _disposed = false;

        public JavaScriptEngine(IJsEngine engine)
        {
            _engine = engine;
        }

        public void Dispose()
        {
            CheckDisposed();
            _engine.Dispose();
            _disposed = true;
        }

        /// <inheritdoc />
        public string Name
        {
            get
            {
                CheckDisposed();
                return _engine.Name;
            }
        }

        /// <inheritdoc />
        public string Version
        {
            get
            {
                CheckDisposed();
                return _engine.Version;
            }
        }

        /// <inheritdoc />
        public bool SupportsScriptPrecompilation
        {
            get
            {
                CheckDisposed();
                return _engine.SupportsScriptPrecompilation;
            }
        }

        /// <inheritdoc />
        public bool SupportsScriptInterruption
        {
            get
            {
                CheckDisposed();
                return _engine.SupportsScriptInterruption;
            }
        }

        /// <inheritdoc />
        public bool SupportsGarbageCollection
        {
            get
            {
                CheckDisposed();
                return _engine.SupportsGarbageCollection;
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
            return _engine.Evaluate(expression);
        }

        /// <inheritdoc />
        public object Evaluate(string expression, string documentName)
        {
            CheckDisposed();
            return _engine.Evaluate(expression, documentName);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression)
        {
            CheckDisposed();
            return _engine.Evaluate<T>(expression);
        }

        /// <inheritdoc />
        public T Evaluate<T>(string expression, string documentName)
        {
            CheckDisposed();
            return _engine.Evaluate<T>(expression, documentName);
        }

        /// <inheritdoc />
        public void Execute(string code)
        {
            CheckDisposed();
            _engine.Execute(code);
        }

        /// <inheritdoc />
        public void Execute(string code, string documentName)
        {
            CheckDisposed();
            _engine.Execute(code, documentName);
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
            _engine.ExecuteFile(path, encoding);
        }

        /// <inheritdoc />
        public void ExecuteResource(string resourceName, Type type)
        {
            CheckDisposed();
            _engine.ExecuteResource(resourceName, type);
        }

        /// <inheritdoc />
        public void ExecuteResource(string resourceName, Assembly assembly)
        {
            CheckDisposed();
            _engine.ExecuteResource(resourceName, assembly);
        }

        /// <inheritdoc />
        public object CallFunction(string functionName, params object[] args)
        {
            CheckDisposed();
            return _engine.CallFunction(functionName, args);
        }

        /// <inheritdoc />
        public T CallFunction<T>(string functionName, params object[] args)
        {
            CheckDisposed();
            return _engine.CallFunction<T>(functionName, args);
        }

        /// <inheritdoc />
        public bool HasVariable(string variableName)
        {
            CheckDisposed();
            return _engine.HasVariable(variableName);
        }

        /// <inheritdoc />
        public object GetVariableValue(string variableName)
        {
            CheckDisposed();
            return _engine.GetVariableValue(variableName);
        }

        /// <inheritdoc />
        public T GetVariableValue<T>(string variableName)
        {
            CheckDisposed();
            return _engine.GetVariableValue<T>(variableName);
        }

        /// <inheritdoc />
        public void SetVariableValue(string variableName, object value)
        {
            CheckDisposed();
            _engine.SetVariableValue(variableName, value);
        }

        /// <inheritdoc />
        public void RemoveVariable(string variableName)
        {
            CheckDisposed();
            _engine.RemoveVariable(variableName);
        }

        /// <inheritdoc />
        public void EmbedHostObject(string itemName, object value)
        {
            CheckDisposed();
            _engine.EmbedHostObject(itemName, value);
        }

        /// <inheritdoc />
        public void EmbedHostType(string itemName, Type type)
        {
            CheckDisposed();
            _engine.EmbedHostType(itemName, type);
        }

        /// <inheritdoc />
        public void Interrupt()
        {
            CheckDisposed();

            _engine.Interrupt();
        }

        /// <inheritdoc />
        public void CollectGarbage()
        {
            CheckDisposed();

            _engine.CollectGarbage();
        }
        
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JavaScriptEngine));
            }
        }
    }
}

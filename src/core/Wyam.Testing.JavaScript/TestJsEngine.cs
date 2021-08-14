using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Jint;
using Wyam.Common.JavaScript;
using IJavaScriptEngine = Wyam.Common.JavaScript.IJavaScriptEngine;

namespace Wyam.Testing.JavaScript
{
    public class TestJsEngine : IJavaScriptEngine
    {
        private readonly JavaScriptEngineSwitcher.Core.IJsEngine _engine;

        static TestJsEngine()
        {
            JsEngineSwitcher.Current.EngineFactories.Add(new JintJsEngineFactory());
            JsEngineSwitcher.Current.DefaultEngineName = JintJsEngine.EngineName;
        }

        public TestJsEngine()
        {
            _engine = JsEngineSwitcher.Current.CreateDefaultEngine();
        }

        public TestJsEngine(string engineName)
        {
            _engine = JsEngineSwitcher.Current.CreateEngine(engineName);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        public string Name => _engine.Name;

        public string Version => _engine.Version;

        public bool SupportsScriptPrecompilation => _engine.SupportsScriptPrecompilation;

        public bool SupportsScriptInterruption => _engine.SupportsScriptInterruption;

        public bool SupportsGarbageCollection => _engine.SupportsGarbageCollection;

        public IPrecompiledJavaScript Precompile(string code) => throw new NotImplementedException("Not used by Wyam2");

        public IPrecompiledJavaScript Precompile(string code, string documentName) => throw new NotImplementedException("Not used by Wyam2");

        public IPrecompiledJavaScript PrecompileFile(string path, Encoding encoding = null) => throw new NotImplementedException("Not used by Wyam2");

        public IPrecompiledJavaScript PrecompileResource(string resourceName, Type type) => throw new NotImplementedException("Not used by Wyam2");

        public IPrecompiledJavaScript PrecompileResource(string resourceName, Assembly assembly) => throw new NotImplementedException("Not used by Wyam2");

        public object Evaluate(string expression) => _engine.Evaluate(expression);

        public object Evaluate(string expression, string documentName) => _engine.Evaluate(expression, documentName);

        public T Evaluate<T>(string expression) => _engine.Evaluate<T>(expression);

        public T Evaluate<T>(string expression, string documentName) => _engine.Evaluate<T>(expression, documentName);

        public void Execute(string code) => _engine.Execute(code);

        public void Execute(string code, string documentName) => _engine.Execute(code, documentName);

        public void Execute(IPrecompiledJavaScript precompiledScript) => throw new NotImplementedException("Not used by Wyam2");

        public void ExecuteFile(string path, Encoding encoding = null) => _engine.ExecuteFile(path, encoding);

        public void ExecuteResource(string resourceName, Type type) => _engine.ExecuteResource(resourceName, type);

        public void ExecuteResource(string resourceName, Assembly assembly) => _engine.ExecuteResource(resourceName, assembly);

        public object CallFunction(string functionName, params object[] args) => _engine.CallFunction(functionName, args);

        public T CallFunction<T>(string functionName, params object[] args) => _engine.CallFunction<T>(functionName, args);

        public bool HasVariable(string variableName) => _engine.HasVariable(variableName);

        public object GetVariableValue(string variableName) => _engine.GetVariableValue(variableName);

        public T GetVariableValue<T>(string variableName) => _engine.GetVariableValue<T>(variableName);

        public void SetVariableValue(string variableName, object value) => _engine.SetVariableValue(variableName, value);

        public void RemoveVariable(string variableName) => _engine.RemoveVariable(variableName);

        public void EmbedHostObject(string itemName, object value) => _engine.EmbedHostObject(itemName, value);

        public void EmbedHostType(string itemName, Type type) => _engine.EmbedHostType(itemName, type);

        public void Interrupt() => _engine.Interrupt();

        public void CollectGarbage() => _engine.CollectGarbage();
    }
}

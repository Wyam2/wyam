namespace Wyam.Common.JavaScript
{
    /// <summary>
    /// Represents a pre-compiled JavaScript script that can be executed by different instances of the JavaScript engine.
    /// </summary>
    public interface IPrecompiledJavaScript
    {
        /// <summary>
        /// Gets a name of JavaScript engine for which the pre-compiled script was created.
        /// </summary>
        string EngineName { get; }
    }
}
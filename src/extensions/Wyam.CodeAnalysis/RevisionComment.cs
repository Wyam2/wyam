namespace Wyam.CodeAnalysis
{
    /// <summary>
    /// Represents a <i>revision</i> XML comment element
    /// </summary>
    public class RevisionComment
    {
        /// <summary>
        /// Revision date in MM/dd/yyyy format for compatibility with SHFB
        /// </summary>
        public string Date { get; }
        
        /// <summary>
        /// Revision version
        /// </summary>
        public string Version { get; }
        
        /// <summary>
        /// Revision author
        /// </summary>
        public string Author { get; }
        
        /// <summary>
        /// Revision description as HTML
        /// </summary>
        public string Html { get; }
        
        public RevisionComment(string date, string version, string author, string html)
        {
            Date = date;
            Version = version;
            Author = author;
            Html = html;
        }
    }
}
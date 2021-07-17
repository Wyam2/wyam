namespace Wyam.Configuration
{
    /// <summary>
     /// Lookup data for all known extensions.
     /// </summary>
    public partial class KnownExtension : ClassEnum<KnownExtension>
    {
        // Third-party extension field declarations would go here

        /// <summary>
        /// Gets the package that contains the extension.
        /// </summary>
        public string PackageId { get; }

        private KnownExtension(string packageId)
        {
            PackageId = packageId;
        }

        public static readonly KnownExtension All = new KnownExtension("Wyam2.All");

        public static readonly KnownExtension AmazonWebServices = new KnownExtension("Wyam2.AmazonWebServices");
        public static readonly KnownExtension CodeAnalysis = new KnownExtension("Wyam2.CodeAnalysis");
        public static readonly KnownExtension Feeds = new KnownExtension("Wyam2.Feeds");
        public static readonly KnownExtension GitHub = new KnownExtension("Wyam2.GitHub");
        public static readonly KnownExtension Highlight = new KnownExtension("Wyam2.Highlight");
        public static readonly KnownExtension Html = new KnownExtension("Wyam2.Html");
        public static readonly KnownExtension Images = new KnownExtension("Wyam2.Images");
        public static readonly KnownExtension Json = new KnownExtension("Wyam2.Json");
        public static readonly KnownExtension Less = new KnownExtension("Wyam2.Less");
        public static readonly KnownExtension Markdown = new KnownExtension("Wyam2.Markdown");
        public static readonly KnownExtension Minification = new KnownExtension("Wyam2.Minification");
        public static readonly KnownExtension Razor = new KnownExtension("Wyam2.Razor");
        public static readonly KnownExtension Sass = new KnownExtension("Wyam2.Sass");
        public static readonly KnownExtension SearchIndex = new KnownExtension("Wyam2.SearchIndex");
        public static readonly KnownExtension Tables = new KnownExtension("Wyam2.Tables");
        public static readonly KnownExtension TextGeneration = new KnownExtension("Wyam2.TextGeneration");
        public static readonly KnownExtension Xmp = new KnownExtension("Wyam2.Xmp");
        public static readonly KnownExtension Yaml = new KnownExtension("Wyam2.Yaml");
        public static readonly KnownExtension YouTube = new KnownExtension("Wyam2.YouTube");

        public static readonly KnownExtension BlogTemplateTheme = new KnownExtension("Wyam2.Blog.BlogTemplate");
        public static readonly KnownExtension CleanBlogTheme = new KnownExtension("Wyam2.Blog.CleanBlog");
        public static readonly KnownExtension PhantomTheme = new KnownExtension("Wyam2.Blog.Phantom");
        public static readonly KnownExtension SolidStateTheme = new KnownExtension("Wyam2.Blog.SolidState");
        public static readonly KnownExtension StellarTheme = new KnownExtension("Wyam2.Blog.Stellar");
        public static readonly KnownExtension TrophyTheme = new KnownExtension("Wyam2.Blog.Trophy");
        public static readonly KnownExtension SamsonTheme = new KnownExtension("Wyam2.Docs.Samson");
    }
}

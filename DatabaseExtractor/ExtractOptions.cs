namespace DatabaseExtractor
{
    /// <summary>
    /// Configuration options for database schema extraction.
    /// </summary>
    public class ExtractOptions
    {
        /// <summary>
        /// Gets or sets whether to extract all table data along with schema.
        /// Default: false (schema only)
        /// </summary>
        public bool ExtractAllTableData { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to ignore extended properties during extraction.
        /// Default: false (include extended properties)
        /// </summary>
        public bool IgnoreExtendedProperties { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to verify the extraction after completion.
        /// Default: true
        /// </summary>
        public bool VerifyExtraction { get; set; } = true;
    }
}


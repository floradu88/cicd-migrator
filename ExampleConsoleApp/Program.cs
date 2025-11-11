using System;
using System.IO;
using DatabaseExtractor;

namespace ExampleConsoleApp
{
    /// <summary>
    /// Example console application demonstrating how to use the DatabaseExtractor class library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Database Schema Extractor Example ===");
            Console.WriteLine();

            // ============================================
            // EXAMPLE 1: Basic Usage
            // ============================================
            Console.WriteLine("Example 1: Basic Schema Extraction");
            Console.WriteLine("-----------------------------------");

            // Your Azure SQL Database connection string
            string connectionString = "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=youruser;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            // Output path for the DACPAC file
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyDatabase.dacpac");

            // Uncomment to run:
            /*
            try
            {
                var extractor = new DatabaseSchemaExtractor();
                
                Console.WriteLine($"Extracting schema from database...");
                Console.WriteLine($"Output: {outputPath}");
                Console.WriteLine();

                bool success = extractor.ExtractSchema(connectionString, outputPath);

                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine("✓ Schema extraction completed successfully!");
                    Console.WriteLine($"  DACPAC file: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
            */

            Console.WriteLine("(Code commented out - uncomment to run)");
            Console.WriteLine();

            // ============================================
            // EXAMPLE 2: With Custom Options
            // ============================================
            Console.WriteLine("Example 2: Extraction with Custom Options");
            Console.WriteLine("------------------------------------------");

            /*
            try
            {
                var extractor = new DatabaseSchemaExtractor();
                
                var options = new ExtractOptions
                {
                    ExtractAllTableData = false,        // Schema only, no data
                    IgnoreExtendedProperties = false,    // Include extended properties
                    VerifyExtraction = true              // Verify after extraction
                };

                Console.WriteLine("Extracting with custom options...");
                extractor.ExtractSchema(connectionString, outputPath, options);
                Console.WriteLine("✓ Extraction completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
            */

            Console.WriteLine("(Code commented out - uncomment to run)");
            Console.WriteLine();

            // ============================================
            // EXAMPLE 3: Simplified Method Signature
            // ============================================
            Console.WriteLine("Example 3: Using Simplified Method");
            Console.WriteLine("------------------------------------");

            /*
            try
            {
                var extractor = new DatabaseSchemaExtractor();
                
                // Extract with inline parameters
                extractor.ExtractSchema(
                    connectionString: connectionString,
                    outputDacpacPath: outputPath,
                    extractTableData: false,
                    ignoreExtendedProperties: false,
                    verifyExtraction: true
                );
                
                Console.WriteLine("✓ Extraction completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
            */

            Console.WriteLine("(Code commented out - uncomment to run)");
            Console.WriteLine();

            // ============================================
            // EXAMPLE 4: Error Handling
            // ============================================
            Console.WriteLine("Example 4: Comprehensive Error Handling");
            Console.WriteLine("----------------------------------------");

            /*
            try
            {
                var extractor = new DatabaseSchemaExtractor();
                extractor.ExtractSchema(connectionString, outputPath);
                Console.WriteLine("✓ Success!");
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"✗ Invalid parameter: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✗ Invalid argument: {ex.Message}");
            }
            catch (DacServicesException ex)
            {
                Console.WriteLine($"✗ Extraction failed: {ex.Message}");
                Console.WriteLine($"  This usually indicates a connection or permission issue.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
                Console.WriteLine($"  Type: {ex.GetType().Name}");
            }
            */

            Console.WriteLine("(Code commented out - uncomment to run)");
            Console.WriteLine();

            Console.WriteLine("===========================================");
            Console.WriteLine("To use these examples:");
            Console.WriteLine("1. Update the connectionString variable with your Azure SQL connection string");
            Console.WriteLine("2. Uncomment the example you want to run");
            Console.WriteLine("3. Build and run the application");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}


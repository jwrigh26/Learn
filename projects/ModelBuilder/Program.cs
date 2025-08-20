using System.Text.Json;
using ModelBuilder;
using ModelBuilder.Preprocessing;

// ModelBuilder: Combines individual intent JSON files into a single training dataset.
// This tool reads all .json files from the Data directory and combines them into 
// a versioned dataset files for ML model training.

Console.WriteLine("ModelBuilder: Intent Dataset Generator");
Console.WriteLine("=====================================");

// Load stopwords once at startup
var stopwordsFile = Path.Combine(Directory.GetCurrentDirectory(), "stopwords.txt");
HashSet<string> stopwords;

try
{
    stopwords = StopwordPreprocessor.LoadStopwords(stopwordsFile);
    Console.WriteLine($"Loaded {stopwords.Count} stopwords");
}
catch (Exception ex)
{
    Console.WriteLine($"Could not load stopwords file: {ex.Message}");
    Console.WriteLine("Using empty stopword list (no filtering will occur)");
    stopwords = new HashSet<string>();
}

// Optional: Add domain-specific keep words
// You can extend this list with company-specific terms, product, names, etc.
var extraKeepWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{  
    // Add your domain-specific terms here
    "api", "database", "backend", "frontend", "oauth", "jwt", "ssl", "https", "address",
    "admin", "user", "client", "server", "deployment", "kubernetes", "docker"
    // TODO: Consider loading these from a config file for easier maintenance
};
Console.WriteLine($"Using {extraKeepWords.Count} additional keep words");

// Text preprocessing options
bool collapseNumbers = false; //Set to true if you want numbers replaced with <NUM>

// Note: collapseNumbers helps generalize numeric patterns but may lose important
// context like specific dates, IDs, or quantities that matter for intent classification

Console.WriteLine($"Text preprocessing: collapseNumbers={collapseNumbers}");

// Configuration
var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output");

// Ensure directories exist
if (!Directory.Exists(dataDirectory))
{
    Console.WriteLine($"Data directory not found {dataDirectory}");
    Console.WriteLine("Please create the Data directory with intent JSON files.");
    return 1;
}

Directory.CreateDirectory(outputDirectory);

// Find all JSON files in Data directory
var jsonFiles = Directory.GetFiles(dataDirectory, "*.json");
if (jsonFiles.Length == 0)
{
    Console.WriteLine($"No JSON files found in: {dataDirectory}");
    return 1;
}

Console.WriteLine($"Found {jsonFiles.Length} JSON files:");
foreach (var file in jsonFiles)
{
    Console.WriteLine($" - {Path.GetFileName(file)}");
}

// Combine all JSON files
var allRecords = new List<QueryRecord>();
var sourceFiles = new List<string>();
var intentCounts = new Dictionary<string, int>();

foreach (var jsonFile in jsonFiles)
{
    try
    {
        Console.WriteLine($"Reading {Path.GetFileName(jsonFile)}...");

        var json = await File.ReadAllTextAsync(jsonFile);
        var records = JsonSerializer.Deserialize<List<QueryRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (records != null && records.Count > 0)
        {
            // First split multi-intent queries into focused fragments
            // var splitRecords = SentenceSplitter.SplitQueries(records);
            // if (splitRecords.Count > records.Count)
            // {
            //     Console.WriteLine($"Split {records.Count} records into {splitRecords.Count} fragments");
            // }

            // Process each record's text through StopwordPreprocessor
            var processRecords = new List<QueryRecord>();
            Console.WriteLine($"Processing {records.Count} text samples...");

            foreach (var record in records)
            {
                if (!string.IsNullOrEmpty(record.Text))
                {
                    // Normalize the text using StopwordPreprocessor
                    var normalizedText = StopwordPreprocessor.Normalize(
                        record.Text,
                        stopwords,
                        extraKeepWords,
                        collapseNumbers
                    );

                    // Create processed record with both original and normalized text
                    var processRecord = new QueryRecord
                    {
                        Text = normalizedText,
                        Label = record.Label,
                        OriginalText = record.Text // Keep original for reference
                    };

                    processRecords.Add(processRecord);

                    // Show first few transformations as examples
                    if (processRecords.Count <= 3 && record.Text != normalizedText)
                    {
                        Console.WriteLine($"  \"{record.Text}\" -> \"{normalizedText}\"");
                    }
                }
                else
                {
                    processRecords.Add(record);
                }
            }

            allRecords.AddRange(processRecords);
            sourceFiles.Add(Path.GetFileName(jsonFile));

            // Count records per intent
            foreach (var record in processRecords)
            {
                if (!string.IsNullOrEmpty(record.Label))
                {
                    intentCounts[record.Label] = intentCounts.GetValueOrDefault(record.Label, 0) + 1;
                }

            }

            Console.WriteLine($"Add {processRecords.Count} records (text normalized)");
        }
        else
        {
            Console.WriteLine($"No records found {Path.GetFileName(jsonFile)}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading {Path.GetFileName(jsonFile)}: {ex.Message}");
        return 1;
    }
}

if (allRecords.Count == 0)
{
    Console.WriteLine("No records were loaded from any files.");
    return 1;
}


// Generate versions string (timestamp-based)
var version = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var datasetFileName = $"intent_dataset_v{version}.json";
var metadataFileName = $"metadata_v{version}.json";

// Create metadata
var metadata = new DatasetMetadata
{
    Version = version,
    CreatedAt = DateTime.UtcNow,
    SourceFiles = sourceFiles,
    TotalRecords = allRecords.Count,
    IntentCounts = intentCounts,
};


// Write combined dataset
var datasetPath = Path.Combine(outputDirectory, datasetFileName);
var metadataPath = Path.Combine(outputDirectory, metadataFileName);

try
{
    var datasetJson = JsonSerializer.Serialize(allRecords, new JsonSerializerOptions
    {
        WriteIndented = true,
    });

    await File.WriteAllTextAsync(datasetPath, datasetJson);

    // Write metadata
    var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
    {
        WriteIndented = true,
    });

    await File.WriteAllTextAsync(metadataPath, metadataJson);
    Console.WriteLine();
    Console.WriteLine($"Dataset created: {datasetFileName}");
    Console.WriteLine($"Metadata created: {metadataFileName}");
    Console.WriteLine($"Total records: {allRecords.Count}");
    Console.WriteLine("Intent Distribution:");
    foreach (var (intent, count) in intentCounts.OrderByDescending(kv => kv.Value))
    {
        var percentage = (count * 100.0) / allRecords.Count;
        Console.WriteLine($" - {intent}: {count} ({percentage:F1}%)");
    }
    Console.WriteLine($"Output save to: {outputDirectory}");
    return 0;

}
catch (Exception ex)
{
    Console.WriteLine($"Error writing output files: {ex.Message}");
    return 1;
}

// QueryRecord class used by both SentenceSplitter and Program
public class QueryRecord
{
    public string Text { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty; // Keep original text for reference
}

public class DatasetMetadata
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> SourceFiles { get; set; } = new List<string>();
    public int TotalRecords { get; set; }
    public Dictionary<string, int> IntentCounts { get; set; } = new Dictionary<string, int>();
}





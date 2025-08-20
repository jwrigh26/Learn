using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;

// ModelTrainer: ML.NET Intent Classification Model Builder
// This tool trains and evaluates ML.NET models for intent classification
// Based on the ml_multiclass_model.ipynb notebook

Console.WriteLine("ModelTrainer: ML.NET Intent Classification");
Console.WriteLine("===========================================");

// Configuration
var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output");

// Ensure directories exist
Directory.CreateDirectory(outputDirectory);

// Look for training data
var jsonFiles = Directory.GetFiles(dataDirectory, "intent_dataset_v*.json");
if (jsonFiles.Length == 0)
{
    Console.WriteLine($"No training dataset found in: {dataDirectory}");
    Console.WriteLine("Please copy a dataset file from ModelBuilder/Output/ to ModelTrainer/Data/");
    Console.WriteLine("Expected format: intent_dataset_v*.json");
    return 1;
}

// Use the most recent dataset file
var datasetFile = jsonFiles.OrderByDescending(f => f).First();
Console.WriteLine($"Using dataset: {Path.GetFileName(datasetFile)}");

// Load training data
List<QueryRecord> seed = new();
try
{
    var json = await File.ReadAllTextAsync(datasetFile);
    seed = JsonSerializer.Deserialize<List<QueryRecord>>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? new List<QueryRecord>();

    Console.WriteLine($"Loaded {seed.Count} training records");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading dataset: {ex.Message}");
    return 1;
}

// Initialize ML.NET context
Console.WriteLine($"Building ML pipeline...");
var ml = new MLContext(seed: 42);

// Load data into ML.NET
var data = ml.Data.LoadFromEnumerable(seed);

// Train/test split
var split = ml.Data.TrainTestSplit(data, testFraction: 0.25);

var pipeline = ml.Transforms.Conversion.MapValueToKey(
        inputColumnName: nameof(QueryRecord.Label),
        outputColumnName: "Label")
    .Append(ml.Transforms.Text.FeaturizeText(
        outputColumnName: "Features",
        inputColumnName: nameof(QueryRecord.Text)))
    .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
        labelColumnName: "Label", featureColumnName: "Features"))
    .Append(ml.Transforms.Conversion.MapKeyToValue(
        outputColumnName: nameof(IntentPrediction.PredictedLabel),
        inputColumnName: "PredictedLabel"));

// Train the model
Console.WriteLine("Training model...");
ITransformer model;
try
{
    model = pipeline.Fit(split.TrainSet);
    Console.WriteLine("Model training completed");
}
catch (Exception ex)
{
    Console.WriteLine($"Training failed: {ex.Message}");
    return 1;
}

// Evaluate the model
Console.WriteLine("Evaluating model performance...");
var scored = model.Transform(split.TestSet);
var metrics = ml.MulticlassClassification.Evaluate(
    scored,
    labelColumnName: "Label",
    scoreColumnName: "Score",
    predictedLabelColumnName: "PredictedLabel"
);

// Get class names for detailed metrics
var labelCol = scored.Schema["Label"];
VBuffer<ReadOnlyMemory<char>> keyValues = default;
labelCol.GetKeyValues(ref keyValues);
var ClassNames = keyValues.DenseValues().Select(v => v.ToString()).ToArray();

// Display overall metrics
Console.WriteLine("\n Overall Model Performance");
Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:F3} ({metrics.MicroAccuracy:P1})");
Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:F3} ({metrics.MacroAccuracy:P1})");
Console.WriteLine($"LogLoss: {metrics.LogLoss:F3}");

// Display per-class log loss
Console.WriteLine("\n Per-Class LogLoss:");
for (int i = 0; i < metrics.PerClassLogLoss.Count; i++)
{
    var name = i < ClassNames.Length ? ClassNames[i] : $"class_{i}";
    Console.WriteLine($"    {i,2}: {name,-24} -> {metrics.PerClassLogLoss[i]:F3}");
}

// Display confusion matrix
Console.WriteLine("\n Confusion Matrix (rows=true, cols=predicted):");
var cm = metrics.ConfusionMatrix;

string Pad(string s, int w) => s.Length > w ? s.Substring(0, w) : s.PadRight(w);

int w = 24;
Console.Write("     " + Pad("true\\pred", w));
for (int j = 0; j < ClassNames.Length; j++)
    Console.Write(Pad(ClassNames[j], w));
Console.WriteLine();

for (int i = 0; i < cm.Counts.Count; i++)
{
    Console.Write("     " + Pad(ClassNames[i], w));
    for (int j = 0; j < cm.Counts[i].Count; j++)
        Console.Write(Pad(cm.Counts[i][j].ToString(), w));
    Console.WriteLine();
}

// Highlight worst performing class
int worstIdx = Enumerable.Range(0, metrics.PerClassLogLoss.Count)
                         .OrderByDescending(k => metrics.PerClassLogLoss[k])
                         .First();
Console.WriteLine($"Worst performing class: [{worstIdx}] {ClassNames[worstIdx]} (LogLoss: {metrics.PerClassLogLoss[worstIdx]:F3})");

// Save the model
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var modelFileName = $"intent_model_v{timestamp}.zip";
var modelPath = Path.Combine(outputDirectory, modelFileName);

try
{
    using (var fs = File.Create(modelPath))
    {
        ml.Model.Save(model, split.TrainSet.Schema, fs);
    }
    Console.WriteLine($"\nModel saved: {modelFileName}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error saving model: {ex.Message}");
    return 1;
}

// Test the saved model
Console.WriteLine("\nTesting saved model");
try
{
    ITransformer reloadedModel;
    using (var fs = File.OpenRead(modelPath))
    {
        reloadedModel = ml.Model.Load(fs, out var schema);
    }

    var engine = ml.Model.CreatePredictionEngine<QueryRecord, IntentPrediction>(reloadedModel);

    // Test with a sample query
    var testQuery = "list engineers";
    var prediction = engine.Predict(new QueryRecord { Text = testQuery });
    Console.WriteLine($"    Test query: '{testQuery}'");
    Console.WriteLine($"    Predicted intent: {prediction.PredictedLabel}");
    Console.WriteLine("     Model reload test successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"Model reload test failed {ex.Message}");
    return 1;
}


// Save detailed metrics to JSON
var trainRowCount = 0;
var testRowCount = 0;

// Try to get row counts safely
try
{
    trainRowCount = (int)(split.TrainSet.GetRowCount() ?? 0);
    testRowCount = (int)(split.TestSet.GetRowCount() ?? 0);
}
catch
{
    // If we can't get exact counts, estimate from total
    trainRowCount = (int)(seed.Count * 0.75);
    testRowCount = seed.Count - trainRowCount;

}

var metricsData = new ModelMetrics
{
    Version = timestamp,
    CreatedAt = DateTime.UtcNow,
    DatasetFile = Path.GetFileName(datasetFile),
    TotalRecords = seed.Count,
    TrainRecords = trainRowCount,
    TestRecords = testRowCount,
    MicroAccuracy = metrics.MicroAccuracy,
    MacroAccuracy = metrics.MacroAccuracy,
    LogLoss = metrics.LogLoss,
    PerClassLogLoss = metrics.PerClassLogLoss.Select((loss, i) => new ClassMetric
    {
        ClassName = i < ClassNames.Length ? ClassNames[i] : $"class_{i}",
        LogLoss = loss
    }).ToList(),
    ConfusionMatrix = cm.Counts.Select((row, i) => new ConfusionRow
    {
        TrueClass = i < ClassNames.Length ? ClassNames[i] : $"class_{i}",
        Predictions = row.Select((count, j) => new PredictionCount
        {
            PredictedClass = j < ClassNames.Length ? ClassNames[j] : $"class_{j}",
            Count = (int)count
        }).ToList()
    }).ToList()
};

var metricsFileName = $"model_metrics_v{timestamp}.json";
var metricsPath = Path.Combine(outputDirectory, metricsFileName);

try
{
    var metricsJson = JsonSerializer.Serialize(metricsData, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    await File.WriteAllTextAsync(metricsPath, metricsJson);
    Console.WriteLine($"Metrics saved: {metricsFileName}");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not save metrics file: {ex.Message}");
}

Console.WriteLine("\nTraining completed successfully!");
Console.WriteLine($"Output directory: {outputDirectory}");
Console.WriteLine($"Model file: {modelFileName}");
Console.WriteLine($"Metrics file: {metricsFileName}");
// Program end (update with training pipeline later)
return 0;

public class QueryRecord
{
    public string Text { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty; // Keep original text for reference
}

public class IntentPrediction
{
    public string PredictedLabel { get; set; } = string.Empty;
    public float[] Score { get; set; } = Array.Empty<float>();
}

public class ModelMetrics
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DatasetFile { get; set; } = string.Empty;
    public int TotalRecords { get; set; } = 0;
    public int TrainRecords { get; set; } = 0;
    public int TestRecords { get; set; } = 0;
    public double MicroAccuracy { get; set; } = 0;
    public double MacroAccuracy { get; set; } = 0;
    public double LogLoss { get; set; } = 0;
    public List<ClassMetric> PerClassLogLoss { get; set; } = new();
    public List<ConfusionRow> ConfusionMatrix { get; set; } = new();
}

public class ClassMetric
{
    public string ClassName { get; set; } = string.Empty;
    public double LogLoss { get; set; } = 0;
}

public class ConfusionRow
{
    public string TrueClass { get; set; } = string.Empty;
    public List<PredictionCount> Predictions { get; set; } = new();
}

public class PredictionCount
{
    public string PredictedClass { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
}
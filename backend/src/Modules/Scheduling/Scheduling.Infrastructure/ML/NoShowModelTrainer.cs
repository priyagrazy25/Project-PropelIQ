using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Scheduling.Infrastructure.ML;

/// <summary>
/// ML.NET training pipeline for no-show risk prediction (AIR-007).
/// Uses FastTree binary classification for optimal performance.
/// </summary>
public sealed class NoShowModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly ILogger<NoShowModelTrainer> _logger;

    public NoShowModelTrainer(ILogger<NoShowModelTrainer> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
    }

    /// <summary>
    /// Trains a new model on the provided training data.
    /// </summary>
    /// <param name="trainingData">Training samples.</param>
    /// <returns>Trained model and metrics.</returns>
    public (ITransformer Model, ModelMetrics Metrics) TrainModel(
        IEnumerable<NoShowModelInput> trainingData)
    {
        _logger.LogInformation("Starting model training with {Count} samples", trainingData.Count());

        // Load data
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Split for evaluation
        var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Build pipeline
        var pipeline = BuildPipeline();

        // Train model
        var startTime = DateTime.UtcNow;
        var model = pipeline.Fit(splitData.TrainSet);
        var trainingTime = DateTime.UtcNow - startTime;

        _logger.LogInformation("Model training completed in {Duration:N2}s", trainingTime.TotalSeconds);

        // Evaluate
        var predictions = model.Transform(splitData.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions);

        var modelMetrics = new ModelMetrics(
            Accuracy: metrics.Accuracy,
            AreaUnderCurve: metrics.AreaUnderRocCurve,
            F1Score: metrics.F1Score,
            Precision: metrics.PositivePrecision,
            Recall: metrics.PositiveRecall,
            TrainingSamples: trainingData.Count(),
            TrainingDuration: trainingTime);

        _logger.LogInformation(
            "Model metrics - Accuracy: {Accuracy:P2}, AUC: {AUC:P2}, F1: {F1:P2}",
            modelMetrics.Accuracy,
            modelMetrics.AreaUnderCurve,
            modelMetrics.F1Score);

        return (model, modelMetrics);
    }

    /// <summary>
    /// Trains a model using synthetic data for initial deployment (AC-4).
    /// </summary>
    /// <param name="sampleCount">Number of synthetic samples.</param>
    /// <returns>Trained model and metrics.</returns>
    public (ITransformer Model, ModelMetrics Metrics) TrainOnSyntheticData(int sampleCount = 5000)
    {
        _logger.LogInformation("Generating {Count} synthetic training samples", sampleCount);
        var syntheticData = SyntheticDataGenerator.GenerateTrainingData(sampleCount);
        return TrainModel(syntheticData);
    }

    /// <summary>
    /// Saves a trained model to disk.
    /// </summary>
    /// <param name="model">Trained model.</param>
    /// <param name="filePath">Output file path.</param>
    /// <param name="schema">Data schema.</param>
    public void SaveModel(ITransformer model, string filePath, DataViewSchema schema)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _mlContext.Model.Save(model, schema, filePath);
        _logger.LogInformation("Model saved to {Path}", filePath);
    }

    /// <summary>
    /// Loads a model from disk.
    /// </summary>
    /// <param name="filePath">Model file path.</param>
    /// <returns>Loaded model and schema.</returns>
    public (ITransformer Model, DataViewSchema Schema) LoadModel(string filePath)
    {
        var model = _mlContext.Model.Load(filePath, out var schema);
        _logger.LogInformation("Model loaded from {Path}", filePath);
        return (model, schema);
    }

    /// <summary>
    /// Gets the input schema for the model.
    /// </summary>
    public DataViewSchema GetInputSchema()
    {
        var emptyData = _mlContext.Data.LoadFromEnumerable(Array.Empty<NoShowModelInput>());
        return emptyData.Schema;
    }

    private IEstimator<ITransformer> BuildPipeline()
    {
        // Feature concatenation
        var featureCols = new[]
        {
            nameof(NoShowModelInput.AppointmentType),
            nameof(NoShowModelInput.DayOfWeek),
            nameof(NoShowModelInput.HourOfDay),
            nameof(NoShowModelInput.PriorNoShows),
            nameof(NoShowModelInput.PriorCancellations),
            nameof(NoShowModelInput.TotalPriorAppointments),
            nameof(NoShowModelInput.DaysSinceLastVisit),
            nameof(NoShowModelInput.HasInsurance),
            nameof(NoShowModelInput.IsNewPatient),
            nameof(NoShowModelInput.LeadTimeDays)
        };

        return _mlContext.Transforms.Concatenate("Features", featureCols)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));
    }
}

/// <summary>
/// Model training metrics.
/// </summary>
public record ModelMetrics(
    double Accuracy,
    double AreaUnderCurve,
    double F1Score,
    double Precision,
    double Recall,
    int TrainingSamples,
    TimeSpan TrainingDuration);

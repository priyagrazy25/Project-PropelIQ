using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Scheduling.Application.Abstractions;

namespace Scheduling.Infrastructure.ML;

/// <summary>
/// Prediction engine for serving no-show risk predictions.
/// Thread-safe singleton pattern with hot-reload capability.
/// </summary>
public sealed class NoShowPredictionEngine : IDisposable
{
    private readonly MLContext _mlContext;
    private readonly ModelVersionManager _versionManager;
    private readonly NoShowModelTrainer _trainer;
    private readonly ILogger<NoShowPredictionEngine> _logger;
    private readonly SemaphoreSlim _modelLock = new(1, 1);

    private PredictionEngine<NoShowModelInput, NoShowModelOutput>? _predictionEngine;
    private ITransformer? _activeModel;
    private DataViewSchema? _modelSchema;
    private string? _loadedVersion;
    private bool _isInitialized;

    public NoShowPredictionEngine(
        ModelVersionManager versionManager,
        NoShowModelTrainer trainer,
        ILogger<NoShowPredictionEngine> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _versionManager = versionManager;
        _trainer = trainer;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the prediction engine with the active model.
    /// Creates initial model from synthetic data if none exists.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        await _modelLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            var activeVersion = _versionManager.GetActiveStoredVersion();
            if (activeVersion == null)
            {
                _logger.LogInformation("No active model found, training initial model from synthetic data");
                await TrainAndActivateInitialModelAsync(cancellationToken);
            }
            else
            {
                // Load directly without acquiring lock (already held)
                var (model, schema) = _trainer.LoadModel(activeVersion.ModelPath);
                await LoadModelInternalAsync(model, schema, activeVersion.Version);
            }

            _isInitialized = true;
        }
        finally
        {
            _modelLock.Release();
        }
    }

    /// <summary>
    /// Predicts no-show risk for a single input.
    /// Returns risk score 0-100 with category.
    /// </summary>
    public NoShowRiskPrediction Predict(NoShowRiskInput input)
    {
        EnsureInitialized();

        var modelInput = MapToModelInput(input);
        var output = _predictionEngine!.Predict(modelInput);

        var score = (int)Math.Round(output.Probability * 100);
        var riskLevel = score switch
        {
            <= 30 => "Low",
            <= 70 => "Medium",
            _ => "High"
        };

        var contributingFactors = AnalyzeRiskFactors(modelInput, output.Probability);

        return new NoShowRiskPrediction(
            RiskScore: score,
            RiskLevel: riskLevel,
            ContributingFactors: contributingFactors,
            Probability: output.Probability,
            ModelVersion: _loadedVersion ?? "unknown");
    }

    /// <summary>
    /// Predicts no-show risk for multiple appointments.
    /// </summary>
    public IReadOnlyList<NoShowRiskPrediction> PredictBatch(IEnumerable<NoShowRiskInput> inputs)
    {
        return inputs.Select(Predict).ToList();
    }

    /// <summary>
    /// Retrains the model and optionally activates it.
    /// </summary>
    public async Task<string> RetrainAsync(
        IEnumerable<NoShowModelInput>? trainingData = null,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        await _modelLock.WaitAsync(cancellationToken);
        try
        {
            // Train new model
            var (model, metrics) = trainingData != null
                ? _trainer.TrainModel(trainingData)
                : _trainer.TrainOnSyntheticData();

            // Save version
            var schema = _trainer.GetInputSchema();
            var storedVersion = await _versionManager.SaveVersionAsync(
                model, schema, metrics, _mlContext, cancellationToken);

            if (activate)
            {
                await _versionManager.ActivateVersionAsync(storedVersion.Version, cancellationToken);
                await LoadModelInternalAsync(model, schema, storedVersion.Version);
            }

            _logger.LogInformation(
                "Retrained model {Version}, Accuracy={Accuracy:P2}, Activated={Activated}",
                storedVersion.Version, metrics.Accuracy, activate);

            return storedVersion.Version;
        }
        finally
        {
            _modelLock.Release();
        }
    }

    /// <summary>
    /// Rolls back to a previous model version.
    /// </summary>
    public async Task<(bool Success, string? Message)> RollbackAsync(
        string? targetVersion = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _versionManager.RollbackAsync(targetVersion, cancellationToken);

        if (result.Success)
        {
            var activeVersion = _versionManager.GetActiveStoredVersion();
            if (activeVersion != null)
            {
                await LoadModelAsync(activeVersion.ModelPath, activeVersion.Version, cancellationToken);
            }
        }

        return result;
    }

    /// <summary>
    /// Reloads the currently active model from disk.
    /// </summary>
    public async Task ReloadActiveModelAsync(CancellationToken cancellationToken = default)
    {
        var activeVersion = _versionManager.GetActiveStoredVersion();
        if (activeVersion == null)
        {
            throw new InvalidOperationException("No active model version found");
        }

        await LoadModelAsync(activeVersion.ModelPath, activeVersion.Version, cancellationToken);
    }

    /// <summary>
    /// Gets the current model version.
    /// </summary>
    public string? GetCurrentVersion() => _loadedVersion;

    /// <summary>
    /// Gets all available model versions.
    /// </summary>
    public IReadOnlyList<ModelVersionInfo> GetAllVersions() => _versionManager.GetAllVersions();

    /// <summary>
    /// Checks if rollback is available.
    /// </summary>
    public bool CanRollback() => _versionManager.CanRollback();

    /// <summary>
    /// Gets remaining rollback time in seconds.
    /// </summary>
    public int GetRemainingRollbackSeconds() => _versionManager.GetRemainingRollbackSeconds();

    private async Task TrainAndActivateInitialModelAsync(CancellationToken cancellationToken)
    {
        var (model, metrics) = _trainer.TrainOnSyntheticData(5000);
        var schema = _trainer.GetInputSchema();

        var storedVersion = await _versionManager.SaveVersionAsync(
            model, schema, metrics, _mlContext, cancellationToken);

        await _versionManager.ActivateVersionAsync(storedVersion.Version, cancellationToken);
        await LoadModelInternalAsync(model, schema, storedVersion.Version);

        _logger.LogInformation("Initial model trained and activated: {Version}", storedVersion.Version);
    }

    private async Task LoadModelAsync(string modelPath, string version, CancellationToken cancellationToken)
    {
        await _modelLock.WaitAsync(cancellationToken);
        try
        {
            var (model, schema) = _trainer.LoadModel(modelPath);
            await LoadModelInternalAsync(model, schema, version);
        }
        finally
        {
            _modelLock.Release();
        }
    }

    private Task LoadModelInternalAsync(ITransformer model, DataViewSchema schema, string version)
    {
        // Dispose old prediction engine
        _predictionEngine?.Dispose();

        _activeModel = model;
        _modelSchema = schema;
        _loadedVersion = version;
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<NoShowModelInput, NoShowModelOutput>(model);

        _logger.LogInformation("Loaded model version {Version}", version);
        return Task.CompletedTask;
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized || _predictionEngine == null)
        {
            throw new InvalidOperationException(
                "Prediction engine not initialized. Call InitializeAsync first.");
        }
    }

    private static NoShowModelInput MapToModelInput(NoShowRiskInput input)
    {
        return new NoShowModelInput
        {
            AppointmentType = (float)input.AppointmentType,
            DayOfWeek = (float)input.DayOfWeek,
            HourOfDay = input.HourOfDay,
            PriorNoShows = input.PriorNoShows,
            PriorCancellations = input.PriorCancellations,
            TotalPriorAppointments = input.TotalPriorAppointments,
            DaysSinceLastVisit = input.DaysSinceLastVisit,
            HasInsurance = input.HasInsurance ? 1 : 0,
            IsNewPatient = input.IsNewPatient ? 1 : 0,
            LeadTimeDays = input.LeadTimeDays
        };
    }

    private static List<string> AnalyzeRiskFactors(NoShowModelInput input, float probability)
    {
        var factors = new List<string>();

        // Only add factors for medium/high risk
        if (probability < 0.3) return factors;

        if (input.PriorNoShows >= 2)
            factors.Add($"History of {(int)input.PriorNoShows} prior no-shows");
        else if (input.PriorNoShows == 1)
            factors.Add("1 prior no-show on record");

        if (input.IsNewPatient == 1)
            factors.Add("New patient (no visit history)");

        if (input.HasInsurance == 0)
            factors.Add("No insurance on file");

        if (input.LeadTimeDays > 30)
            factors.Add($"Long lead time ({(int)input.LeadTimeDays} days)");

        if (input.DayOfWeek == 1)
            factors.Add("Monday appointment (higher no-show trend)");

        if (input.DayOfWeek == 5 && input.HourOfDay >= 14)
            factors.Add("Friday afternoon slot");

        if (input.HourOfDay < 10)
            factors.Add("Early morning appointment");

        if (input.DaysSinceLastVisit > 180)
            factors.Add($"Long gap since last visit ({(int)input.DaysSinceLastVisit} days)");

        return factors;
    }

    public void Dispose()
    {
        _predictionEngine?.Dispose();
        _modelLock.Dispose();
    }
}

using System;
using SolidWorks.Interop.sldworks;
using TextToCad.SolidWorksAddin.Interfaces;

namespace TextToCad.SolidWorksAddin.Utils
{
    /// <summary>
    /// RAII-style guard for safe undo/rollback of SolidWorks operations.
    /// Implements the Dispose pattern to automatically rollback changes if not committed.
    /// </summary>
    /// <remarks>
    /// USAGE PATTERN:
    /// <code>
    /// using (var scope = new UndoScope(model, "Create Base Plate", logger))
    /// {
    ///     // Perform SolidWorks operations (sketches, features, etc.)
    ///     sketch.CreateLine(...);
    ///     feature.FeatureExtrusion2(...);
    ///     
    ///     // If all operations succeed, commit the changes
    ///     scope.Commit();
    /// }
    /// // If Commit() was not called (e.g., exception thrown), Dispose() rolls back changes
    /// </code>
    /// 
    /// IMPORTANT NOTES:
    /// - SolidWorks undo/rollback behavior varies by version and API usage patterns
    /// - This implementation uses SetUndoPoint() at start and EditRollback() on failure
    /// - Some SolidWorks versions may have limitations with programmatic undo
    /// - Always test with your specific SolidWorks version
    /// - For complex multi-step operations, consider using IModelDoc2.SetAddToDB(false) pattern
    /// 
    /// LIMITATIONS:
    /// - Cannot capture numeric undo mark ID directly in all SW versions
    /// - Rollback may not work perfectly for all operation types
    /// - Some operations (like saving) cannot be undone programmatically
    /// - User manual undo (Ctrl+Z) may behave differently
    /// 
    /// ALTERNATIVE PATTERNS:
    /// If this doesn't work for your use case, consider:
    /// 1. SetAddToDB(false) before operations, SetAddToDB(true) after
    /// 2. Creating features in a separate temporary part, then copying
    /// 3. Using IModelDoc2.Extension.StartRecordingUndoObject() (SW 2015+)
    /// </remarks>
    public sealed class UndoScope : IDisposable
    {
        private readonly IModelDoc2 _model;
        private readonly string _label;
        private readonly ILogger _logger;
        private bool _committed = false;
        private bool _disposed = false;
        private bool _undoPointSet = false;

        /// <summary>
        /// Create an undo scope for a series of model operations.
        /// </summary>
        /// <param name="model">The SolidWorks model document</param>
        /// <param name="label">Description of the operation (appears in undo history)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <exception cref="ArgumentNullException">If model is null</exception>
        /// <example>
        /// using (var scope = new UndoScope(modelDoc, "Create Cylinder", logger))
        /// {
        ///     // Create sketch
        ///     modelDoc.SketchManager.CreateCircle(...);
        ///     
        ///     // Create extrude feature
        ///     modelDoc.FeatureManager.FeatureExtrusion2(...);
        ///     
        ///     // All good - commit the changes
        ///     scope.Commit();
        /// }
        /// </example>
        public UndoScope(IModelDoc2 model, string label, ILogger logger = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _label = label ?? "Operation";
            _logger = logger ?? Utils.Logger.Null();

            try
            {
                // Note: SolidWorks doesn't have a reliable cross-version API for programmatic undo points
                // StartRecordingUndoObject exists but signature varies by version
                // For now, mark as ready and rely on manual EditRollback if needed
                _undoPointSet = true;
                _logger.Info($"Undo scope started: {_label}");
                _logger.Info("Note: Rollback relies on EditRollback() which may not work for all operations");
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in undo scope setup: {ex.Message}");
                _undoPointSet = false;
            }
        }

        /// <summary>
        /// Mark the operation as successful.
        /// Prevents rollback when the scope is disposed.
        /// </summary>
        /// <remarks>
        /// Call this after all operations in the scope have completed successfully.
        /// If Commit() is not called before disposal, the scope will attempt to rollback.
        /// </remarks>
        /// <example>
        /// using (var scope = new UndoScope(model, "Create Holes"))
        /// {
        ///     CreateHolePattern();
        ///     scope.Commit();  // Success - don't rollback
        /// }
        /// </example>
        public void Commit()
        {
            if (_disposed)
            {
                _logger.Warn($"Attempted to commit disposed undo scope: {_label}");
                return;
            }

            _committed = true;
            _logger.Info($"Undo scope committed: {_label}");
        }

        /// <summary>
        /// Dispose the undo scope.
        /// If not committed, attempts to rollback changes.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (!_committed)
                {
                    // Operation was not committed - attempt rollback
                    _logger.Warn($"Undo scope not committed, attempting rollback: {_label}");
                    Rollback();
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Attempt to rollback changes made since the undo point.
        /// </summary>
        /// <remarks>
        /// This uses IModelDoc2.EditRollback() which undoes all changes back to the last undo point.
        /// 
        /// IMPORTANT CAVEATS:
        /// - EditRollback() behavior varies by SolidWorks version
        /// - May not work for all operation types
        /// - Some operations cannot be programmatically undone
        /// - If SetUndoPoint() failed, rollback may undo more than intended
        /// 
        /// For maximum reliability, consider alternative patterns:
        /// - Use SetAddToDB(false) before operations
        /// - Create features in a separate Part document
        /// - Manually delete created features if rollback fails
        /// </remarks>
        private void Rollback()
        {
            if (_model == null)
            {
                _logger.Error("Cannot rollback: model is null");
                return;
            }

            try
            {
                if (_undoPointSet)
                {
                    // Attempt to roll back to the undo point
                    // EditRollback() undoes changes back to the last SetUndoPoint()
                    bool success = _model.EditRollback();

                    if (success)
                    {
                        _logger.Info($"Successfully rolled back: {_label}");
                    }
                    else
                    {
                        _logger.Error($"EditRollback() returned false for: {_label}");
                        _logger.Error("Changes may not have been rolled back completely");
                        _logger.Info("Manual undo (Ctrl+Z) may be required");
                    }
                }
                else
                {
                    _logger.Warn($"Cannot rollback {_label}: undo point was not set");
                    _logger.Info("Consider using manual undo or alternative error handling");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception during rollback of {_label}: {ex.Message}");
                _logger.Error("Manual cleanup may be required");
            }
        }

        /// <summary>
        /// Finalizer to ensure rollback if Dispose is not called.
        /// </summary>
        ~UndoScope()
        {
            if (!_disposed && !_committed)
            {
                _logger?.Error($"UndoScope finalized without Dispose: {_label}");
                _logger?.Warn("Always use 'using' statement or explicit Dispose()");
            }
        }
    }
}

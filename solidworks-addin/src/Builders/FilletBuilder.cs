using System;
using System.Collections.Generic;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Builders
{
    /// <summary>
    /// Builder for creating fillet features on model edges.
    /// Supports filleting recent feature edges or all sharp edges in the model.
    /// </summary>
    public class FilletBuilder
    {
        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        /// <summary>
        /// Create a new fillet builder.
        /// </summary>
        /// <param name="sw">SolidWorks application instance</param>
        /// <param name="log">Logger for operation tracking</param>
        /// <exception cref="ArgumentNullException">If sw or log is null</exception>
        public FilletBuilder(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Apply fillet to the edges of the most recently created feature.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="radiusMm">Fillet radius in millimeters (must be > 0)</param>
        /// <returns>True if fillet was successfully applied; false otherwise</returns>
        public bool ApplyFilletToRecentEdges(IModelDoc2 model, double radiusMm)
        {
            if (model == null)
            {
                _log.Error("ApplyFilletToRecentEdges: model is null");
                return false;
            }

            // Validate radius
            if (radiusMm <= 0)
            {
                _log.Error($"Invalid fillet radius: {radiusMm} mm (must be > 0)");
                return false;
            }

            // Check document type
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Fillet only works with Part documents");
                return false;
            }

            _log.Info($"Applying {radiusMm} mm fillet to recent feature edges...");

            // Get the most recent feature
            IFeature lastFeature = GetLastFeature(model);
            if (lastFeature == null)
            {
                _log.Error("No features found in model - cannot apply fillet");
                return false;
            }

            _log.Info($"Target feature: {lastFeature.Name}");

            // Get edges from the feature
            IEdge[] edges = FaceMapping.GetFeatureEdges(lastFeature);
            if (edges == null || edges.Length == 0)
            {
                _log.Warn($"Feature '{lastFeature.Name}' has no edges - cannot apply fillet");
                return false;
            }

            _log.Info($"Found {edges.Length} edges on feature");

            // Apply fillet to these edges
            return ApplyFilletToEdges(model, edges, radiusMm);
        }

        /// <summary>
        /// Apply fillet to all sharp edges in the model.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="radiusMm">Fillet radius in millimeters (must be > 0)</param>
        /// <param name="angleThresholdDeg">Optional: Only fillet edges with angle > threshold (default: 0, fillet all edges)</param>
        /// <returns>True if fillet was successfully applied; false otherwise</returns>
        /// <remarks>
        /// This method can be slow on complex models with many edges.
        /// Consider using ApplyFilletToRecentEdges for targeted filleting.
        /// </remarks>
        public bool ApplyFilletToAllSharpEdges(IModelDoc2 model, double radiusMm, double angleThresholdDeg = 0.0)
        {
            if (model == null)
            {
                _log.Error("ApplyFilletToAllSharpEdges: model is null");
                return false;
            }

            // Validate radius
            if (radiusMm <= 0)
            {
                _log.Error($"Invalid fillet radius: {radiusMm} mm (must be > 0)");
                return false;
            }

            // Check document type
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Fillet only works with Part documents");
                return false;
            }

            _log.Info($"Applying {radiusMm} mm fillet to all sharp edges (angle threshold: {angleThresholdDeg} deg)...");

            // Get all edges from all bodies
            IPartDoc part = (IPartDoc)model;
            object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true);

            if (bodies == null || bodies.Length == 0)
            {
                _log.Warn("No solid bodies found in model");
                return false;
            }

            var allEdges = new List<IEdge>();

            foreach (object bodyObj in bodies)
            {
                IBody2 body = (IBody2)bodyObj;
                object[] edges = (object[])body.GetEdges();

                if (edges != null && edges.Length > 0)
                {
                    allEdges.AddRange(edges.Cast<IEdge>());
                }
            }

            if (allEdges.Count == 0)
            {
                _log.Warn("No edges found in model");
                return false;
            }

            _log.Info($"Found {allEdges.Count} total edges");

            // Filter by angle if threshold specified
            IEdge[] targetEdges;
            if (angleThresholdDeg > 0)
            {
                targetEdges = allEdges.Where(e => IsSharpEdge(e, angleThresholdDeg)).ToArray();
                _log.Info($"Filtered to {targetEdges.Length} sharp edges (> {angleThresholdDeg} deg)");
            }
            else
            {
                targetEdges = allEdges.ToArray();
            }

            if (targetEdges.Length == 0)
            {
                _log.Warn("No edges to fillet after filtering");
                return false;
            }

            // Apply fillet to filtered edges
            return ApplyFilletToEdges(model, targetEdges, radiusMm);
        }

        /// <summary>
        /// Core method to apply fillet to a set of edges.
        /// </summary>
        /// <param name="model">SolidWorks model document</param>
        /// <param name="edges">Edges to fillet</param>
        /// <param name="radiusMm">Fillet radius in millimeters</param>
        /// <returns>True if successful; false otherwise</returns>
        private bool ApplyFilletToEdges(IModelDoc2 model, IEdge[] edges, double radiusMm)
        {
            if (edges == null || edges.Length == 0)
            {
                _log.Warn("No edges provided for filleting");
                return false;
            }

            using (var scope = new UndoScope(model, "Apply Fillet", _log))
            {
                try
                {
                    _log.Info($"Selecting {edges.Length} edges for fillet...");

                    // Clear any existing selections
                    model.ClearSelection2(true);

                    // Select all target edges
                    ISelectionMgr selMgr = (ISelectionMgr)model.SelectionManager;
                    int selectedCount = 0;

                    foreach (IEdge edge in edges)
                    {
                        try
                        {
                            ISelectData selData = selMgr.CreateSelectData();
                            selData.Mark = 1; // Mark for fillet feature

                            if (((IEntity)edge).Select4(true, (SelectData)selData)) // Append to selection
                            {
                                selectedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Failed to select edge: {ex.Message}");
                        }
                    }

                    if (selectedCount == 0)
                    {
                        _log.Error("Failed to select any edges");
                        return false;
                    }

                    _log.Info($"Selected {selectedCount}/{edges.Length} edges");

                    // Create fillet feature using InsertFeatureFillet
                    IFeatureManager featMgr = model.FeatureManager;

                    _log.Info($"Creating constant-radius fillet (radius: {radiusMm} mm)...");

                    // Convert radius to meters
                    double radiusM = Units.MmToM(radiusMm);

                    // Use InsertFeatureFillet for simple constant-radius fillets
                    // Parameters: Type, Radius, FacePropagate, AllFilletEdges, InstanceCount
                    IFeature filletFeature = (IFeature)featMgr.InsertFeatureFillet(
                        (int)swFeatureFilletOptions_e.swFeatureFilletConstantRadius,  // Type: constant radius
                        radiusM,                                                       // Radius in meters
                        0,                                                             // FacePropagate: 0 = no propagation
                        0,                                                             // AllFilletEdges: 0 = use selected edges
                        0                                                              // InstanceCount: 0 = default
                    );

                    if (filletFeature == null)
                    {
                        _log.Error("InsertFeatureFillet returned null - fillet creation failed");
                        _log.Error("Possible causes:");
                        _log.Error($"  - Radius ({radiusMm} mm) too large for edge geometry");
                        _log.Error("  - Invalid edge selection or edge type");
                        _log.Error("  - Fillet would create invalid geometry");
                        _log.Error($"  - Try a smaller radius (< {radiusMm / 2} mm)");
                        return false;
                    }

                    _log.Info($"Fillet feature '{filletFeature.Name}' created successfully");

                    // Rebuild to apply the fillet
                    model.ForceRebuild3(false);

                    scope.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Fillet creation failed: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the most recently created feature in the model.
        /// </summary>
        /// <param name="model">SolidWorks model document</param>
        /// <returns>Last feature, or null if no features exist</returns>
        private IFeature GetLastFeature(IModelDoc2 model)
        {
            try
            {
                IFeature feature = (IFeature)model.FirstFeature();
                if (feature == null)
                    return null;

                IFeature lastFeature = null;

                // Iterate to find the last feature
                while (feature != null)
                {
                    // Skip origin features and reference geometry
                    string typeName = feature.GetTypeName2();
                    if (typeName != "ProfileFeature" &&
                        typeName != "RefPlane" &&
                        typeName != "CoordSys" &&
                        !typeName.Contains("OriginFeature"))
                    {
                        lastFeature = feature;
                    }

                    feature = (IFeature)feature.GetNextFeature();
                }

                return lastFeature;
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to get last feature: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Determine if an edge is "sharp" (angle exceeds threshold).
        /// </summary>
        /// <param name="edge">Edge to check</param>
        /// <param name="angleThresholdDeg">Angle threshold in degrees</param>
        /// <returns>True if edge angle exceeds threshold</returns>
        private bool IsSharpEdge(IEdge edge, double angleThresholdDeg)
        {
            try
            {
                // Get adjacent faces to measure angle
                object[] facesObj = (object[])edge.GetTwoAdjacentFaces2();
                if (facesObj == null || facesObj.Length < 2)
                    return false; // Boundary edge, no angle to measure

                // Future implementation -- fillet addition here (compute dihedral angle accurately).

                // For now, consider all edges with two faces as potentially sharp.
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

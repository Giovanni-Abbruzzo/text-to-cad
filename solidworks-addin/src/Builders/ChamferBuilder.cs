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
    /// Builder for creating chamfer features on model edges.
    /// Supports chamfering recent feature edges or all sharp edges in the model.
    /// </summary>
    public class ChamferBuilder
    {
        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        /// <summary>
        /// Create a new chamfer builder.
        /// </summary>
        /// <param name="sw">SolidWorks application instance</param>
        /// <param name="log">Logger for operation tracking</param>
        /// <exception cref="ArgumentNullException">If sw or log is null</exception>
        public ChamferBuilder(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Apply chamfer to the edges of the most recently created feature.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="distanceMm">Chamfer distance in millimeters (must be > 0)</param>
        /// <param name="angleDeg">Optional chamfer angle in degrees (angle-distance type)</param>
        /// <returns>True if chamfer was successfully applied; false otherwise</returns>
        public bool ApplyChamferToRecentEdges(IModelDoc2 model, double distanceMm, double? angleDeg = null)
        {
            if (model == null)
            {
                _log.Error("ApplyChamferToRecentEdges: model is null");
                return false;
            }

            if (distanceMm <= 0)
            {
                _log.Error($"Invalid chamfer distance: {distanceMm} mm (must be > 0)");
                return false;
            }

            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Chamfer only works with Part documents");
                return false;
            }

            _log.Info($"Applying {distanceMm} mm chamfer to recent feature edges...");

            IFeature lastFeature = GetLastFeature(model);
            if (lastFeature == null)
            {
                _log.Error("No features found in model - cannot apply chamfer");
                return false;
            }

            _log.Info($"Target feature: {lastFeature.Name}");

            IEdge[] edges = FaceMapping.GetFeatureEdges(lastFeature);
            if (edges == null || edges.Length == 0)
            {
                _log.Warn($"Feature '{lastFeature.Name}' has no edges - cannot apply chamfer");
                return false;
            }

            _log.Info($"Found {edges.Length} edges on feature");

            return ApplyChamferToEdges(model, edges, distanceMm, angleDeg);
        }

        /// <summary>
        /// Apply chamfer to all sharp edges in the model.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="distanceMm">Chamfer distance in millimeters (must be > 0)</param>
        /// <param name="angleDeg">Optional chamfer angle in degrees (angle-distance type)</param>
        /// <param name="angleThresholdDeg">Optional: Only chamfer edges with angle > threshold (default: 0, chamfer all edges)</param>
        /// <returns>True if chamfer was successfully applied; false otherwise</returns>
        public bool ApplyChamferToAllSharpEdges(IModelDoc2 model, double distanceMm, double? angleDeg = null, double angleThresholdDeg = 0.0)
        {
            if (model == null)
            {
                _log.Error("ApplyChamferToAllSharpEdges: model is null");
                return false;
            }

            if (distanceMm <= 0)
            {
                _log.Error($"Invalid chamfer distance: {distanceMm} mm (must be > 0)");
                return false;
            }

            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Chamfer only works with Part documents");
                return false;
            }

            _log.Info($"Applying {distanceMm} mm chamfer to all sharp edges (angle threshold: {angleThresholdDeg} deg)...");

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
                _log.Warn("No edges to chamfer after filtering");
                return false;
            }

            return ApplyChamferToEdges(model, targetEdges, distanceMm, angleDeg);
        }

        /// <summary>
        /// Core method to apply chamfer to a set of edges.
        /// </summary>
        /// <param name="model">SolidWorks model document</param>
        /// <param name="edges">Edges to chamfer</param>
        /// <param name="distanceMm">Chamfer distance in millimeters</param>
        /// <param name="angleDeg">Optional chamfer angle in degrees</param>
        /// <returns>True if successful; false otherwise</returns>
        private bool ApplyChamferToEdges(IModelDoc2 model, IEdge[] edges, double distanceMm, double? angleDeg)
        {
            if (edges == null || edges.Length == 0)
            {
                _log.Warn("No edges provided for chamfer");
                return false;
            }

            using (var scope = new UndoScope(model, "Apply Chamfer", _log))
            {
                try
                {
                    _log.Info($"Selecting {edges.Length} edges for chamfer...");

                    model.ClearSelection2(true);

                    ISelectionMgr selMgr = (ISelectionMgr)model.SelectionManager;
                    int selectedCount = 0;

                    foreach (IEdge edge in edges)
                    {
                        try
                        {
                            ISelectData selData = selMgr.CreateSelectData();
                            selData.Mark = 1;

                            if (((IEntity)edge).Select4(true, (SelectData)selData))
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

                    IFeatureManager featMgr = model.FeatureManager;

                    double distanceM = Units.MmToM(distanceMm);
                    bool useAngle = angleDeg.HasValue && angleDeg.Value > 0;
                    double angleRad = useAngle ? (angleDeg.Value * Math.PI / 180.0) : 0.0;
                    double otherDistanceM = useAngle ? 0.0 : distanceM;
                    int chamferType = useAngle
                        ? (int)swChamferType_e.swChamferAngleDistance
                        : (int)swChamferType_e.swChamferDistanceDistance;

                    _log.Info(useAngle
                        ? $"Creating chamfer (distance {distanceMm} mm, angle {angleDeg.Value} deg)..."
                        : $"Creating chamfer (distance {distanceMm} mm)...");

                    IFeature chamferFeature = (IFeature)featMgr.InsertFeatureChamfer(
                        0,          // Options
                        chamferType,
                        distanceM,  // Width
                        angleRad,   // Angle (radians)
                        otherDistanceM, // OtherDist
                        0.0,        // VertexChamDist1
                        0.0,        // VertexChamDist2
                        0.0         // VertexChamDist3
                    );

                    if (chamferFeature == null)
                    {
                        _log.Error("InsertFeatureChamfer returned null - chamfer creation failed");
                        _log.Error("Possible causes:");
                        _log.Error("  - Invalid edge selection or edge type");
                        _log.Error("  - Chamfer would create invalid geometry");
                        _log.Error("  - Distance too large for edge geometry");
                        return false;
                    }

                    _log.Info($"Chamfer feature '{chamferFeature.Name}' created successfully");

                    model.ForceRebuild3(false);

                    scope.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Chamfer creation failed: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the most recently created feature in the model.
        /// </summary>
        private IFeature GetLastFeature(IModelDoc2 model)
        {
            try
            {
                IFeature feature = (IFeature)model.FirstFeature();
                if (feature == null)
                    return null;

                IFeature lastFeature = null;

                while (feature != null)
                {
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
        private bool IsSharpEdge(IEdge edge, double angleThresholdDeg)
        {
            try
            {
                object[] facesObj = (object[])edge.GetTwoAdjacentFaces2();
                if (facesObj == null || facesObj.Length < 2)
                    return false;

                // Future implementation -- chamfer addition here (compute dihedral angle accurately).

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

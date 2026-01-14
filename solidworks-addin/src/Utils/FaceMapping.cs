using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Linq;
using TextToCad.SolidWorksAddin.Interfaces;

namespace TextToCad.SolidWorksAddin.Utils
{
    /// <summary>
    /// Utility for mapping natural language face orientations (top, bottom, left, right, front, back)
    /// to actual model faces using surface normal analysis.
    /// </summary>
    /// <remarks>
    /// This class enables selecting faces based on their orientation in model coordinate system:
    /// - top/bottom: +/-Y axis alignment
    /// - left/right: +/-X axis alignment
    /// - front/back: +/-Z axis alignment
    /// 
    /// Strategy:
    /// 1. Iterate all planar faces on all bodies
    /// 2. Calculate surface normal for each face
    /// 3. Compute dot product with cardinal directions
    /// 4. Return face with strongest alignment above threshold
    /// </remarks>
    public static class FaceMapping
    {
        /// <summary>
        /// Minimum dot product threshold for considering a face aligned with a direction.
        /// Value of 0.7 is approximately a 45 deg tolerance from perfect alignment.
        /// </summary>
        private const double ALIGNMENT_THRESHOLD = 0.7;

        /// <summary>
        /// Cardinal direction vectors in model coordinate system.
        /// Assumes SolidWorks default orientation: X = right, Y = up, Z = front.
        /// </summary>
        private static readonly Dictionary<string, double[]> DirectionVectors = new Dictionary<string, double[]>
        {
            { "top",    new[] {  0.0,  1.0,  0.0 } },  // +Y
            { "bottom", new[] {  0.0, -1.0,  0.0 } },  // -Y
            { "right",  new[] {  1.0,  0.0,  0.0 } },  // +X
            { "left",   new[] { -1.0,  0.0,  0.0 } },  // -X
            { "front",  new[] {  0.0,  0.0,  1.0 } },  // +Z
            { "back",   new[] {  0.0,  0.0, -1.0 } }   // -Z
        };

        /// <summary>
        /// Find a face by its orientation in the model coordinate system.
        /// </summary>
        /// <param name="model">The active model document</param>
        /// <param name="target">Target orientation: "top", "bottom", "left", "right", "front", "back" (case-insensitive)</param>
        /// <param name="logger">Optional logger for diagnostic messages</param>
        /// <returns>Face approximating the requested orientation, or null if none found</returns>
        /// <example>
        /// <code>
        /// var topFace = FaceMapping.FindByOrientation(model, "top", logger);
        /// if (topFace != null)
        /// {
        ///     // Select and use the top face
        ///     SelectionData selData = model.Extension.CreateSelectData();
        ///     topFace.Select4(true, selData);
        /// }
        /// </code>
        /// </example>
        public static IFace2 FindByOrientation(IModelDoc2 model, string target, ILogger logger = null)
        {
            if (model == null)
            {
                logger?.Error("FaceMapping.FindByOrientation: model is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                logger?.Error("FaceMapping.FindByOrientation: target orientation is empty");
                return null;
            }

            // Normalize target to lowercase
            string targetLower = target.Trim().ToLowerInvariant();

            if (!DirectionVectors.ContainsKey(targetLower))
            {
                logger?.Error($"FaceMapping: Unknown orientation '{target}'. Valid: top, bottom, left, right, front, back");
                return null;
            }

            logger?.Info($"Finding '{target}' face...");

            // Get target direction vector
            double[] targetVector = DirectionVectors[targetLower];

            // Ensure we're working with a Part document
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                logger?.Warn("FaceMapping only works with Part documents");
                return null;
            }

            // Get all bodies in the part
            IPartDoc part = (IPartDoc)model;
            object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true);

            if (bodies == null || bodies.Length == 0)
            {
                logger?.Warn("No solid bodies found in model");
                return null;
            }

            logger?.Info($"Searching {bodies.Length} solid bodies for '{target}' face...");

            // Track best match
            IFace2 bestFace = null;
            double bestAlignment = ALIGNMENT_THRESHOLD; // Must exceed threshold

            int totalFaces = 0;
            int planarFaces = 0;

            // Iterate all faces on all bodies
            foreach (object bodyObj in bodies)
            {
                IBody2 body = (IBody2)bodyObj;
                object[] faces = (object[])body.GetFaces();

                if (faces == null) continue;

                totalFaces += faces.Length;

                foreach (object faceObj in faces)
                {
                    IFace2 face = (IFace2)faceObj;

                    // Only consider planar faces
                    ISurface surface = (ISurface)face.GetSurface();
                    if (surface == null || !surface.IsPlane())
                        continue;

                    planarFaces++;

                    // Get surface normal at face center
                    double[] normal = GetFaceNormal(face);
                    if (normal == null)
                        continue;

                    // Compute dot product with target direction
                    double alignment = DotProduct(normal, targetVector);

                    // Track best aligned face
                    if (alignment > bestAlignment)
                    {
                        bestAlignment = alignment;
                        bestFace = face;
                    }
                }
            }

            if (bestFace != null)
            {
                logger?.Info($"Found '{target}' face (alignment: {bestAlignment:F3})");
                logger?.Info($"  Checked {totalFaces} total faces ({planarFaces} planar)");
                return bestFace;
            }
            else
            {
                logger?.Warn($"No suitable '{target}' face found (threshold: {ALIGNMENT_THRESHOLD})");
                logger?.Warn($"  Checked {totalFaces} total faces ({planarFaces} planar)");
                return null;
            }
        }

        /// <summary>
        /// Get the surface normal vector for a planar face.
        /// </summary>
        /// <param name="face">The face to analyze</param>
        /// <returns>Normal vector [x, y, z] in model coordinates, or null if unable to compute</returns>
        private static double[] GetFaceNormal(IFace2 face)
        {
            try
            {
                ISurface surface = (ISurface)face.GetSurface();
                if (surface == null || !surface.IsPlane())
                    return null;

                // Get plane parameters
                // SolidWorks plane is defined by: ax + by + cz = d
                // where [a, b, c] is the normal vector
                double[] planeParams = (double[])surface.PlaneParams;
                if (planeParams == null || planeParams.Length < 3)
                    return null;

                // Extract normal components (first 3 values)
                double[] normal = new[] { planeParams[0], planeParams[1], planeParams[2] };

                // Normalize the vector
                double length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
                if (length < 1e-9)
                    return null;

                normal[0] /= length;
                normal[1] /= length;
                normal[2] /= length;

                return normal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compute dot product of two 3D vectors.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>Dot product (ranges from -1 to 1)</returns>
        private static double DotProduct(double[] a, double[] b)
        {
            if (a == null || b == null || a.Length < 3 || b.Length < 3)
                return 0.0;

            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        /// <summary>
        /// Get all edges from a face (helper for fillet operations).
        /// </summary>
        /// <param name="face">Face to extract edges from</param>
        /// <returns>Array of edges, or empty array if none</returns>
        public static IEdge[] GetFaceEdges(IFace2 face)
        {
            if (face == null)
                return Array.Empty<IEdge>();

            try
            {
                object[] edgesObj = (object[])face.GetEdges();
                if (edgesObj == null || edgesObj.Length == 0)
                    return Array.Empty<IEdge>();

                return edgesObj.Cast<IEdge>().ToArray();
            }
            catch
            {
                return Array.Empty<IEdge>();
            }
        }

        /// <summary>
        /// Get all edges from a model feature (helper for fillet operations).
        /// </summary>
        /// <param name="feature">Feature to extract edges from</param>
        /// <returns>Array of edges, or empty array if none</returns>
        public static IEdge[] GetFeatureEdges(IFeature feature)
        {
            if (feature == null)
                return Array.Empty<IEdge>();

            try
            {
                object[] facesObj = (object[])feature.GetFaces();
                if (facesObj == null || facesObj.Length == 0)
                    return Array.Empty<IEdge>();

                var edges = new List<IEdge>();
                foreach (object faceObj in facesObj)
                {
                    IFace2 face = (IFace2)faceObj;
                    edges.AddRange(GetFaceEdges(face));
                }

                // Remove duplicates (edges shared between faces)
                return edges.Distinct().ToArray();
            }
            catch
            {
                return Array.Empty<IEdge>();
            }
        }
    }
}

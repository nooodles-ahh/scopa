﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Scopa
{
    /// <summary>
    /// Represents a coplanar, directed polygon with at least 3 vertices. Uses high-precision value types.
    /// </summary>
    public class Polygon
    {
        public IReadOnlyList<Vector3> Vertices { get; }

        public Plane Plane;
        public Vector3 Origin => Vertices.Aggregate(Vector3.zero, (x, y) => x + y) / Vertices.Count;

        /// <summary>
        /// Creates a polygon from a list of points
        /// </summary>
        /// <param name="vertices">The vertices of the polygon</param>
        public Polygon(IEnumerable<Vector3> vertices)
        {
            Vertices = vertices.ToList();
            Plane = new Plane(Vertices[0], Vertices[1], Vertices[2]);
        }

        /// <summary>
        /// Creates a polygon from a plane and a radius.
        /// Expands the plane to the radius size to create a large polygon with 4 vertices.
        /// </summary>
        /// <param name="plane">The polygon plane</param>
        /// <param name="radius">The polygon radius</param>
        public Polygon(Plane plane, float radius = 10000f)
        {
            // Get aligned up and right axes to the plane
            var direction = plane.GetClosestAxisToNormal();
            var tempV = direction == Vector3.forward ? Vector3.up : Vector3.forward;
            var up = tempV.Cross(plane.Normal).Normalise();
            var right = plane.Normal.Cross(up).Normalise();
            up *= radius;
            right *= radius;

            var verts = new List<Vector3>
            {
                plane.PointOnPlane + right + up, // Top right
                plane.PointOnPlane - right + up, // Top left
                plane.PointOnPlane - right - up, // Bottom left
                plane.PointOnPlane + right - up, // Bottom right
            };
            
            // var origin = verts.Aggregate(Vector3.zero, (x, y) => x + y) / verts.Count;
            Vertices = verts.ToList();

            Plane = plane.Clone();
        }

        /// <summary>
        /// Determines if this polygon is behind, in front, or spanning a plane. Returns calculated data.
        /// </summary>
        /// <param name="p">The plane to test against</param>
        /// <param name="classifications">The OnPlane classification for each vertex</param>
        /// <param name="front">The number of vertices in front of the plane</param>
        /// <param name="back">The number of vertices behind the plane</param>
        /// <param name="onplane">The number of vertices on the plane</param>
        /// <returns>A PlaneClassification value.</returns>
        private PlaneClassification ClassifyAgainstPlane(Plane p, out int[] classifications, out int front, out int back, out int onplane)
        {
            var count = Vertices.Count;
            front = 0;
            back = 0;
            onplane = 0;
            classifications = new int[count];

            for (var i = 0; i < Vertices.Count; i++)
            {
                var test = p.OnPlane(Vertices[i]);

                // Vertices on the plane are both in front and behind the plane in this context
                if (test <= 0) back++;
                if (test >= 0) front++;
                if (test == 0) onplane++;

                classifications[i] = test;
            }

            if (onplane == count) return PlaneClassification.OnPlane;
            if (front == count) return PlaneClassification.Front;
            if (back == count) return PlaneClassification.Back;
            return PlaneClassification.Spanning;
        }

        /// <summary>
        /// Splits this polygon by a clipping plane, returning the back and front planes.
        /// The original polygon is not modified.
        /// </summary>
        /// <param name="clip">The clipping plane</param>
        /// <param name="back">The back polygon</param>
        /// <param name="front">The front polygon</param>
        /// <returns>True if the split was successful</returns>
        public bool Split(Plane clip, out Polygon back, out Polygon front)
        {
            return SplitNew(clip, out back, out front, out _, out _);
        }

        /// <summary>
        /// Splits this polygon by a clipping plane, returning the back and front planes.
        /// The original polygon is not modified.
        /// </summary>
        /// <param name="clip">The clipping plane</param>
        /// <param name="back">The back polygon</param>
        /// <param name="front">The front polygon</param>
        /// <param name="coplanarBack">If the polygon rests on the plane and points backward, this will not be null</param>
        /// <param name="coplanarFront">If the polygon rests on the plane and points forward, this will not be null</param>
        /// <returns>True if the split was successful</returns>

        public bool SplitOld(Plane clip, out Polygon back, out Polygon front, out Polygon coplanarBack, out Polygon coplanarFront)
        {
            var count = Vertices.Count;

            var classify = ClassifyAgainstPlane(clip, out var classifications, out _, out _, out _);

            // If the polygon doesn't span the plane, return false.
            if (classify != PlaneClassification.Spanning)
            {
                back = front = null;
                coplanarBack = coplanarFront = null;
                if (classify == PlaneClassification.Back) back = this;
                else if (classify == PlaneClassification.Front) front = this;
                else if (Plane.Normal.Dot(clip.Normal) > 0) coplanarFront = this;
                else coplanarBack = this;
                return false;
            }

            // Get the new front and back vertices
            var backVerts = new List<Vector3>();
            var frontVerts = new List<Vector3>();
            var prev = 0;

            for (var i = 0; i <= count; i++)
            {
                var idx = i % count;
                var end = Vertices[idx];
                var cls = classifications[idx];

                // Check plane crossing
                if (i > 0 && cls != 0 && prev != 0 && prev != cls)
                {
                    // This line end point has crossed the plane
                    // Add the line intersect to the 
                    var start = Vertices[i - 1];
                    // var line = new Line(start, end);
                    var isect = clip.GetIntersectionPoint(start, end);
                    if (isect == null) Debug.LogError("Expected intersection, got null.");
                    frontVerts.Add(isect.Value);
                    backVerts.Add(isect.Value);
                }

                // Add original points
                if (i < Vertices.Count)
                {
                    // OnPlane points get put in both polygons, doesn't generate split
                    if (cls >= 0) frontVerts.Add(end);
                    if (cls <= 0) backVerts.Add(end);
                }

                prev = cls;
            }

            back = new Polygon(backVerts);
            front = new Polygon(frontVerts);
            coplanarBack = coplanarFront = null;

            return true;
        }

        public bool SplitNew(Plane clip, out Polygon back, out Polygon front, out Polygon coplanarBack, out Polygon coplanarFront)
        {
            const float epsilon = 0.000001f;
            
            var distances = Vertices.Select(clip.EvalAtPoint).ToList();
            
            int cb = 0, cf = 0;
            for (var i = 0; i < distances.Count; i++)
            {
                if (distances[i] < -epsilon) cb++;
                else if (distances[i] > epsilon) cf++;
                else distances[i] = 0;
            }

            // Check non-spanning cases
            if (cb == 0 && cf == 0)
            {
                // Co-planar
                back = front = coplanarBack = coplanarFront = null;
                if (Plane.Normal.Dot(clip.Normal) >= 0) coplanarFront = this;
                else coplanarBack = this;
                return false;
            }
            else if (cb == 0)
            {
                // All vertices in front
                back = coplanarBack = coplanarFront = null;
                front = this;
                return false;
            }
            else if (cf == 0)
            {
                // All vertices behind
                front = coplanarBack = coplanarFront = null;
                back = this;
                return false;
            }

            // Get the new front and back vertices
            var backVerts = new List<Vector3>();
            var frontVerts = new List<Vector3>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var j = (i + 1) % Vertices.Count;

                Vector3 s = Vertices[i], e = Vertices[j];
                float sd = distances[i], ed = distances[j];

                if (sd <= 0) backVerts.Add(s);
                if (sd >= 0) frontVerts.Add(s);

                if ((sd < 0 && ed > 0) || (ed < 0 && sd > 0))
                {
                    var t = sd / (sd - ed);
                    var intersect = s * (1 - t) + e * t;

                    backVerts.Add(intersect);
                    frontVerts.Add(intersect);
                }
            }
            
            back = new Polygon(backVerts.Select(x => new Vector3(x.x, x.y, x.z)));
            front = new Polygon(frontVerts.Select(x => new Vector3(x.x, x.y, x.z)));
            coplanarBack = coplanarFront = null;

            return true;
        }
    }
}
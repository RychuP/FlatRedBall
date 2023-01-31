﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlatRedBall.Math.Geometry
{
    public class CapsulePolygon : Polygon
    {
        bool mSupressPointsRecalculation;
        Point[] mUnrotatedPoints;

        float mWidth;
        public float Width
        {
            get { return mWidth; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(Width) + " must be bigger than zero.", nameof(Width));
                }
                else
                {
                    bool changed = mWidth != value;
                    mWidth = value;

                    if (changed)
                    {
                        RecalculatePoints(true);
                    }
                }
            }
        }

        float mHeight;
        public float Height
        {
            get { return mHeight; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(Height) + " must be bigger than zero.", nameof(Height));
                }
                else
                {
                    bool changed = mHeight != value;
                    mHeight = value;

                    if (changed)
                    {
                        RecalculatePoints(true);
                    }
                }
            }
        }

        int mSemicircleNumberOfSegments;

        /// <summary>
        /// Number of segments used to define each of the round edges. The more segments the more accurate the semicircle will be.
        /// </summary>
        public int SemicircleNumberOfSegments
        {
            get { return mSemicircleNumberOfSegments; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("The number of segments per semicircle must be bigger than one.", nameof(SemicircleNumberOfSegments));
                }
                else
                {
                    bool changed = mSemicircleNumberOfSegments != value;
                    mSemicircleNumberOfSegments = value;

                    if (changed)
                    {
                        RecalculatePoints(true);
                    }
                }
            }
        }

        public new float RotationZ
        {
            get
            {
                return base.RotationZ;
            }
            set
            {
                bool changed = base.RotationZ != value;
                base.RotationZ = value;

                if (changed)
                {
                    RecalculatePoints(false);
                }
            }
        }

        // We don't want the user to modify the points since it could mess the shape and it would no longer resemble a capsule.
        public new IList<Point> Points { get => new ReadOnlyCollection<Point>(base.Points); }

        int SemicircleNumberOfPoints => mSemicircleNumberOfSegments - 1;
        int NumberOfShapePoints => SemicircleNumberOfPoints * 2 + 5;

        public CapsulePolygon() : this(32, 16, 8) { }

        public CapsulePolygon(float width, float height, int semicircleNumberOfSegments)
        {
            mSupressPointsRecalculation = true;

            Width = width;
            Height = height;
            SemicircleNumberOfSegments = semicircleNumberOfSegments;

            mSupressPointsRecalculation = false;

            RecalculatePoints(true);
        }

        void RecalculatePoints(bool recalculateShape)
        {
            if (mSupressPointsRecalculation)
            {
                return;
            }

            // Only recreate the shape if the width, height or number of segments change
            if (recalculateShape)
            {
                // Miguel: I'm creating the capsule in a linear fashion. One straight line
                // followed by a semicircle, another straight line and another semicircle.

                int semicircleNumberOfPoints = SemicircleNumberOfPoints;
                mUnrotatedPoints = new Point[NumberOfShapePoints];

                float halfWidth = mWidth / 2;
                float halfHeight = mHeight / 2;
                float semicircleCenter;
                float radiansPerSemicircleStep = MathHelper.Pi / mSemicircleNumberOfSegments;

                Vector2 s1, s2, r1, r2, r3, r4, s1Direction, s2Direction;

                bool horizontal = mWidth > mHeight;

                if (horizontal)
                {
                    semicircleCenter = halfWidth - halfHeight;
                    s1 = new Vector2(semicircleCenter, 0);
                    s2 = new Vector2(-semicircleCenter, 0);
                    r1 = new Vector2(-semicircleCenter, halfHeight);
                    r2 = new Vector2(semicircleCenter, halfHeight);
                    r3 = new Vector2(semicircleCenter, -halfHeight);
                    r4 = new Vector2(-semicircleCenter, -halfHeight);
                    s1Direction = new Vector2(0, -halfHeight);
                    s2Direction = new Vector2(0, halfHeight);
                }
                else // Vertical
                {
                    semicircleCenter = halfHeight - halfWidth;
                    s1 = new Vector2(0, semicircleCenter);
                    s2 = new Vector2(0, -semicircleCenter);
                    r1 = new Vector2(-halfWidth, -semicircleCenter);
                    r2 = new Vector2(-halfWidth, semicircleCenter);
                    r3 = new Vector2(halfWidth, semicircleCenter);
                    r4 = new Vector2(halfWidth, -semicircleCenter);
                    s1Direction = new Vector2(-halfWidth, 0);
                    s2Direction = new Vector2(halfWidth, 0);
                }

                // Add first straight points
                mUnrotatedPoints[0] = new Point(ref r1);
                mUnrotatedPoints[1] = new Point(ref r2);
                int lastPointInserted = 1;

                // Add first semicircle points
                for (int i = 0; i < semicircleNumberOfPoints; i++)
                {
                    float rotationAmount = (i + 1) * radiansPerSemicircleStep * (horizontal ? 1f : -1f);
                    var rotation = Matrix.CreateRotationZ(rotationAmount);
                    var rotatedVector = Vector2.Transform(s1Direction, rotation);
                    var finalPosition = s1 + rotatedVector;
                    finalPosition.Y *= !horizontal ? 1f : -1f;
                    mUnrotatedPoints[lastPointInserted + i + 1] = new Point(ref finalPosition);
                }

                // Add second straight points
                lastPointInserted = 1 + semicircleNumberOfPoints;
                mUnrotatedPoints[lastPointInserted + 1] = new Point(ref r3);
                mUnrotatedPoints[lastPointInserted + 2] = new Point(ref r4);
                lastPointInserted += 2;

                // Add second semicircle points
                for (int i = 0; i < semicircleNumberOfPoints; i++)
                {
                    float rotationAmount = (i + 1) * radiansPerSemicircleStep * (horizontal ? 1f : -1f);
                    var rotation = Matrix.CreateRotationZ(rotationAmount);
                    var rotatedVector = Vector2.Transform(s2Direction, rotation);
                    var finalPosition = s2 + rotatedVector;
                    finalPosition.Y *= !horizontal ? 1f : -1f;
                    mUnrotatedPoints[lastPointInserted + i + 1] = new Point(ref finalPosition);
                }

                lastPointInserted += semicircleNumberOfPoints;

                // Add closing point
                mUnrotatedPoints[lastPointInserted + 1] = new Point(ref r1);
            }

            if (mRotationZ == 0)
            {
                base.Points = mUnrotatedPoints;
            }
            else
            {
                // Make a copy of the unrotated points and apply rotation to the copy.
                // This allows us to skip shape recreation when only RotationZ changes.
                var rotatedPoints = new Point[NumberOfShapePoints];
                mUnrotatedPoints.CopyTo(rotatedPoints, 0);

                for (int i = 0; i < rotatedPoints.Length; i++)
                {
                    MathFunctions.RotatePointAroundPoint(Point.Zero, ref rotatedPoints[i], mRotationZ);
                }

                base.Points = rotatedPoints;
            }
        }
    }
}
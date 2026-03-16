using System;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorSpiralStairsSettings : IShapeSettings
    {
        public const float kMinStepDepth = 0.1f;
        public const float kMinStepHeight = 0.1f;
        public const int kMaxSteps = 300;

        public AABB bounds = new AABB();
        public Vector3[] vertices = new Vector3[0];
        public int[] vertexIDs = new int[0];
        public bool[] onGeometryVertices = new bool[0];

        [SerializeField] private float stepDepth;

        public float StepDepth
        {
            get => stepDepth;
            private set
            {
                if(stepDepth == value)
                {
                    return;
                }

                stepDepth                          = value;
                CSGSettings.LinearStairsStepLength = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float stepHeight;

        public float StepHeight
        {
            get => stepHeight;
            private set
            {
                if(stepHeight == value)
                {
                    return;
                }

                stepHeight                         = value;
                CSGSettings.LinearStairsStepHeight = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float stairsWidth;

        public float StairsWidth
        {
            get => stairsWidth;
            private set
            {
                if(stairsWidth == value)
                {
                    return;
                }

                stairsWidth                       = value;
                CSGSettings.LinearStairsStepWidth = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private int totalSteps;

        public int TotalSteps
        {
            get => Mathf.Clamp(totalSteps, 1, kMaxSteps);
            private set
            {
                if(totalSteps == value)
                {
                    return;
                }

                totalSteps                         = Mathf.Clamp(value, 1, kMaxSteps);
                CSGSettings.LinearStairsTotalSteps = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float stairsDepth;

        public float StairsDepth
        {
            get => stairsDepth;
            private set
            {
                if(stairsDepth == value)
                {
                    return;
                }

                stairsDepth                    = value;
                CSGSettings.LinearStairsLength = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float stairsHeight;

        public float StairsHeight
        {
            get => stairsHeight;
            private set
            {
                if(stairsHeight == value)
                {
                    return;
                }

                stairsHeight                   = value;
                CSGSettings.LinearStairsHeight = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float extraDepth;

        public float ExtraDepth
        {
            get => extraDepth;
            private set
            {
                if(extraDepth == value)
                {
                    return;
                }

                extraDepth                           = value;
                CSGSettings.LinearStairsLengthOffset = value;
                CSGSettings.Save();
            }
        }

        [SerializeField] private float extraHeight;

        public float ExtraHeight
        {
            get => extraHeight;
            private set
            {
                if(extraHeight == value)
                {
                    return;
                }

                extraHeight                          = value;
                CSGSettings.LinearStairsHeightOffset = value;
                CSGSettings.Save();
            }
        }

        public void CalcTotalSteps()
        {
            totalSteps = Mathf.Max(1, Mathf.RoundToInt((stairsHeight - extraHeight) / stepHeight));
        }

        public void SetStairsWidth(float newWidth)
        {
            AABB newBounds = bounds;

            if(newWidth >= 0)
            {
                newBounds.MaxX = newBounds.MinX + newWidth;
            }
            else
            {
                newBounds.MinX = newBounds.MaxX - newWidth;
            }

            SetBounds(newBounds);
        }

        public void SetStairsHeight(float newHeight)
        {
            AABB newBounds = bounds;

            if(newHeight >= 0)
            {
                newBounds.MaxY = newBounds.MinY + newHeight;
            }
            else
            {
                newBounds.MinY = newBounds.MaxY - newHeight;
            }

            SetBounds(newBounds);
        }

        public void SetStairsDepth(float newDepth)
        {
            AABB newBounds = bounds;

            if(newDepth >= 0)
            {
                newBounds.MaxZ = newBounds.MinZ + newDepth;
            }
            else
            {
                newBounds.MinZ = newBounds.MaxZ - newDepth;
            }

            SetBounds(newBounds);
        }

        public void SetBounds(AABB newBounds, bool autoAdjust = true)
        {
            Vector3 boundsSize = newBounds.Size;
            stairsWidth  = GeometryUtility.CleanLength(boundsSize.x);
            stairsHeight = GeometryUtility.CleanLength(boundsSize.y);
            stairsDepth  = GeometryUtility.CleanLength(boundsSize.z);

            if(float.IsInfinity(stairsWidth) || float.IsNaN(stairsWidth))
            {
                stairsWidth = 1.0f;
            }

            if(float.IsInfinity(stairsDepth) || float.IsNaN(stairsDepth))
            {
                stairsDepth = stepDepth;
            }

            if(float.IsInfinity(stairsHeight) || float.IsNaN(stairsHeight))
            {
                stairsHeight = stepHeight;
            }

            stairsWidth  = Mathf.Max(0, stairsWidth);
            stairsDepth  = Mathf.Max(0, stairsDepth);
            stairsHeight = Mathf.Max(0, stairsHeight);

            if(autoAdjust)
            {
                if(!bounds.IsEmpty())
                {
                    float offsetY = newBounds.MaxY - bounds.MaxY + (bounds.MinY - newBounds.MinY);
                    float offsetZ = newBounds.MaxZ - bounds.MaxZ + (bounds.MinZ - newBounds.MinZ);

                    if(offsetY != 0) // scaling in height direction
                    {
                        if(offsetY > 0) // growing
                        {
                            if(extraDepth > 0)
                            {
                                extraDepth = stairsDepth
                                  - Mathf.Max(0, stairsHeight - extraHeight) / stepHeight * stepDepth;
                            }
                            else
                            {
                                extraDepth  -= offsetY;
                                extraHeight += offsetY;
                            }
                        }
                        else // shrinking
                        {
                            extraHeight += offsetY;

                            extraDepth = stairsDepth
                              - Mathf.Max(0, stairsHeight - extraHeight) / stepHeight * stepDepth;
                        }
                    }

                    if(offsetZ != 0) // scaling in depth direction
                    {
                        if(offsetZ > 0) // growing
                        {
                            if(extraHeight > 0)
                            {
                                extraHeight = stairsHeight
                                  - Mathf.Max(0, stairsDepth - extraDepth) / stepDepth * stepHeight;
                            }
                            else
                            {
                                extraDepth += offsetZ;

                                if(extraDepth < 0)
                                {
                                    extraDepth = 0;
                                }

                                extraHeight -= offsetZ;
                            }
                        }
                        else // shrinking
                        {
                            if(extraDepth > 0)
                            {
                                extraDepth = Mathf.Max(0, extraDepth + offsetZ);
                            }

                            extraHeight = stairsHeight
                              - Mathf.Max(0, stairsDepth - extraDepth) / stepDepth * stepHeight;
                        }

                        extraDepth = stairsDepth - Mathf.Max(0, stairsHeight - extraHeight) / stepHeight * stepDepth;
                    }

                    if(extraDepth < 0)
                    {
                        extraDepth = 0;
                    }

                    if(extraHeight < 0)
                    {
                        extraHeight = 0;
                    }
                }
                else
                {
                    extraDepth  = Mathf.Max(0, stairsDepth - totalSteps * stepDepth);
                    extraHeight = 0;
                }

                CalcTotalSteps();
            }

            bounds = newBounds;
        }

        public void SetExtraHeight(float newHeightOffset)
        {
            extraHeight = Mathf.Min(stairsHeight, Mathf.Max(0, newHeightOffset));
            float totalStepHeight  = stairsHeight - extraHeight;
            float totalStepLength  = stairsDepth - extraDepth;
            float totalHeightSteps = Mathf.Max(1, totalStepHeight / stepHeight);
            stepDepth = Mathf.Max(kMinStepDepth, totalStepLength / totalHeightSteps);
            CalcTotalSteps();
        }

        public void SetExtraDepth(float newLengthOffset)
        {
            CalcTotalSteps();
            float minTotalStepLength = totalSteps * kMinStepDepth;
            extraDepth = Mathf.Min(stairsDepth - minTotalStepLength, Mathf.Max(0, newLengthOffset));
            float totalStepHeight  = stairsHeight - extraHeight;
            float totalStepLength  = stairsDepth - extraDepth;
            float totalHeightSteps = Mathf.Max(1, totalStepHeight / stepHeight);
            stepDepth = Mathf.Max(kMinStepDepth, totalStepLength / totalHeightSteps);
        }

        public void SetStepDepth(float newStepDepth)
        {
            extraDepth = stairsDepth - totalSteps * stepDepth;
            stepDepth  = Mathf.Max(kMinStepDepth, newStepDepth);
            float totalDepth = stepDepth * totalSteps + extraDepth;
            AABB  newBounds  = bounds;

            if(totalDepth >= 0)
            {
                newBounds.MaxZ = newBounds.MinZ + totalDepth;
            }
            else
            {
                newBounds.MinZ = newBounds.MaxZ - totalDepth;
            }

            SetBounds(newBounds, false);
            CalcTotalSteps();
        }

        public void SetStepHeight(float newStepHeight)
        {
            stepHeight = Mathf.Max(kMinStepHeight, newStepHeight);
            CalcTotalSteps();
        }

        public void SetTotalSteps(int newTotalSteps)
        {
            extraDepth = stairsDepth - Mathf.Max(0, stairsHeight - extraHeight) / stepHeight * stepDepth;
            float totalDepth  = stepDepth * newTotalSteps + extraDepth;
            float totalHeight = stepHeight * newTotalSteps + extraHeight;

            totalSteps = newTotalSteps;

            AABB newBounds = bounds;

            if(totalHeight >= 0)
            {
                newBounds.MaxY = newBounds.MinY + totalHeight;
            }
            else
            {
                newBounds.MinY = newBounds.MaxY - totalHeight;
            }

            if(totalDepth >= 0)
            {
                newBounds.MaxZ = newBounds.MinZ + totalDepth;
            }
            else
            {
                newBounds.MinZ = newBounds.MaxZ - totalDepth;
            }

            SetBounds(newBounds, false);
        }

        public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
        }

        public void SetPoint(int index, Vector3 position)
        {
            if(vertices.Length < index)
            {
                return;
            }

            if(vertices.Length == index)
            {
                ArrayUtility.Add(ref vertices, position);

                return;
            }

            vertices[index] = position;
        }

        public void CalculatePlane(ref CSGPlane plane)
        {
        }

        public void Reset()
        {
            stepDepth    = CSGSettings.LinearStairsStepLength;
            stepHeight   = CSGSettings.LinearStairsStepHeight;
            stairsWidth  = CSGSettings.LinearStairsStepWidth;
            totalSteps   = Mathf.Max(1, CSGSettings.LinearStairsTotalSteps);
            stairsDepth  = CSGSettings.LinearStairsLength;
            stairsHeight = CSGSettings.LinearStairsHeight;
            extraDepth   = CSGSettings.LinearStairsLengthOffset;
            extraHeight  = CSGSettings.LinearStairsHeightOffset;

            vertices    = new Vector3[0];
            extraDepth  = 0;
            extraHeight = 0;
            bounds.Reset();
        }

        public Vector3[] GetVertices(
            CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid
        )
        {
            isValid = true;

            return vertices;
        }

        public void ProjectShapeOnBuildPlane(CSGPlane plane)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = plane.Project(vertices[i]);
            }
        }

        public void MoveShape(Vector3 offset)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i] + offset;
            }
        }

        public Vector3 GetCenter(CSGPlane plane)
        {
            return vertices[0];
        }

        public AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
        {
            AABB bounds = new AABB();
            bounds.Reset();

            for(int i = 0; i < vertices.Length; i++)
            {
                bounds.Extend(rotation * vertices[i]);
            }

            return bounds;
        }
    }
}

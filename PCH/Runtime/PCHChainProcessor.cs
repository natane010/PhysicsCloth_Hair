using TK.Mono;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TK.Mono
{
    public class PCHChainProcessor: PCHRuntimePoint, IPCHPhysicMonoComponent
    {
        public const string virtualKey = " virtual";
        [SerializeField]
        private PCHPhysicsSetting PCHSetting;
        public Transform RootTransform { get { return transform; } }

        public List<PCHRuntimePoint> fixedPointList { get { return ChildPoints; } }
        public Transform[] allPointTransforms;
        public List<PCHRuntimePoint> allPointList;
        public bool isUseLocalRadiusAndColliderMask;
        public bool isInitialize;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsStructuralVertical;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsStructuralHorizontal;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsShear;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsBendingVertical;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsBendingHorizontal;
        [SerializeField]
        private List<PCHRuntimeConstraint> constraintsCircumference;

        private ConstraintRead[][] constraintList;
        private PointRead[] pointReadList;
        private PointReadWrite[] pointReadWriteList;
        private int maxPointDepth;
        private float maxChainLength;

        public static PCHChainProcessor CreatePCHChainProcessor(Transform rootTransform, string keyWord, PCHPhysicsSetting setting) 
        {
            PCHChainProcessor chainProcessor = rootTransform.gameObject.AddComponent<PCHChainProcessor>();
            chainProcessor.keyWord = keyWord;
            chainProcessor.SetDepth(-1);
            chainProcessor.index = -1;
            chainProcessor.allowCreateAllConstraint = false;
            chainProcessor.initialScale = rootTransform.lossyScale;

            chainProcessor.allPointList = new List<PCHRuntimePoint>();
            chainProcessor.maxPointDepth = 1;
            chainProcessor.PCHSetting = setting;

            return chainProcessor;
        }
        public static PCHChainProcessor CreatePCHChainProcessor(PCHRuntimePoint rootPoint ,PCHPhysicsSetting setting)
        {
            PCHChainProcessor chainProcessor = rootPoint.gameObject.AddComponent<PCHChainProcessor>();
            chainProcessor.keyWord = rootPoint.keyWord;
            chainProcessor.SetDepth(-1);
            chainProcessor.index = -1;
            chainProcessor.allowCreateAllConstraint = false;
            chainProcessor.initialScale = rootPoint.transform.lossyScale;

            chainProcessor.allPointList = new List<PCHRuntimePoint>();
            chainProcessor.maxPointDepth = 1;
            chainProcessor.PCHSetting = setting;

            chainProcessor.AddChild(rootPoint.ChildPoints);
            DestroyImmediate(rootPoint);

            return chainProcessor;
        }
        public  void Clear()
        {
            allPointList.Clear();
            allPointTransforms= null;
        }
        public void Initialize()
        {
            if (isInitialize)
            {
                Clear();
            }

            SerializeAndSearchAllPoints(this, ref allPointList, out maxPointDepth);
            CreatePointStruct1(allPointList);

            UpdateJointConnection(fixedPointList);
            CreationConstraintList();
            ComputeWeight();
            UpdatePointStruct();
            GetMaxDeep();
            isInitialize = true;
        }

        private void GetMaxDeep()
        {
            maxChainLength = GetMaxDeep(this);
            if (PCHSetting!=null)
            {
                maxChainLength *= PCHSetting.structuralStretchVertical * 1.1f; ;
            }

        }

        private void UpdatePointStruct()
        {
            allPointTransforms = new Transform[allPointList.Count];
            for (int i = 0; i < allPointList.Count; i++)
            {
                pointReadList[i] = allPointList[i].pointRead;
                pointReadWriteList[i] = allPointList[i].pointReadWrite;
                allPointTransforms[i] = allPointList[i].transform;

            }
        }
        #region point

        private void SerializeAndSearchAllPoints(PCHRuntimePoint point, ref List<PCHRuntimePoint> allPointList, out int maxPointDepth)
        {
            if (point == null|| point.transform==null)
            {
                maxPointDepth = 0;
                return;
            }
            if (point.ChildPoints == null || point.ChildPoints.Count == 0)
            {
                if (Application.isPlaying && PCHSetting != null && PCHSetting.isComputeVirtual && (!point.transform.name.Contains(virtualKey)))
                {
                    Transform childPointTrans = new GameObject(point.transform.name + virtualKey).transform;
                    childPointTrans.position = point.transform.position + ((point.Parent != null && point.Parent.depth != -1 && !PCHSetting.ForceLookDown) ?
                        (point.transform.position - point.Parent.transform.position).normalized * PCHSetting.virtualPointAxisLength :
                        Vector3.down * PCHSetting.virtualPointAxisLength);

                    childPointTrans.parent = point.transform;

                    PCHRuntimePoint virtualPoint = PCHRuntimePoint.CreateRuntimePoint(childPointTrans, point.depth + 1, point.keyWord, PCHSetting.isAllowComputeOtherConstraint);
                    point.AddChild(virtualPoint);
                }
                else
                {
                    point.pointRead.childFirstIndex = -1;
                    point.pointRead.childLastIndex = -1;
                    point.pointDepthRateMaxPointDepth = 1;
                    maxPointDepth = point.depth;
                    return;
                }
            }

            point.pointRead.childFirstIndex = allPointList.Count;
            point.pointRead.childLastIndex = point.pointRead.childFirstIndex + point.ChildPoints.Count;

            maxPointDepth = point.depth;
            if (point.ChildPoints.Count!=0)
            
            {
                for (int i = 0; i < point.ChildPoints.Count; i++)
                {

                    PCHRuntimePoint childPoint = point.ChildPoints[i];

                    childPoint.pointRead.parentIndex = point.index;
                    childPoint.pointRead.fixedIndex = point.pointRead.fixedIndex;
                    childPoint.Parent = point;

                    childPoint.pointRead.initialLocalPosition = Quaternion.Inverse(point.transform.rotation) * (childPoint.transform.position - point.transform.position).normalized;
                    childPoint.pointRead.initialLocalPositionLength = (childPoint.transform.position - point.transform.position).magnitude;
                    childPoint.pointRead.initialLocalRotation = childPoint.transform.localRotation;
                    childPoint.pointRead.initialRotation = childPoint.transform.rotation;
                    childPoint.index = allPointList.Count;

                    if (childPoint.isFixed)
                    {
                        childPoint.pointRead.fixedIndex = childPoint.index;
                        childPoint.pointRead.initialPosition = Vector3.zero;
                    }
                    else
                    {
                        childPoint.pointRead.fixedIndex = point.pointRead.fixedIndex;
                        var fixedPoint = allPointList[childPoint.pointRead.fixedIndex];

                        childPoint.pointRead.initialPosition = Quaternion.Inverse(fixedPoint.transform.rotation) * (childPoint.transform.position - fixedPoint.transform.position);

                    }
                    allPointList.Add(childPoint);

                }

                for (int i = 0; i < point.ChildPoints.Count; i++)
                {
                    int maxDeep = point.depth;
                    SerializeAndSearchAllPoints(point.ChildPoints[i], ref allPointList, out maxDeep);
                    if (maxDeep > maxPointDepth)
                    {
                        maxPointDepth = maxDeep;
                        point.pointDepthRateMaxPointDepth = point.depth / (float)maxDeep;
                    }
                }
            }

        }
        private void CreatePointStruct1(List<PCHRuntimePoint> allPointList)
        {
            pointReadList = new PointRead[allPointList.Count];
            pointReadWriteList = new PointReadWrite[allPointList.Count];
            if (PCHSetting==null)
            {
                return;
            }

            for (int i = 0; i < allPointList.Count; ++i)
            {
                var point = allPointList[i];

                float rate = point.pointDepthRateMaxPointDepth;
                if (!isUseLocalRadiusAndColliderMask)
                {
                    point.pointRead.colliderMask = (int)PCHSetting.colliderChoice;
                    point.pointRead.radius = PCHSetting.ispointRadiuCurve? PCHSetting.pointRadiuCurve.Evaluate(rate): PCHSetting.pointRadiuValue;
                }

                point.pointRead.isFixedPointFreezeRotation = PCHSetting.isFixedPointFreezeRotation;


                point.pointRead.gravity = PCHSetting.isgravityScaleCurve ? PCHSetting.gravity * PCHSetting.gravityScaleCurve.Evaluate(rate) : PCHSetting.gravity * PCHSetting.gravityScaleValue;
                point.pointRead.stiffnessWorld = PCHSetting.isstiffnessWorldCurve? PCHSetting.stiffnessWorldCurve.Evaluate(rate):PCHSetting.stiffnessWorldValue;
                point.pointRead.stiffnessLocal = PCHSetting.isstiffnessLocalCurve ? PCHSetting.stiffnessLocalCurve.Evaluate(rate) : PCHSetting.stiffnessWorldValue;
                point.pointRead.elasticity = PCHSetting.iselasticityCurve? PCHSetting.elasticityCurve.Evaluate(rate): PCHSetting.elasticityValue;
                point.pointRead.elasticityVelocity = PCHSetting.iselasticityVelocityCurve ? PCHSetting.elasticityVelocityCurve.Evaluate(rate) : PCHSetting.elasticityVelocityValue;
                point.pointRead.lengthLimitForceScale = PCHSetting.islengthLimitForceScaleCurve ? PCHSetting.lengthLimitForceScaleCurve.Evaluate(rate) : PCHSetting.lengthLimitForceScaleValue;
                point.pointRead.damping = PCHSetting.isdampingCurve ? PCHSetting.dampingCurve.Evaluate(rate) : PCHSetting.dampingValue;
                point.pointRead.moveInert = PCHSetting.ismoveInertCurve ? PCHSetting.moveInertCurve.Evaluate(rate) : PCHSetting.moveInertValue;
                point.pointRead.velocityIncrease = PCHSetting.isvelocityIncreaseCurve ? PCHSetting.velocityIncreaseCurve.Evaluate(rate) : PCHSetting.velocityIncreaseValue;
                point.pointRead.friction = PCHSetting.isfrictionCurve? PCHSetting.frictionCurve.Evaluate(rate):PCHSetting.frictionValue;
                point.pointRead.addForceScale = PCHSetting.isaddForceScaleCurve ? PCHSetting.addForceScaleCurve.Evaluate(rate) : PCHSetting.addForceScaleValue;

                point.pointRead.circumferenceShrink = PCHSetting.iscircumferenceShrinkScaleCurve?  PCHSetting.circumferenceShrinkScaleCurve.Evaluate(rate):PCHSetting.circumferenceShrinkScaleValue*0.5f;
                point.pointRead.circumferenceStretch = PCHSetting.iscircumferenceStretchScaleCurve?  PCHSetting.circumferenceStretchScaleCurve.Evaluate(rate): PCHSetting.circumferenceStretchScaleValue*0.5f;
                point.pointRead.structuralShrinkVertical = PCHSetting.isstructuralShrinkVerticalScaleCurve? PCHSetting.structuralShrinkVerticalScaleCurve.Evaluate(rate):PCHSetting.structuralShrinkVerticalScaleValue*0.5f;
                point.pointRead.structuralStretchVertical = PCHSetting.isstructuralStretchVerticalScaleCurve ?  PCHSetting.structuralStretchVerticalScaleCurve.Evaluate(rate):PCHSetting.structuralStretchVerticalScaleValue*0.5f;
                point.pointRead.structuralShrinkHorizontal = PCHSetting.isstructuralStretchVerticalScaleCurve ?  PCHSetting.structuralShrinkHorizontalScaleCurve.Evaluate(rate):PCHSetting.structuralShrinkHorizontalScaleValue*0.5f;
                point.pointRead.structuralStretchHorizontal = PCHSetting.isstructuralStretchHorizontalScaleCurve? PCHSetting.structuralStretchHorizontalScaleCurve.Evaluate(rate): PCHSetting.structuralStretchHorizontalScaleValue*0.5f;
                point.pointRead.shearShrink = PCHSetting.isshearShrinkScaleCurve ?  PCHSetting.shearShrinkScaleCurve.Evaluate(rate) : PCHSetting.shearShrinkScaleValue*0.5f;
                point.pointRead.shearStretch= PCHSetting.isshearStretchScaleCurve ?  PCHSetting.shearStretchScaleCurve.Evaluate(rate) : PCHSetting.shearStretchScaleValue*0.5f;
                point.pointRead.bendingShrinkVertical = PCHSetting.isbendingShrinkVerticalScaleCurve ?  PCHSetting.bendingShrinkVerticalScaleCurve.Evaluate(rate) : PCHSetting.bendingShrinkVerticalScaleValue*0.5f;
                point.pointRead.bendingStretchVertical = PCHSetting.isbendingStretchVerticalScaleCurve ?  PCHSetting.bendingStretchVerticalScaleCurve.Evaluate(rate) : PCHSetting.bendingStretchVerticalScaleValue*0.5f;
                point.pointRead.bendingShrinkHorizontal = PCHSetting.isbendingShrinkHorizontalScaleCurve ?  PCHSetting.bendingShrinkHorizontalScaleCurve.Evaluate(rate) : PCHSetting.bendingShrinkHorizontalScaleValue*0.5f;
                point.pointRead.bendingStretchHorizontal = PCHSetting.isbendingStretchHorizontalScaleCurve ?  PCHSetting.bendingStretchHorizontalScaleCurve.Evaluate(rate) : PCHSetting.bendingStretchHorizontalScaleValue*0.5f;

                point.pointRead.stiffnessLocal = 1 - Mathf.Clamp01(Mathf.Cos(point.pointRead.stiffnessLocal * Mathf.PI * 0.5f));
                point.pointRead.damping = 0.5f + point.pointRead.damping * 0.5f;
            }
        }

        private void ComputeWeight()
        {
            if (PCHSetting==null)
            {
                return;
            }
            if (PCHSetting.isAutoComputeWeight)
            {
                float[] pointWeight = new float[allPointList.Count];

                float[] HorizontalVector = new float[allPointList.Count];
                float[] VerticalVector = new float[allPointList.Count];
                if (PCHSetting.isComputeStructuralHorizontal)
                {
                    for (int i = 0; i < constraintsStructuralHorizontal.Count; i++)
                    {
                        HorizontalVector[constraintsStructuralHorizontal[i].pointA.index] += constraintsStructuralHorizontal[i].direction.magnitude;
                        HorizontalVector[constraintsStructuralHorizontal[i].pointB.index] += constraintsStructuralHorizontal[i].direction.magnitude;
                    }
                }
                if (PCHSetting.isComputeStructuralVertical)
                {
                    for (int i = 0; i < constraintsStructuralVertical.Count; i++)
                    {
                        VerticalVector[constraintsStructuralVertical[i].pointA.index] += constraintsStructuralVertical[i].direction.magnitude;
                        VerticalVector[constraintsStructuralVertical[i].pointB.index] += constraintsStructuralVertical[i].direction.magnitude;
                    }
                }


                for (int i = 0; i < pointWeight.Length; i++)
                {
                    if (!PCHSetting.isComputeStructuralHorizontal && !PCHSetting.isComputeStructuralVertical)
                    {
                        pointWeight[i] = 1f;
                    }
                    else
                    {
                        pointWeight[i] = (HorizontalVector[i] + VerticalVector[i]) * 0.5f;
                    }

                }
                ComputeWeight(pointWeight, allPointList);
            }
            else
            {
                for (int i = 0; i < allPointList.Count; i++)
                {
                    var point = allPointList[i];
                    if (point.isFixed)
                    {
                        point.pointRead.mass = 1E10f;
                    }
                    else
                    {
                        point.pointRead.mass = PCHSetting.weightCurve.Evaluate(point.pointDepthRateMaxPointDepth);
                        point.pointRead.mass = point.pointRead.mass < 1f ? 1f : point.pointRead.mass;
                    }
                }

            }


        }

        private void ComputeWeight(float[] pointWeight, List<PCHRuntimePoint> allPointList)
        {
            float weightSum = 0;
            for (int i = allPointList.Count - 1; i >= 0; i--)
            {

                if (allPointList[i].isFixed)
                {
                    allPointList[i].pointRead.mass = 1E10f;
                }
                else
                {
                    float weight = pointWeight[i];

                    if (weight <= 0.001f)
                    {
                        Debug.Log(allPointList[i].transform.name + " weight is too small ");
                        weight = 0.001f;
                    }

                    pointWeight[allPointList[i].pointRead.parentIndex] += weight;
                    allPointList[i].pointRead.mass = pointWeight[i];
                    weightSum += pointWeight[i];
                }
            }
            weightSum /= allPointList.Count;

            for (int i = allPointList.Count - 1; i >= 0; i--)
            {
                allPointList[i].pointRead.mass /= weightSum;
            }
        }

        #endregion
        #region constraint
        private void CreationConstraintList()
        {
            if (PCHSetting == null)
            {
                constraintList = new ConstraintRead[0][];
                return;
            }
            var ConstraintReadList = new List<List<ConstraintRead>>();
            int jobcount = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;
            for (int i = 0; i < jobcount; i++)
            {
                ConstraintReadList.Add(new List<ConstraintRead>());
            }
            int constraintindex = 0;
            CheckAndAddConstraint(PCHSetting.isComputeStructuralVertical, ref constraintindex, constraintsStructuralVertical, ref ConstraintReadList);
            CheckAndAddConstraint(PCHSetting.isComputeStructuralHorizontal, ref constraintindex, constraintsStructuralHorizontal, ref ConstraintReadList);
            CheckAndAddConstraint(PCHSetting.isComputeShear, ref constraintindex, constraintsShear, ref ConstraintReadList);
            CheckAndAddConstraint(PCHSetting.isComputeBendingVertical, ref constraintindex, constraintsBendingVertical, ref ConstraintReadList);
            CheckAndAddConstraint(PCHSetting.isComputeBendingHorizontal, ref constraintindex, constraintsBendingHorizontal, ref ConstraintReadList);
            CheckAndAddConstraint(PCHSetting.isComputeCircumference, ref constraintindex, constraintsCircumference, ref ConstraintReadList);

            constraintList = new ConstraintRead[ConstraintReadList.Count][];
            for (int i = 0; i < ConstraintReadList.Count; i++)
            {
                constraintList[i] = ConstraintReadList[i].ToArray();
            }
        }

        private void CheckAndAddConstraint(bool isCompute, ref int constraintIndex, List<PCHRuntimeConstraint> constraintList, ref List<List<ConstraintRead>> ConstraintReadList)
        {
            if (!isCompute)
            {
                return;
            }
            int k = 0;
            for (int i = 0; i < constraintList.Count; i++)
            {

                var constraint = i % 2 == 0 ? constraintList[i / 2] : constraintList[constraintList.Count - 1 - i / 2];

                var isAdd = false;

                for (int j = 0; j < ConstraintReadList.Count; j++, k++)
                {
                    if (k == ConstraintReadList.Count)
                    {
                        k = 0;
                    }

                    if (!ConstraintReadList[k].Contains(constraint.constraintRead))
                    {
                        ConstraintReadList[k].Add(constraint.constraintRead);
                        isAdd = true;
                        break;
                    }
                }
                if (!isAdd)
                {
                    var list = new List<ConstraintRead>();
                    list.Add(constraint.constraintRead);
                    ConstraintReadList.Add(list);
                }
            }

        }

        private void UpdateJointConnection(List<PCHRuntimePoint> fixedPointList)
        {
            if (PCHSetting == null)
            {
                return;
            }
            int HorizontalRootCount = fixedPointList.Count;
            #region Structural_Vertical
            constraintsStructuralVertical = new List<PCHRuntimeConstraint>();
            {
                for (int i = 0; i < HorizontalRootCount; ++i)
                {
                    CreateConstraintStructuralVertical(fixedPointList[i], ref constraintsStructuralVertical, PCHSetting.structuralShrinkVertical, PCHSetting.structuralStretchVertical);
                }
            }
            #endregion
            #region Structural_Horizontal
            constraintsStructuralHorizontal = new List<PCHRuntimeConstraint>();
            for (int i = 0; i < HorizontalRootCount - 1; ++i)
            {
                CreationConstraintHorizontal(fixedPointList[i + 0], fixedPointList[i + 1], ref constraintsStructuralHorizontal, PCHSetting.structuralShrinkHorizontal, PCHSetting.structuralStretchHorizontal);
            }
            if (PCHSetting.isLoopRootPoints && HorizontalRootCount > 2)
            {
                CreationConstraintHorizontal(fixedPointList[HorizontalRootCount - 1], fixedPointList[0], ref constraintsStructuralHorizontal, PCHSetting.structuralShrinkHorizontal, PCHSetting.structuralStretchHorizontal);
            }
            else
            {
                CreationConstraintHorizontal(fixedPointList[HorizontalRootCount - 1], fixedPointList[0], ref constraintsStructuralHorizontal, 0, float.MaxValue);
            }
            #endregion
            #region Shear
            constraintsShear = new List<PCHRuntimeConstraint>();

            for (int i = 0; i < HorizontalRootCount - 1; ++i)
            {
                CreationConstraintShear(fixedPointList[i + 0], fixedPointList[i + 1], ref constraintsShear, PCHSetting.shearShrink, PCHSetting.shearStretch);
            }

            if (PCHSetting.isLoopRootPoints && HorizontalRootCount > 2)
            {
                CreationConstraintShear(fixedPointList[HorizontalRootCount - 1], fixedPointList[0], ref constraintsShear, PCHSetting.shearShrink, PCHSetting.shearStretch);
            }
            else
            {
                CreationConstraintShear(fixedPointList[HorizontalRootCount - 1], fixedPointList[0], ref constraintsShear, 0, 0);
            }
            #endregion
            #region Bending_Vertical
            constraintsBendingVertical = new List<PCHRuntimeConstraint>();
            for (int i = 0; i < HorizontalRootCount; ++i)
            {
                CreationConstraintBendingVertical(fixedPointList[i], ref constraintsBendingVertical, PCHSetting.bendingShrinkVertical, PCHSetting.bendingStretchVertical);
            }
            #endregion
            #region Bending_Horizontal
            constraintsBendingHorizontal = new List<PCHRuntimeConstraint>();
            CreationConstraintBendingHorizontal(constraintsStructuralHorizontal, ref constraintsBendingHorizontal, PCHSetting.bendingShrinkHorizontal, PCHSetting.bendingStretchHorizontal, PCHSetting.isLoopRootPoints);

            #endregion
            #region Circumference
            constraintsCircumference = new List<PCHRuntimeConstraint>();
            for (int i = 0; i < HorizontalRootCount; ++i)
            {
                CreationConstraintCircumference(fixedPointList[i], ref constraintsCircumference, PCHSetting.circumferenceShrink, PCHSetting.circumferenceStretch);
            }

            #endregion
        }

        private void CreateConstraintStructuralVertical(PCHRuntimePoint Point, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch)
        {
            if (Point == null || Point.ChildPoints == null) return;

            for (int i = 0; i < Point.ChildPoints.Count; ++i)
            {
                PCHRuntimeConstraint constraint = new PCHRuntimeConstraint(ConstraintType.Structural_Vertical, Point, Point.ChildPoints[i], shrink, stretch, PCHSetting.isCollideStructuralVertical);

                ConstraintList.Add(constraint);
                CreateConstraintStructuralVertical(Point.ChildPoints[i], ref ConstraintList, shrink, stretch);
            }
        }

        private void CreationConstraintHorizontal(PCHRuntimePoint PointA, PCHRuntimePoint PointB, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch)
        {
            if ((PointA == null) || (PointB == null)) return;
            if (PointA == PointB) return;

            var childPointAList = PointA.ChildPoints;
            var childPointBList = PointB.ChildPoints;


            if ((childPointAList != null&& childPointAList.Count!=0) && (childPointBList != null&& childPointBList.Count != 0))
            {
                if (!childPointAList[0].allowCreateAllConstraint || !childPointBList[0].allowCreateAllConstraint) return;


                if (childPointAList.Count >= 2)
                {
                    sortByDistance(childPointBList[0], ref childPointAList, false);
                    sortByDistance(childPointAList[childPointAList.Count - 1], ref childPointBList, true);
                    for (int i = 0; i < childPointAList.Count - 1; i++)
                    {
                        ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Structural_Horizontal, childPointAList[i], childPointAList[i + 1], shrink, stretch, PCHSetting.isCollideStructuralHorizontal));
                        CreationConstraintHorizontal(childPointAList[i], childPointAList[i + 1], ref ConstraintList, shrink, stretch);
                    }
                }
                ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Structural_Horizontal, childPointAList[childPointAList.Count - 1], childPointBList[0], shrink, stretch, PCHSetting.isCollideStructuralHorizontal));
                CreationConstraintHorizontal(childPointAList[childPointAList.Count - 1], childPointBList[0], ref ConstraintList, shrink, stretch);
            }
            else if ((childPointAList != null) && (childPointBList == null))
            {
                if (!childPointAList[0].allowCreateAllConstraint) return;

                sortByDistance(PointB, ref childPointAList, false);
                if (childPointAList.Count >= 2)
                {
                    for (int i = 0; i < childPointAList.Count - 1; i++)
                    {
                        ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Structural_Horizontal, childPointAList[i], childPointAList[i + 1], shrink, stretch, PCHSetting.isCollideStructuralHorizontal));
                        CreationConstraintHorizontal(childPointAList[i], childPointAList[i + 1], ref ConstraintList, shrink, stretch);
                    }
                }
                ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Structural_Horizontal, childPointAList[childPointAList.Count - 1], PointB, shrink, stretch, PCHSetting.isCollideStructuralHorizontal));
                CreationConstraintHorizontal(childPointAList[childPointAList.Count - 1], PointB, ref ConstraintList, shrink, stretch);
            }
        }

        private void CreationConstraintShear(PCHRuntimePoint PointA, PCHRuntimePoint PointB, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch)
        {
            if ((PointA == null) || (PointB == null)) return;
            if (PointA == PointB) return;

            var childPointAList = PointA.ChildPoints;
            var childPointBList = PointB.ChildPoints;

            if ((childPointAList != null&& childPointAList.Count!=0) && (childPointBList != null && childPointBList.Count != 0))
            {
                if (!childPointAList[0].allowCreateAllConstraint || !childPointBList[0].allowCreateAllConstraint) return;

                sortByDistance(PointB, ref childPointAList, false);
                sortByDistance(PointA, ref childPointBList, true);
                if (childPointAList.Count >= 2)
                {
                    for (int i = 0; i < childPointAList.Count - 1; i++)
                    {
                        CreationConstraintShear(childPointAList[i], childPointAList[i + 1], ref ConstraintList, shrink, stretch);
                    }
                }
                if (childPointBList.Count >= 2)
                {
                    for (int i = 0; i < childPointBList.Count - 1; i++)
                    {
                        CreationConstraintShear(childPointBList[i], childPointBList[i + 1], ref ConstraintList, shrink, stretch);
                    }
                }
                ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Shear, childPointAList[childPointAList.Count - 1], PointB, shrink, stretch, PCHSetting.isCollideShear));
                ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Shear, childPointBList[0], PointA, shrink, stretch, PCHSetting.isCollideShear));
                CreationConstraintShear(childPointAList[childPointAList.Count - 1], childPointBList[0], ref ConstraintList, shrink, stretch);
            }
            else if ((childPointAList != null ^ childPointBList != null) && !PCHSetting.isComputeStructuralHorizontal)
            {
                PCHRuntimePoint existPoint = childPointAList == null ? PointA : PointB;
                List<PCHRuntimePoint> existList = childPointAList == null ? childPointBList : childPointAList;

                if (!existPoint.allowCreateAllConstraint || !existList[0].allowCreateAllConstraint) return;

                sortByDistance(existPoint, ref existList, false);
                if (existList.Count >= 2)
                {
                    for (int i = 0; i < existList.Count - 1; i++)
                    {
                        CreationConstraintHorizontal(existList[i], existList[i + 1], ref ConstraintList, shrink, stretch);
                    }
                }
                ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Shear, existList[existList.Count - 1], existPoint, shrink, stretch, PCHSetting.isCollideShear));
                CreationConstraintShear(existList[existList.Count - 1], existPoint, ref ConstraintList, shrink, stretch);
            }
        }

        private static void CreationConstraintBendingVertical(PCHRuntimePoint Point, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch)
        {
            if (Point.ChildPoints == null) return;
            foreach (var child in Point.ChildPoints)
            {
                if (child.ChildPoints == null) continue;
                foreach (var grandSon in child.ChildPoints)
                {
                    if (grandSon.allowCreateAllConstraint)
                    {
                        ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Bending_Vertical, Point, grandSon, shrink, stretch, false));
                    }
                }
                CreationConstraintBendingVertical(child, ref ConstraintList, shrink, stretch);
            }
        }

        private static void CreationConstraintBendingHorizontal(List<PCHRuntimeConstraint> horizontalConstraintList, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch, bool isLoop)
        {
            for (int i = 0; i < horizontalConstraintList.Count; i++)
            {

                PCHRuntimeConstraint ConstraintA = horizontalConstraintList[i];

                int j0 = isLoop ? 0 : i;
                for (; j0 < horizontalConstraintList.Count; j0++)
                {
                    PCHRuntimeConstraint ConstraintB = horizontalConstraintList[j0];
                    if (!(ConstraintA.constraintRead.shrink == 0 && ConstraintA.constraintRead.stretch == 2 || 
                        ConstraintB.constraintRead.shrink == 0 && ConstraintB.constraintRead.stretch == 2) &&
                        ConstraintA.pointB == ConstraintB.pointA)
                    {
                        ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Bending_Horizontal, ConstraintA.pointA, ConstraintB.pointB, shrink, stretch, false));
                    }
                }
            }
        }

        private void CreationConstraintCircumference(PCHRuntimePoint point, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch, int deep = 0)
        {
            if (point == null || point.ChildPoints == null) return;

            for (int i = 0; i < point.ChildPoints.Count; i++)
            {

                if (!point.ChildPoints[i].allowCreateAllConstraint) continue;
                if (!(PCHSetting.isComputeStructuralVertical && deep == 0) && !(PCHSetting.isComputeBendingVertical && deep == 1)) 
                {
                    ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Circumference, point, point.ChildPoints[i], shrink, stretch, false));
                }
                CreationConstraintCircumference(point, point.ChildPoints[i], ref ConstraintList, shrink, stretch);
            }
        }

        private void CreationConstraintCircumference(PCHRuntimePoint PointA, PCHRuntimePoint PointB, ref List<PCHRuntimeConstraint> ConstraintList, float shrink, float stretch)
        {
            if (PointB == null || PointA == null) return;

            var childPointB = PointB.ChildPoints;
            if ((childPointB != null))
            {
                for (int i = 0; i < childPointB.Count; i++)
                {
                    var isRepetA = PCHSetting.isComputeStructuralVertical && (childPointB[i].depth == 1);
                    var isRepetB = PCHSetting.isComputeBendingVertical && (childPointB[i].depth == 2);
                    if (!(isRepetA || isRepetB))
                    {
                        ConstraintList.Add(new PCHRuntimeConstraint(ConstraintType.Circumference, PointA, childPointB[i], shrink, stretch, false));
                    }
                    CreationConstraintCircumference(PointA, childPointB[i], ref ConstraintList, shrink, stretch);
                }
            }
        }

        private static void sortByDistance(PCHRuntimePoint target, ref List<PCHRuntimePoint> List, bool isInverse)
        {
            if (List.Count < 2 || target == null) return;

            int fore = isInverse ? 1 : -1;
            List.Sort((point1, point2) =>
            {
                return (Vector3.Distance(point1.transform.position, target.transform.position) > Vector3.Distance(point2.transform.position, target.transform.position)) ? -fore : fore;
            });
        }
        private static float GetMaxDeep(PCHRuntimePoint point)
        {
            if (point == null)
            {
                return 0;
            }
            else if (point.ChildPoints == null || point.ChildPoints.Count == 0)
            {
                return point.pointRead.initialLocalPositionLength;
            }
            else
            {
                float max = 0;
                for (int i = 0; i < point.ChildPoints.Count; i++)
                {
                    max = Mathf.Max(max, GetMaxDeep(point.ChildPoints[i]));
                }
                if (point.isFixed)
                {
                    return max;
                }
                else
                {
                    return point.pointRead.initialLocalPositionLength + max;
                }

            }
        }

        #endregion
        #region public Func
        public void SetData(PCHPhysicsKernel dataPackage)
        {

            dataPackage.SetPointAndConstraintData(constraintList, pointReadList, pointReadWriteList, allPointTransforms);
        }
        public PCHPhysicsSetting GetPCHSetting()
        {
            return PCHSetting;
        }
        public void SetPCHSetting(PCHPhysicsSetting PCHPhysicsSetting)
        {
            this.PCHSetting = PCHPhysicsSetting;
        }

        public PCHRuntimeConstraint[] GetConstraint(ConstraintType constrianttype)
        {
            switch (constrianttype)
            {
                case ConstraintType.Structural_Vertical:
                    return constraintsStructuralVertical.ToArray();
                case ConstraintType.Structural_Horizontal:
                    return constraintsStructuralHorizontal.ToArray();
                case ConstraintType.Shear:
                    return constraintsShear.ToArray();
                case ConstraintType.Bending_Vertical:
                    return constraintsBendingVertical.ToArray();
                case ConstraintType.Bending_Horizontal:
                    return constraintsBendingHorizontal.ToArray();
                case ConstraintType.Circumference:
                    return constraintsCircumference.ToArray();
                default:
                    Debug.LogError("can not find the constraint");
                    return null;
            }
        }

        public bool CanMerge(PCHRuntimePoint point,PCHPhysicsSetting targetSetting)
        {

            bool b = point.transform.parent == RootTransform;
            b &=  keyWord == point. keyWord;
            b&= PCHSetting.Equals(targetSetting);
            return b;

        }

        public Bounds GetCurrentRangeBounds()
        {
            return new Bounds(RootTransform.position,Vector3.one* maxChainLength * 2);
        }
        #endregion


        #region Gizmo
        public void DrawGizmos(Mono.ColliderCollisionType colliderCollisionType)
        {

            foreach (var point in allPointList)
            {
                point.DrawGizmos();
            }
            if (PCHSetting.isComputeStructuralVertical)
            {
                DrawConstraint(constraintsStructuralVertical, colliderCollisionType);
            }
            if (PCHSetting.isComputeStructuralHorizontal)
            {
                DrawConstraint(constraintsStructuralHorizontal, colliderCollisionType);
            }
            if (PCHSetting.isComputeShear)
            {
                DrawConstraint(constraintsShear, colliderCollisionType);
            }
            if (PCHSetting.isComputeCircumference)
            {
                DrawConstraint(constraintsCircumference, colliderCollisionType);
            }
            if (PCHSetting.isComputeBendingHorizontal)
            {
                DrawConstraint(constraintsBendingHorizontal, colliderCollisionType);
            }
            if (PCHSetting.isComputeBendingVertical)
            {
                DrawConstraint(constraintsBendingVertical, colliderCollisionType);
            }
        }


        private void DrawConstraint(List<PCHRuntimeConstraint> constraints, Mono.ColliderCollisionType colliderCollisionType)
        {
            if (constraints == null) return;
            for (int i = 0; i < constraints.Count; i++)
            {
                constraints[i].OnDrawGizmos(colliderCollisionType == Mono.ColliderCollisionType.Constraint || colliderCollisionType == Mono.ColliderCollisionType.Both);
            }
        }


        #endregion
    }
}



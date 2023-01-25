using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using System;
namespace TK.Mono
{
    public class PCHRuntimePoint: MonoBehaviour
    {
        public MonoBehaviour Target => this;
        public PCHRuntimePoint Parent { get { return parent; }set { parent = value; } }
        public List<PCHRuntimePoint> ChildPoints { get { return childPoints; } } 
        public bool isFixed { get { return depth == 0; } }

        public bool isRoot { get { return depth == -1; } }

        [NonSerialized]
        public PointReadWrite pointReadWrite;
        [SerializeField]
        public float3 initialScale;
        [SerializeField]
        public PointRead pointRead;
        [SerializeField]
        private PCHRuntimePoint parent;
        [SerializeField]
        private List<PCHRuntimePoint> childPoints;
        public string keyWord;
        [SerializeField]
        public int depth;
        [SerializeField]
        public int index;
        [SerializeField]
        public bool allowCreateAllConstraint;
        [SerializeField]
        public float pointDepthRateMaxPointDepth;
        public static PCHRuntimePoint CreateRuntimePoint(Transform trans, int depth, string keyWord = null, bool isAllowComputeOtherConstraint = true)
        {
            PCHRuntimePoint point = trans.gameObject.AddComponent<PCHRuntimePoint>();
            point.keyWord = keyWord;
            point.depth = depth;
            point.pointRead = new PointRead();
            point.pointReadWrite = new PointReadWrite();
            point.allowCreateAllConstraint = isAllowComputeOtherConstraint;
            point.initialScale = trans.lossyScale;
            return point;
        }
        internal virtual void DrawGizmos()
        {
            Gizmos.color = allowCreateAllConstraint ?  Color.yellow: new Color(0.3f,1f,0.3f,1);
            if (pointRead.radius > 0.005f)
            {
                Matrix4x4 temp = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.FromToRotation(Vector3.up, pointRead.initialLocalPosition) * transform.rotation, transform.lossyScale/ initialScale);
                Gizmos.DrawWireSphere(Vector3.zero, pointRead.radius);
                Gizmos.matrix = temp;

            }
            else
            {
                Gizmos.DrawSphere(transform.position, 0.005f);
            }
        }
       public void SetDepth(int i )
        {
            depth = i;
        }

        public void AddChild(List<PCHRuntimePoint> childPoints)
        {
            for (int i = 0; i < childPoints?.Count; i++)
            {
                AddChild(childPoints[i]);
            }

        }
        public void AddChild(PCHRuntimePoint childPoint)
        {
            if (childPoint != null)
            {
                if (childPoints == null)
                {
                    childPoints = new List<PCHRuntimePoint>();
                }
                childPoints.Add(childPoint);
                childPoint.Parent = this;
            }


        }
    }


}

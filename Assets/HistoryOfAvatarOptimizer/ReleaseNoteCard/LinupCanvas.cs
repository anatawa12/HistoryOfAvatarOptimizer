using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HistoryOfAvatarOptimizer.ReleaseNoteCard
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class LinupCanvas : UdonSharpBehaviour
    {
        public float scale = 0.0005f;
        public int space = 20;
        public float resetInterval = 0.5f;
        public float width = 1.5f;

        void Start()
        {
            float lastZ = float.NegativeInfinity;
            float leftYPosition = 0;
            float rightYPosition = 0;

            var childCount = transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.localPosition.z > lastZ + resetInterval)
                {
                    leftYPosition = 0;
                    rightYPosition = 0;
                    lastZ = child.localPosition.z;
                }

                var left = leftYPosition <= rightYPosition;

                var canvas = (RectTransform)child.GetChild(0);
                var element = (RectTransform)canvas.GetChild(0);
                var cylinder = child.GetChild(1);
                var verticalCylinder = child.GetChild(2);

                var height = element.sizeDelta.y;
                var canvasSize = canvas.sizeDelta;
                canvasSize.y = height;
                canvas.sizeDelta = canvasSize;

                var canvasPosition = canvas.localPosition;
                canvasPosition.y += left ? leftYPosition : rightYPosition;
                canvasPosition.x = left ? -width : width;
                canvas.localPosition = canvasPosition;
                
                canvas.Rotate(Vector3.up, left ? 40 : 180 - 40);

                if (left)
                {
                    var position = cylinder.localPosition;
                    position.x = -position.x;
                    cylinder.localPosition = position;

                    position = verticalCylinder.localPosition;
                    position.x = -position.x;
                    verticalCylinder.localPosition = position;

                    verticalCylinder.localScale = new Vector3(1, 1 + leftYPosition, 1);
                }
                else
                {
                    verticalCylinder.localScale = new Vector3(1, 1 + rightYPosition, 1);
                }

                if (left) leftYPosition += (height + space) * scale;
                else rightYPosition += (height + space) * scale;
            }
        }
    }
}

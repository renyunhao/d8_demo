using UnityEngine;
using System;

namespace GameFramework
{
    public class ListViewItem : BindableMonoBehaviour
    {
        public Action<int, Vector2> Event_OnWrapContentItemSizeChanged;
        protected int index = -1;
        protected RectTransform rectTransform;
        protected Vector2 originSize;
        protected Vector2 size;
        protected bool sizeCacluated = false;
        /// <summary>
        /// GalleryMode下的Item距离视野中心的百分比
        /// -1相当于Item的位置在视野的左边缘
        /// 1相当于Item的位置在视野的右边缘
        /// 0相当于Item在视野中心位置
        /// </summary>
        protected float relativePosToCenter = 0;
        protected ListView listView;
        protected bool isCenterOn = false;

        public virtual int Index
        {
            get { return index; }
            set
            {
                index = value;
            }
        }

        public float RelativePosToCenter
        {
            get
            {
                return relativePosToCenter;
            }
            set
            {
                relativePosToCenter = value;
            }
        }

        /// <summary>
        /// 当前Size（多行多列的情况下不要使用）
        /// </summary>
        public Vector2 Size
        {
            get
            {
                //CheckSize();
                return size;
            }
            set
            {
                size = value;
                this.GetComponent<RectTransform>().sizeDelta = size;
                if (Event_OnWrapContentItemSizeChanged != null)
                {
                    Event_OnWrapContentItemSizeChanged(index, size);
                }
            }
        }

        public Vector2 Position
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = this.GetComponent<RectTransform>();
                }
                return rectTransform.anchoredPosition;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = this.GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        public ListView ListView => listView;

        public bool IsCenterOn => isCenterOn;

        protected virtual void Awake()
        {
            rectTransform = this.GetComponent<RectTransform>();
            originSize = rectTransform.sizeDelta;
            size = originSize;
            OnLoseCentered();
        }

        public void SetListView(ListView listView)
        {
            this.listView = listView;
        }

        public void Scale(float scale)
        {
            //CheckSize();
            this.transform.localScale = new Vector3(scale, scale, 1);
            this.size = originSize * scale;
        }

        /// <summary>
        /// 通用计算物体大小()
        /// </summary>
        /// <returns></returns>
        protected virtual void ComputeItemSize()
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(transform);
            size = bounds.size;
            originSize = bounds.size;
        }

        private void CheckSize()
        {
            if (sizeCacluated == false)
            {
                sizeCacluated = true;
                ComputeItemSize();
            }
        }

        public void SetSize(Vector2 size)
        {
            //CheckSize();
            float scale = size.x / originSize.x;
            this.transform.localScale = new Vector3(scale, scale, 1);
            this.size = size;
        }

        public virtual void OnCentered()
        {
            isCenterOn = true;
        }

        public virtual void OnLoseCentered()
        {
            isCenterOn = false;
        }
    }
}
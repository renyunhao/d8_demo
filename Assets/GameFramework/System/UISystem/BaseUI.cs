using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameFramework
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(UnityEngine.UI.CanvasScaler))]
    [RequireComponent(typeof(UnityEngine.UI.GraphicRaycaster))]

    public abstract class BaseUI : BindableMonoBehaviour
    {
        public Canvas UICanvas { get; set; }

        /// <summary>
        /// 如果需要获取UI GameObject的名字，使用这个属性，避免直接使用Object.name属性，会引起GC Alloc
        /// </summary>
        public string Name { get; set; }

        #region 派生类需重写的方法，不建议外部调用，由UIMgr调用

        public virtual void OnCreate()
        {
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnShow(ITuple tuple)
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void OnLanguageChange()
        {
        }

        #endregion

        public (string sortingLayer, int sortingOrder) GetSortingLayer()
        {
            Canvas canvas = this.GetComponent<Canvas>();
            return (canvas.sortingLayerName, canvas.sortingOrder);
        }
    }
}
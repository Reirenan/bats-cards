using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BatsSolitaire.Presentation
{
    public sealed class CardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<int> Swiped;
        private Vector2 start;
        public void OnBeginDrag(PointerEventData e) => start = e.position;
        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e)
        {
            var delta = e.position - start;
            if (Mathf.Abs(delta.x) > 40 * UiFactory.PixelDensity && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Swiped?.Invoke(delta.x < 0 ? 1 : -1);
        }
    }
}

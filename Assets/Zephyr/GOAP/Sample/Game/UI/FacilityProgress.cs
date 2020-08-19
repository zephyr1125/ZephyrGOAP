using UnityEngine;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class FacilityProgress : MonoBehaviour
    {
        public RectTransform BackgroundTransform, BarTransform;

        private Transform _transform;
        
        private float _fullWidth;

        private void OnEnable()
        {
            _transform = transform;
            _fullWidth = BackgroundTransform.rect.width-2;
        }

        /// <summary>
        /// 范围0-1
        /// </summary>
        /// <param name="progress"></param>
        public void SetProgress(float progress, Vector3 position)
        {
            var rect = BarTransform.rect;
            BarTransform.sizeDelta = new Vector2(_fullWidth*progress, rect.height);

            transform.position = position;
        }
    }
}
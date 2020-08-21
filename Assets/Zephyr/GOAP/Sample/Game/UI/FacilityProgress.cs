using UnityEngine;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class FacilityProgress : MonoBehaviour
    {
        public RectTransform backgroundTransform, barTransform;

        private Transform _transform;
        
        private float _fullWidth;

        private void OnEnable()
        {
            _transform = transform;
            _fullWidth = backgroundTransform.rect.width-2;
        }

        /// <summary>
        /// 范围0-1
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="position"></param>
        public void SetProgress(float progress, Vector3 position)
        {
            var rect = barTransform.rect;
            barTransform.sizeDelta = new Vector2(_fullWidth*progress, rect.height);

            _transform.position = position;

            if (progress <= 0.001 || progress >= 0.999)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }
    }
}
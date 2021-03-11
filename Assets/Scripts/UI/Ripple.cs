using UnityEngine;
using UnityEngine.UI;

namespace GliderBoy.UI
{
    public class Ripple : MonoBehaviour
    {

        #region Fields

        private float _speed;
        private float _maximumScale;
        private Color _toColor;
        private Image _image;

        #endregion



        #region Functions

        public void Initialise(float speed, float maximumScale, Color fromColor, Color toColor)
        {
            _image = GetComponent<Image>();

            _speed = speed;
            _maximumScale = maximumScale;
            _image.color = fromColor;
            _toColor = toColor;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            // Update the color and scale.
            _image.color = Color.Lerp(_image.color, _toColor, Time.deltaTime * _speed);
            transform.localScale =
                Vector3.Lerp(transform.localScale, Vector3.one * _maximumScale, Time.deltaTime * _speed);

            // If the scale is larger or equal to the predefined maximum scale, destroy the ripple.
            if (transform.localScale.x >= _maximumScale * 0.999f) Destroy(gameObject);
        }

        #endregion

    }
}
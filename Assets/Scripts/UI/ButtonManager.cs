using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GliderBoy.UI
{
    public class ButtonManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {

        #region Fields

        // ------------ Visible ------------

        [Header("Text")] [SerializeField] private string text = "Button";
        [SerializeField] private TMP_Text buttonText;

        [Header("Visual")] 
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color hoverColor = new Color(0.55f, 0.55f, 0.55f);
        [SerializeField] private Color clickColor = new Color(0.65f, 0.65f, 0.65f);

        [Header("Audio")] [SerializeField] private bool useHoverSound;
        [SerializeField] private AudioClip hoverSound;

        [SerializeField] private bool useClickSound;
        [SerializeField] private AudioClip clickSound;

        [SerializeField] private AudioSource soundSource;

        [Header("Ripple Effect")] [SerializeField]
        private bool useRipples = true;

        [SerializeField] private bool rippleFromCenter;
        [SerializeField] private Sprite rippleSprite;
        [SerializeField, Range(0.1f, 5.0f)] public float rippleSpeed = 2f;
        [SerializeField, Range(0.5f, 10.0f)] public float rippleMaximumScale = 5f;
        [SerializeField] private Color rippleFromColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color rippleToColor = new Color(1f, 1f, 1f, 0f);

        [Header("Scaling Effect")] [SerializeField]
        private bool useScaling = true;

        [SerializeField, Range(0.001f, 1.0f)] private float onHoverScaleSpeed = 0.15f;
        [SerializeField] private Vector2 onHoverScale = new Vector2(1.1f, 1.1f);
        [SerializeField, Range(0.001f, 1.0f)] private float onClickScaleSpeed = 0.05f;
        [SerializeField] private Vector2 onClickScale = new Vector2(0.9f, 0.9f);

        [Header("Events")] [SerializeField] private UnityEvent onPointerDown;
        [SerializeField] private UnityEvent onPointerUp;
        [SerializeField] private UnityEvent onPointerEnter;
        [SerializeField] private UnityEvent onPointerExit;

        // ------------ Private ------------

        private Vector2 _originalScale;
        private bool _isHovering;

        #endregion



        #region Private Functions

        /// <summary>
        /// Creates a ripple effect on the button.
        /// </summary>
        private void CreateRipple()
        {
            if (rippleSprite == null) return;

            // Create the ripple object and parent it to this game object.
            var ripple = new GameObject().AddComponent<RectTransform>();
            ripple.name = "Ripple";
            ripple.parent = buttonImage.transform;
            ripple.SetAsFirstSibling();
            ripple.localPosition =
                rippleFromCenter ? Vector3.zero : ripple.parent.InverseTransformPoint(Input.mousePosition);

            // Insert the ripple sprite.
            var rippleImage = ripple.gameObject.AddComponent<Image>();
            rippleImage.sprite = rippleSprite;

            // Initialise the ripple component.
            var rippleComponent = rippleImage.gameObject.AddComponent<Ripple>();
            rippleComponent.Initialise(rippleSpeed, rippleMaximumScale, rippleFromColor, rippleToColor);
        }

        /// <summary>
        /// Changes the overall scale of this object.
        /// </summary>
        /// <param name="duration">How long it takes for the scale to change.</param>
        /// <param name="endSize">The ending scale of this object, after the duration.</param>
        /// <param name="pointerUp">As the pointer up is the final element to be called, it will be called after all scaling.</param>
        private IEnumerator ChangeScale(float duration, Vector2 endSize, bool pointerUp = false)
        {
            var t = 0.0f;
            var startSize = transform.localScale;

            while (t < duration)
            {
                t += Time.deltaTime;
                var scale = Vector2.Lerp(startSize, endSize, t / duration);
                transform.localScale = new Vector3(scale.x, scale.y, 1);
                yield return null;
            }
            
            // Invoke all pointer up events. -Recommended that all *Actual* button events are placed in this.
            if(pointerUp) onPointerUp.Invoke();
        }

        #endregion



        #region Unity Methods

        private void Start()
        {
            // Store original scale.
            _originalScale = transform.localScale;

            // Prepare button for ripples.
            if (useRipples) buttonImage.gameObject.AddComponent<Mask>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Invoke all pointer down events.
            onPointerDown.Invoke();

            // If we aren't hovering or using ripples, return...
            if (!useRipples || !_isHovering) return;

            // Change the buttons color accordingly.
            buttonImage.color = clickColor;

            // If we're using ripples, create a ripple.
            if (useRipples) CreateRipple();

            // If we're using scaling, change the scale accordingly.
            if (useScaling) StartCoroutine(ChangeScale(onClickScaleSpeed, onClickScale));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // If no longer hovering over the button, the user has changed their mind.
            if (!_isHovering) return;
            
            // Play sound effect.
            if (soundSource != null) soundSource.PlayOneShot(clickSound);

            // Change button image, based on whether we're still hovering over button.
            buttonImage.color = _isHovering ? hoverColor : normalColor;

            // If we're using scaling, change the scale accordingly.
            if (useScaling)
                StartCoroutine(_isHovering
                    ? ChangeScale(onClickScaleSpeed, onHoverScale, true)
                    : ChangeScale(onClickScaleSpeed, _originalScale, true));
            else onPointerUp.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Play hover sound effect.
            if (soundSource != null && useHoverSound)
                soundSource.PlayOneShot(hoverSound);

            // Invoke all pointer enter events (when the mouse starts hovering over the buttons).
            onPointerEnter.Invoke();
            _isHovering = true;

            // Change the buttons color accordingly.
            buttonImage.color = hoverColor;

            // If we're using scaling, change the scale accordingly.
            if (useScaling) StartCoroutine(ChangeScale(onHoverScaleSpeed, onHoverScale));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Invoke all pointer exit events (When the mouse is no longer hovering over the button).
            onPointerExit.Invoke();
            _isHovering = false;

            // Change the buttons color accordingly.
            buttonImage.color = normalColor;

            // If we're using scaling, change the scale accordingly.
            if (useScaling) StartCoroutine(ChangeScale(onHoverScaleSpeed, _originalScale));
        }

        /// <summary>
        /// Called when any value within this script is changed in the inspector.
        /// </summary>
        private void OnValidate()
        {
            // Sync the TMP text with the string in this script.
            if (buttonText != null && buttonText.text != text)
                buttonText.text = text;

            // Sync the image color with the color in this script.
            if (buttonImage.color != normalColor)
                buttonImage.color = normalColor;

            // Create a sound source if any of the SFX are enabled.
            if (soundSource == null && (useClickSound || useHoverSound))
                soundSource = gameObject.AddComponent<AudioSource>();

            // Remove any edge issues by making the on click scales a range.
            if (onClickScale.x < transform.localScale.x)
                onClickScale.x = transform.localScale.x;
            if (onClickScale.y < transform.localScale.y)
                onClickScale.y = transform.localScale.y;
        }

        #endregion

    }
}

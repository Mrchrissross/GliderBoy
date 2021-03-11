using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GliderBoy.UI
{
    public class UIPopup : MonoBehaviour
    {

        #region Properties

        private RectTransform RectTransform
        {
            get
            {
                if(_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }
        private RectTransform _rectTransform;

        #endregion
        
        
        
        #region Fields

        [SerializeField] private Vector2 showPosition;
        [SerializeField] private Vector2 hidePosition;

        [Space]
        [SerializeField] private bool hide;
        [SerializeField] private float speed = 1.0f;

        [Space]
        public UnityEvent OnShowEnter;
        public UnityEvent OnShowExit;
        
        [Space]
        public UnityEvent OnHideEnter;
        public UnityEvent OnHideExit;
        
        private float _t;
    
        #endregion


        
        #region Public Functions

        /// <summary>
        /// Hides or shows the popup.
        /// </summary>

        public void Popup()
        {
            if(!gameObject.activeSelf) gameObject.SetActive(true);
            hide = !hide;
            
            if(hide) OnHideEnter.Invoke();
            else OnShowEnter.Invoke();

            StartCoroutine(PerformPopup(speed, hide ? hidePosition : showPosition));
        }
        
        /// <summary>
        /// Resets the object to its shown position.
        /// </summary>
        
        public void ResetToShow()
        {
            hide = false;
            RectTransform.anchoredPosition = showPosition;
        }
        
        /// <summary>
        /// Resets the object to its hidden position.
        /// </summary>
        
        public void ResetToHide()
        {
            hide = true;
            RectTransform.anchoredPosition = hidePosition;
        }

        #endregion
        
        
        
        #region Private Functions
        
        /// <summary>
        /// Performs the popup.
        /// </summary>
        
        private IEnumerator PerformPopup(float duration, Vector2 targetPosition)
        {
            var t = 0.0f;
            var startPosition = RectTransform.anchoredPosition;
            
            while (t < duration)
            {
                t += Time.deltaTime;

                RectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t / duration);
                yield return null;
            }
            
            if(hide) OnHideExit.Invoke();
            else OnShowExit.Invoke();
        }

        #if UNITY_EDITOR

        [ContextMenu("Display Show Position")]
        private void DisplayShowPosition() => RectTransform.anchoredPosition = showPosition;
        
        [ContextMenu("Display Hide Position")]
        private void DisplayHidePosition() => RectTransform.anchoredPosition = hidePosition;
        
        [ContextMenu("Set Show Position")]
        private void SetShowPosition() => showPosition = RectTransform.anchoredPosition;
        
        [ContextMenu("Set Hide Position")]
        private void SetHidePosition() => hidePosition = RectTransform.anchoredPosition;

        #endif

        #endregion

    }
}

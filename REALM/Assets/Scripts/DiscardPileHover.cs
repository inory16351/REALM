using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm
{
    public class DiscardPileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hover Settings")]
        [SerializeField] private float collapsedSpacing = -85f;
        [SerializeField] private float expandedSpacing = 8f;
        [SerializeField] private float animSpeed = 12f;

        private HorizontalLayoutGroup _layoutGroup;
        private bool _isHovered = false;
        private bool _isPinned = false;
        private float _currentSpacing;
        private Coroutine _animRoutine;

        private void Awake()
        {
            _layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (_layoutGroup == null)
            {
                _layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
                _layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                _layoutGroup.childControlWidth = false;
                _layoutGroup.childControlHeight = false;
                _layoutGroup.childForceExpandWidth = false;
                _layoutGroup.childForceExpandHeight = false;
            }

            _currentSpacing = collapsedSpacing;
            _layoutGroup.spacing = _currentSpacing;
            ApplyStackRotation(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            AnimateTo(expandedSpacing, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isPinned)
            {
                AnimateTo(collapsedSpacing, false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _isPinned = !_isPinned;
            AnimateTo(_isPinned || _isHovered ? expandedSpacing : collapsedSpacing, _isPinned || _isHovered);
        }

        public void RefreshStack()
        {
            if (_layoutGroup != null)
            {
                _layoutGroup.spacing = (_isHovered || _isPinned) ? expandedSpacing : collapsedSpacing;
            }
            ApplyStackRotation(_isHovered || _isPinned);
        }

        private void AnimateTo(float targetSpacing, bool expanded)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(AnimateSpacingRoutine(targetSpacing, expanded));
        }

        private IEnumerator AnimateSpacingRoutine(float targetSpacing, bool expanded)
        {
            while (Mathf.Abs(_layoutGroup.spacing - targetSpacing) > 0.5f)
            {
                _currentSpacing = Mathf.Lerp(_layoutGroup.spacing, targetSpacing, Time.deltaTime * animSpeed);
                _layoutGroup.spacing = _currentSpacing;
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                yield return null;
            }

            _layoutGroup.spacing = targetSpacing;
            ApplyStackRotation(expanded);
            _animRoutine = null;
        }

        private void ApplyStackRotation(bool expanded)
        {
            int count = transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null || child.name == "Badge") continue;

                if (expanded)
                {
                    child.localRotation = Quaternion.identity;
                }
                else
                {
                    float zRot = 0f;
                    if (i == 1) zRot = -1.2f;
                    else if (i == 2) zRot = 2.5f;
                    else if (i == 3) zRot = 5.0f;
                    else zRot = (i % 2 == 0) ? 3.0f : -2.0f;

                    child.localRotation = Quaternion.Euler(0, 0, zRot);
                }
            }
        }
    }
}

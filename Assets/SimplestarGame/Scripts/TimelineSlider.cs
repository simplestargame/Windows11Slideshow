using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SimplestarGame
{
    public class TimelineSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] Slider sliderTimeline;
        void Start()
        {
            this.sliderTimeline.onValueChanged.AddListener(this.OnSliderValueChanged);
        }

        void Update()
        {
            if (!this.isSeeking)
            {
                this.sliderTimeline.SetValueWithoutNotify((float)this.videoPlayer.time);
            }
        }

        public void OnSliderValueChanged(float value)
        {
            this.videoPlayer.time = value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            this.isSeeking = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            this.isSeeking = false;
        }

        bool isSeeking = false;
    }
}
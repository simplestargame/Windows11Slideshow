using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SimplestarGame
{
    public class TimelineSlider : MonoBehaviour
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

        public void OnPointerDown()
        {
            this.isSeeking = true;
        }

        public void OnPointerUp()
        {
            this.isSeeking = false;
        }

        bool isSeeking = false;
    }
}
using UnityEngine;

namespace SimplestarGame
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI text;
        void Update()
        {
            if (null == this.text)
            {
                return;
            }
            this.deltaTime += (Time.unscaledDeltaTime - this.deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            this.text.text = "FPS: " + fps.ToString("00");
        }

        float deltaTime = 0.0f;
    }
}
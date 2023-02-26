using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SimplestarGame
{
    public class Slideshow : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_InputField folderPath;
        [SerializeField] TMPro.TMP_InputField pageTime;
        [SerializeField] TMPro.TextMeshProUGUI fullScreenText;
        [SerializeField] RawImage rawImage;
        [SerializeField] RawImage rawImageBack;
        [SerializeField] Button startButton;
        [SerializeField] Button stopButton;
        [SerializeField] Button shaffleButton;
        [SerializeField] Button timeDownButton;
        [SerializeField] Button timeUpButton;
        [SerializeField] Button windowFullscreenButton;
        [SerializeField] Toggle toggleUseAudio;

        void Start()
        {
            if (PlayerPrefs.HasKey(Slideshow.FOLDER_PATH))
            {
                this.folderPath.text = PlayerPrefs.GetString(Slideshow.FOLDER_PATH);
            }
            else
            {
                this.folderPath.text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            this.windowFullscreenButton.onClick.AddListener(this.OnWindowFullscreen);
            this.startButton.onClick.AddListener(this.OnStart);
            this.stopButton.onClick.AddListener(this.OnStop);
            this.shaffleButton.onClick.AddListener(this.OnShaffle);
            this.timeDownButton.onClick.AddListener(this.OnTimeDown);
            this.timeUpButton.onClick.AddListener(this.OnTimeUp);
            this.toggleUseAudio.onValueChanged.AddListener(this.OnUseAudio);

            this.audio = this.GetComponent<AudioSource>();
            this.audio.clip = Microphone.Start(null, true, 999, 44100);
            this.audio.loop = true;
            // this.audio.volume = 0;
            while (!(Microphone.GetPosition(null) > 0)) { }
            this.audio.Play();
        }

        void OnWindowFullscreen()
        {
            bool nextFullScreen = !Screen.fullScreen;
            Screen.SetResolution(Screen.height * 16 / 9, Screen.height, false);
            Screen.fullScreen = nextFullScreen;
            this.fullScreenText.text = nextFullScreen ? "Windowed" : "FullScreen";
        }

        void OnStop()
        {
            this.isPlay = !this.isPlay;
        }

        void OnStart()
        {
            if (null != this.coroutine)
            {
                StopCoroutine(this.coroutine);
                this.coroutine = null;
            }
            this.coroutine = StartCoroutine(this.CoChangeImage());
        }

        void OnDestroy()
        {
            if (null != this.coroutine)
            {
                StopCoroutine(this.coroutine);
            }
        }

        static void Shuffle(int[] array)
        {
            System.Random random = new System.Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        IEnumerator CoChangeImage()
        {
            this.isPlay = true;
            this.ShowUI(false);
            Encoding ascii = Encoding.ASCII;
            Encoding unicode = Encoding.Unicode;

            PlayerPrefs.SetString(Slideshow.FOLDER_PATH, this.folderPath.text);
            PlayerPrefs.Save();

            byte[] unicodeBytes = unicode.GetBytes(this.folderPath.text);
            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);
            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            var di = new DirectoryInfo(this.folderPath.text);
            this.files.Clear();
            this.files.AddRange(di.GetFiles("*.jpg", SearchOption.TopDirectoryOnly));
            this.files.AddRange(di.GetFiles("*.png", SearchOption.TopDirectoryOnly));
            this.indices = new int[this.files.Count];
            for (int i = 0; i < this.indices.Length; i++)
            {
                this.indices[i] = i;
            }
            this.index = 0;
            this.ShowNextImage();
            while (true)
            {

                if (float.TryParse(this.pageTime.text, out float waitTime))
                {
                    waitTime = Mathf.Clamp(waitTime, 1f, 180f);
                }
                else
                {
                    waitTime = 5f;
                }
                yield return new WaitForSeconds(waitTime);
                if (this.isPlay)
                {
                    this.index++;
                    this.ShowNextImage();
                }
            }
        }

        void OnTimeDown()
        {
            if (float.TryParse(this.pageTime.text, out float waitTime))
            {
                this.pageTime.text = Mathf.Clamp(waitTime - 1, 0.1f, 180f).ToString();
            }
        }

        void OnTimeUp()
        {
            if (float.TryParse(this.pageTime.text, out float waitTime))
            {
                this.pageTime.text = Mathf.Clamp(waitTime + 1, 0.1f, 180f).ToString();
            }
        }

        void OnShaffle()
        {
            if (null == this.indices)
            {
                return;
            }
            for (int i = 0; i < this.indices.Length; i++)
            {
                this.indices[i] = i;
            }
            Shuffle(this.indices);
        }

        void ShowNextImage()
        {
            if (null == this.indices)
            {
                return;
            }
            if (this.files.Count <= this.index)
            {
                this.index = 0;
            }
            else if (0 > this.index)
            {
                this.index = this.files.Count - 1;
            }
            int showIndex = this.indices[this.index];
            var file = this.files[showIndex];
            var texture = new Texture2D(100, 100);
            texture.requestedMipmapLevel = 0;
            texture.LoadImage(File.ReadAllBytes(file.FullName));
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Destroy(this.rawImage.texture);
            this.rawImage.texture = texture;
            this.rawImageBack.texture = texture;
            this.rawImage.SetNativeSize();
            var originalSize = this.rawImage.rectTransform.sizeDelta;
            this.rawImage.rectTransform.sizeDelta = new Vector2(originalSize.x * Screen.height / originalSize.y, Screen.height);
            this.rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            this.rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            this.rawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void OnUseAudio(bool useAudio)
        {
            this.useAudio = useAudio;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                this.index--;
                this.ShowNextImage();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                this.index++;
                this.ShowNextImage();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                this.isPlay = false;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                this.isPlay = true;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                this.isPlay = !this.isPlay;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Screen.fullScreen)
                {
                    this.OnWindowFullscreen();
                }
            }
            var pos = Input.mousePosition;
            var buttonPos = this.windowFullscreenButton.transform.position;
            if (100 > Vector3.Distance(pos, buttonPos))
            {
                if (null != this.coroutine2)
                {
                    StopCoroutine(this.coroutine2);
                }
                this.coroutine2 = StartCoroutine(this.CoHideUI());
            }

            this.audio.GetSpectrumData(this.samples, 0, FFTWindow.BlackmanHarris);
            var deltaFreq = AudioSettings.outputSampleRate / this.resolution;
            float low = 0f;
            for (var i = 0; i < this.resolution; ++i)
            {
                var freq = deltaFreq * i;
                if (freq <= this.lowFreqThreshold) low += this.samples[i];
            }

            this.totalLow += low;
            this.frameCount++;
            if (this.frameCount == 30)
            {
                this.averageLow = this.totalLow / this.frameCount;
                this.frameCount = 0;
                this.totalLow = 0f;
            }

            this.smooth = Mathf.SmoothDamp(this.smooth, 1 + Mathf.Clamp(low, -0.1f, 0.1f), ref this.smoothVelocity, Time.deltaTime * 5f);
            if (this.useAudio)
            {
                this.rawImage.transform.localScale = Vector3.one * this.smooth;
                if (!this.isPlay && this.coolTime < 0 && this.averageLow * 1.5f < low)
                {
                    this.index++;
                    this.ShowNextImage();
                    if (float.TryParse(this.pageTime.text, out float waitTime))
                    {
                        waitTime = Mathf.Clamp(waitTime, 1f, 180f);
                    }
                    else
                    {
                        waitTime = 5f;
                    }
                    this.coolTime = waitTime;
                    this.smoothVelocity = 0;
                    this.smooth = 1;
                }
                this.coolTime -= Time.deltaTime;
            }
        }

        IEnumerator CoHideUI()
        {
            this.ShowUI(true);
            yield return new WaitForSeconds(5f);
            this.ShowUI(false);
        }

        void ShowUI(bool show)
        {
            this.startButton.gameObject.SetActive(show);
            this.stopButton.gameObject.SetActive(show);
            this.folderPath.gameObject.SetActive(show);
            this.shaffleButton.gameObject.SetActive(show);
            this.timeUpButton.gameObject.SetActive(show);
            this.timeDownButton.gameObject.SetActive(show);
            this.pageTime.gameObject.SetActive(show);
            this.windowFullscreenButton.gameObject.SetActive(show);
            this.toggleUseAudio.gameObject.SetActive(show);
        }

        const string FOLDER_PATH = "folderPath";
        Coroutine coroutine = null;
        Coroutine coroutine2 = null;
        int[] indices;
        bool isPlay = false;
        int index = 0;
        List<FileInfo> files = new List<FileInfo>();
        new AudioSource audio;
        float[] samples = new float[1024];

        bool useAudio = true;
        float coolTime = 0;
        float smooth = 1f;
        float smoothVelocity = 0;
        int resolution = 1024;
        float lowFreqThreshold = 3000;
        int frameCount = 0;
        float totalLow = 0;
        float averageLow = 0;
    }
}
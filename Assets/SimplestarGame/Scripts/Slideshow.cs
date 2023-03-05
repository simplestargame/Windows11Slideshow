using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
        [SerializeField] Toggle toggleShaffle;
        [SerializeField] Button timeDownButton;
        [SerializeField] Button timeUpButton;
        [SerializeField] Button windowFullscreenButton;
        [SerializeField] Toggle toggleUseAudio;
        [SerializeField] Slider sliderVolume;
        [SerializeField] Toggle toggleTopDirectoryOnly;
        [SerializeField] VideoPlayer videoPlayer;

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
            this.toggleShaffle.onValueChanged.AddListener(this.OnShaffle);
            this.timeDownButton.onClick.AddListener(this.OnTimeDown);
            this.timeUpButton.onClick.AddListener(this.OnTimeUp);
            this.toggleUseAudio.onValueChanged.AddListener(this.OnUseAudio);
            this.toggleTopDirectoryOnly.onValueChanged.AddListener(this.OnTopDirectoryOnly);
            this.sliderVolume.onValueChanged.AddListener(this.OnChangeVolume);
            if (PlayerPrefs.HasKey(Slideshow.AUDIO_VOLUME))
            {
                this.sliderVolume.value = PlayerPrefs.GetFloat(Slideshow.AUDIO_VOLUME);
            }
            if (PlayerPrefs.HasKey(Slideshow.SHAFFLE))
            {
                this.shaffle = 1 == PlayerPrefs.GetInt(Slideshow.SHAFFLE);
                this.toggleShaffle.isOn = this.shaffle;
            }
            if (PlayerPrefs.HasKey(Slideshow.TOP_DIR_ONLY))
            {
                this.toggleTopDirectoryOnly.isOn = 1 == PlayerPrefs.GetInt(Slideshow.TOP_DIR_ONLY);
            }
            if (PlayerPrefs.HasKey(Slideshow.USE_AUDIO))
            {
                this.toggleUseAudio.isOn = 1 == PlayerPrefs.GetInt(Slideshow.USE_AUDIO);
            }

            this.audio = this.GetComponent<AudioSource>();
            this.audio.clip = Microphone.Start(null, true, 999, 44100);
            this.audio.loop = true;
            while (!(Microphone.GetPosition(null) > 0)) { }
            this.audio.Play();

            this.videoPlayer.source = VideoSource.Url;
            this.videoPlayer.prepareCompleted += this.OnVideoPrepareCompleted;
            this.videoPlayer.loopPointReached += OnLoopPointReached;
        }

        private void OnChangeVolume(float volume)
        {
            PlayerPrefs.SetFloat(Slideshow.AUDIO_VOLUME, volume);
            this.videoPlayer.SetDirectAudioVolume(0, volume);
        }

        void OnLoopPointReached(VideoPlayer source)
        {
            if (this.isPlay)
            {
                this.index++;
                this.ShowNextImage();
            }
        }

        void OnVideoPrepareCompleted(VideoPlayer source)
        {
            float scale = Screen.height / (float)source.texture.height;
            var texture = new RenderTexture(Mathf.RoundToInt(source.texture.width * scale), Mathf.RoundToInt(source.texture.height * scale), 4);
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            this.videoPlayer.targetTexture = texture;
            Destroy(this.rawImage.texture);
            this.rawImage.texture = texture;
            this.rawImageBack.texture = texture;
            this.rawImage.SetNativeSize();
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
            this.isPlay = false;
            if (this.mode == Mode.Video)
            {
                this.videoPlayer.Pause();
            }
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

            string[] imageExts = { ".jpg", ".png", ".gif", ".bmp" };
            foreach (var imageExt in imageExts)
            {
                this.files.AddRange(di.GetFiles("*" + imageExt, this.searchOption));
            }
            string[] videoExts = { ".mp4", ".avi", ".mov", ".wmv" };
            foreach (var videoExt in videoExts)
            {
                this.files.AddRange(di.GetFiles("*" + videoExt, this.searchOption));
            }
            
            this.indices = new int[this.files.Count];
            for (int i = 0; i < this.indices.Length; i++)
            {
                this.indices[i] = i;
            }
            if (this.shaffle)
            {
                this.ShaffleIndices();
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
                    if (this.mode == Mode.Image)
                    {
                        this.index++;
                        this.ShowNextImage();
                    }
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

        void ShowNextImage()
        {
            if (null == this.indices || 0 == this.files.Count)
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

            string ext = file.Extension;

            string[] videoExts = { ".mp4", ".avi", ".mov", ".wmv" };
            string[] imageExts = { ".jpg", ".png", ".gif", ".bmp" };

            
            if (videoExts.Contains(ext))
            {
                this.mode = Mode.Video;
                this.videoPlayer.url = file.FullName;
                this.videoPlayer.Prepare();
                this.videoPlayer.SetDirectAudioVolume(0, this.useAudio ? 1 : 0);
                this.videoPlayer.Play();
                this.rawImage.transform.localScale = Vector3.one;
            }
            else if (imageExts.Contains(ext))
            {
                this.mode = Mode.Image;
                var texture = new Texture2D(100, 100);
                texture.requestedMipmapLevel = 0;
                texture.LoadImage(File.ReadAllBytes(file.FullName));
                texture.filterMode = FilterMode.Trilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                Destroy(this.rawImage.texture);
                this.rawImage.texture = texture;
                this.rawImageBack.texture = texture;
                this.rawImage.SetNativeSize();
            }
            var originalSize = this.rawImage.rectTransform.sizeDelta;
            this.rawImage.rectTransform.sizeDelta = new Vector2(originalSize.x * Screen.height / originalSize.y, Screen.height);
            this.rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            this.rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            this.rawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void OnUseAudio(bool useAudio)
        {
            this.useAudio = useAudio;
            PlayerPrefs.SetInt(Slideshow.USE_AUDIO, useAudio ? 1 : 0);
        }

        void OnTopDirectoryOnly(bool topDirectoryOnly)
        {
            this.searchOption = topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            PlayerPrefs.SetInt(Slideshow.TOP_DIR_ONLY, topDirectoryOnly ? 1 : 0);
        }

        void OnShaffle(bool shaffle)
        {
            this.shaffle = shaffle;
            PlayerPrefs.SetInt(Slideshow.SHAFFLE, shaffle ? 1 : 0);
            if (this.shaffle)
            {
                this.ShaffleIndices();
            }
        }

        void ShaffleIndices()
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
                switch (this.mode)
                {
                    case Mode.Image:
                        this.isPlay = false;
                        break;
                    case Mode.Video:
                        if (!this.videoPlayer.isPlaying)
                        {
                            this.videoPlayer.Play();
                        }
                        break;
                }
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                switch (this.mode)
                {
                    case Mode.Image:
                        this.isPlay = true;
                        break;
                    case Mode.Video:
                        if (this.videoPlayer.isPlaying)
                        {
                            this.videoPlayer.Pause();
                        }
                        break;
                }
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                switch (this.mode)
                {
                    case Mode.Image:
                        this.isPlay = !this.isPlay;
                        break;
                    case Mode.Video:
                        if (this.videoPlayer.isPlaying)
                        {
                            this.videoPlayer.Pause();
                        }
                        else
                        {
                            this.videoPlayer.Play();
                        }
                        break;
                }
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

            if(this.mode == Mode.Image)
            {
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
            this.toggleShaffle.gameObject.SetActive(show);
            this.timeUpButton.gameObject.SetActive(show);
            this.timeDownButton.gameObject.SetActive(show);
            this.pageTime.gameObject.SetActive(show);
            this.windowFullscreenButton.gameObject.SetActive(show);
            this.toggleUseAudio.gameObject.SetActive(show);
            this.toggleTopDirectoryOnly.gameObject.SetActive(show);
            this.sliderVolume.gameObject.transform.parent.gameObject.SetActive(show);
        }

        const string FOLDER_PATH = "folderPath";
        const string AUDIO_VOLUME = "audioVolume";
        const string USE_AUDIO = "useAudio";
        const string SHAFFLE = "shaffle";
        const string TOP_DIR_ONLY = "topDirOnly";
        Coroutine coroutine = null;
        Coroutine coroutine2 = null;
        int[] indices;
        bool isPlay = false;
        int index = 0;
        List<FileInfo> files = new List<FileInfo>();
        new AudioSource audio;
        float[] samples = new float[1024];
        enum Mode
        {
            Image,
            Video,
            Max
        }
        Mode mode = Mode.Image;
        SearchOption searchOption = SearchOption.TopDirectoryOnly;
        bool useAudio = true;
        bool shaffle = false;
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
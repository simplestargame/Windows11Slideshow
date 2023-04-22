using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
        [SerializeField] TMPro.TextMeshProUGUI fpsText;
        [SerializeField] Button prevButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button pauseButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button skipButton;
        [SerializeField] Button backButton;

        void Start()
        {
            Application.targetFrameRate = 60;
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
            this.prevButton.onClick.AddListener(this.OnPrev);
            this.nextButton.onClick.AddListener(this.OnNext);
            this.pauseButton.onClick.AddListener(this.OnPause);
            this.quitButton.onClick.AddListener(this.OnQuit);
            this.skipButton.onClick.AddListener(this.OnSkip);
            this.backButton.onClick.AddListener(this.OnBack);
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

            this.videoPlayer.skipOnDrop = false;
            this.videoPlayer.source = VideoSource.Url;
            this.videoPlayer.prepareCompleted += this.OnVideoPrepareCompleted;
            this.videoPlayer.loopPointReached += OnLoopPointReached;
        }

        void OnBack()
        {
            this.videoPlayerTime = this.videoPlayer.time;
            this.videoPlayerTime = Mathf.Max((float)(this.videoPlayerTime - 10f), 0f);
            this.videoPlayer.time = this.videoPlayerTime;
        }

        void OnSkip()
        {
            this.videoPlayerTime = this.videoPlayer.time;
            this.videoPlayerTime = Mathf.Min((float)(this.videoPlayerTime + 10f), (float)videoPlayer.length);
            this.videoPlayer.time = this.videoPlayerTime;
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
            Application.Quit();
        }

        void OnPause()
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
                        this.videoPlayerTime = this.videoPlayer.time;
                        this.pauseCoolDown = 5f;
                    }
                    else
                    {
                        this.videoPlayer.Play();
                        this.pauseCoolDown = 5f;
                    }
                    break;
            }
        }

        void OnNext()
        {
            switch (this.mode)
            {
                case Mode.Video:
                    if (this.videoPlayer.isPaused)
                    {
                        this.videoPlayerTime = Mathf.Min((float)(this.videoPlayerTime + 0.17f), (float)videoPlayer.length);
                        this.videoPlayer.time = this.videoPlayerTime;
                        return;
                    }
                    break;
            }

            if (0 > this.pauseCoolDown)
            {
                this.index++;
                this.ShowNextImage();
            }
            else
            {
                this.videoPlayer.Pause();
            }
        }

        void OnPrev()
        {
            switch (this.mode)
            {
                case Mode.Video:
                    if (this.videoPlayer.isPaused)
                    {
                        this.videoPlayerTime = Mathf.Max((float)(this.videoPlayerTime - 0.17f), 0f);
                        this.videoPlayer.time = this.videoPlayerTime;
                        return;
                    }
                    break;
            }
            if (0 > this.pauseCoolDown)
            {
                this.index--;
                this.ShowNextImage();
            }
            else
            {
                this.videoPlayer.Pause();
            }
        }

        void OnChangeVolume(float volume)
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
            var texture = new RenderTexture(Mathf.RoundToInt(source.texture.width * scale), Mathf.RoundToInt(source.texture.height * scale), 3);
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
            this.files.AddRange(di.GetFiles("*.lnk", this.searchOption));

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

            string targetPath = file.FullName;
            if (".lnk" == ext)
            {
                targetPath = GetTargetPath(file.FullName);
                ext = Path.GetExtension(targetPath);
            }

            string[] videoExts = { ".mp4", ".avi", ".mov", ".wmv" };
            string[] imageExts = { ".jpg", ".png", ".gif", ".bmp" };

            if (videoExts.Contains(ext))
            {
                this.mode = Mode.Video;
                this.videoPlayer.url = targetPath;
                this.videoPlayer.Prepare();
                this.videoPlayer.Play();
                this.rawImage.transform.localScale = Vector3.one;
               
            }
            else if (imageExts.Contains(ext))
            {
                this.mode = Mode.Image;
                var texture = new Texture2D(100, 100);
                texture.requestedMipmapLevel = 0;
                texture.LoadImage(File.ReadAllBytes(targetPath));
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
            this.pauseCoolDown -= Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                switch (this.mode)
                {
                    case Mode.Video:
                        if (this.videoPlayer.isPaused)
                        {
                            this.videoPlayerTime = Mathf.Max((float)(this.videoPlayerTime - 0.017f), 0f);
                            this.videoPlayer.time = this.videoPlayerTime;
                            return;
                        }
                        break;
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (0 > this.pauseCoolDown)
                {
                    this.index--;
                    this.ShowNextImage();
                }
                else
                {
                    this.videoPlayer.Pause();
                }
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                switch (this.mode)
                {
                    case Mode.Video:
                        if (this.videoPlayer.isPaused)
                        {
                            this.videoPlayerTime = Mathf.Min((float)(this.videoPlayerTime + 0.017f), (float)videoPlayer.length);
                            this.videoPlayer.time = this.videoPlayerTime;
                            return;
                        }
                        break;
                }  
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (0 > this.pauseCoolDown)
                {
                    this.index++;
                    this.ShowNextImage();
                }
                else
                {
                    this.videoPlayer.Pause();
                }
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
                            this.pauseCoolDown = 5f;
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
                            this.videoPlayerTime = this.videoPlayer.time;
                            this.pauseCoolDown = 5f;
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
                            this.videoPlayerTime = this.videoPlayer.time;
                            this.pauseCoolDown = 5f;
                        }
                        else
                        {
                            this.videoPlayer.Play();
                            this.pauseCoolDown = 5f;
                        }
                        break;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                this.OnWindowFullscreen();
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                this.videoPlayerTime = Mathf.Max((float)(this.videoPlayer.time - 5f), 0f);
                this.videoPlayer.time = this.videoPlayerTime;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                this.videoPlayerTime = Mathf.Min((float)(this.videoPlayer.time + 5f), (float)videoPlayer.length);
                this.videoPlayer.time = this.videoPlayerTime;
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                this.OnStart();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                this.OnStop();
            }
            float headDistance = this.folderPath.transform.position.y - Input.mousePosition.y;
            if (100 > Mathf.Abs(headDistance) && 0 < Input.mousePosition.x && Input.mousePosition.x < Screen.width)
            {
                if (null != this.coroutine2)
                {
                    StopCoroutine(this.coroutine2);
                }
                this.coroutine2 = StartCoroutine(this.CoHideUI());
            }
            float prevDistance = Vector3.Distance(this.prevButton.transform.position, Input.mousePosition);
            if (100 > prevDistance)
            {
                if (null != this.coroutine3)
                {
                    StopCoroutine(this.coroutine3);
                }
                this.coroutine3 = StartCoroutine(this.CoHidePrevUI());
            }
            float nextDistance = Vector3.Distance(this.nextButton.transform.position, Input.mousePosition);
            if (100 > nextDistance)
            {
                if (null != this.coroutine4)
                {
                    StopCoroutine(this.coroutine4);
                }
                this.coroutine4 = StartCoroutine(this.CoHideNextUI());
            }
            float pauseDistance = Vector3.Distance(this.pauseButton.transform.position, Input.mousePosition);
            if (100 > pauseDistance)
            {
                if (null != this.coroutine5)
                {
                    StopCoroutine(this.coroutine5);
                }
                this.coroutine5 = StartCoroutine(this.CoHidePauseUI());
            }
            float skipDistance = Vector3.Distance(this.skipButton.transform.position, Input.mousePosition);
            if (100 > skipDistance)
            {
                if (null != this.coroutine6)
                {
                    StopCoroutine(this.coroutine6);
                }
                this.coroutine6 = StartCoroutine(this.CoHideSkipUI());
            }
            float backDistance = Vector3.Distance(this.backButton.transform.position, Input.mousePosition);
            if (100 > backDistance)
            {
                if (null != this.coroutine7)
                {
                    StopCoroutine(this.coroutine7);
                }
                this.coroutine7 = StartCoroutine(this.CoHideBackUI());
            }

            if (this.mode == Mode.Image)
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
            yield return new WaitForSeconds(2f);
            this.ShowUI(false);
        }

        IEnumerator CoHidePrevUI()
        {
            this.prevButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            this.prevButton.gameObject.SetActive(false);
        }

        IEnumerator CoHideNextUI()
        {
            this.nextButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            this.nextButton.gameObject.SetActive(false);
        }

        IEnumerator CoHidePauseUI()
        {
            this.pauseButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            this.pauseButton.gameObject.SetActive(false);
        }

        IEnumerator CoHideSkipUI()
        {
            this.skipButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            this.skipButton.gameObject.SetActive(false);
        }

        IEnumerator CoHideBackUI()
        {
            this.backButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            this.backButton.gameObject.SetActive(false);
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
            this.fpsText.gameObject.transform.parent.gameObject.SetActive(show);
            this.quitButton.gameObject.SetActive(show);
        }

        const string FOLDER_PATH = "folderPath";
        const string AUDIO_VOLUME = "audioVolume";
        const string USE_AUDIO = "useAudio";
        const string SHAFFLE = "shaffle";
        const string TOP_DIR_ONLY = "topDirOnly";
        Coroutine coroutine = null;
        Coroutine coroutine2 = null;
        Coroutine coroutine3 = null;
        Coroutine coroutine4 = null;
        Coroutine coroutine5 = null;
        Coroutine coroutine6 = null;
        Coroutine coroutine7 = null;
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
        double videoPlayerTime;
        float pauseCoolDown;

        // ショートカットファイルのリンク先のパスを取得するメソッド
        public static string GetTargetPath(string lnkPath)
        {
            // ショートカットファイルを開く
            var shellLink = (IShellLink)new ShellLink();
            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(lnkPath, 0);

            // リンク先のパスを取得する
            var targetPath = new StringBuilder(260);
            shellLink.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);

            // リンク先のパスを返す
            return targetPath.ToString();
        }

        // IShellLinkインターフェイスの定義
        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        // ShellLinkクラスの定義
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        class ShellLink { }
    }
}
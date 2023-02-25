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
            this.ShowUI(false);
        }

        void OnStart()
        {
            if (null != this.coroutine)
            {
                StopCoroutine(this.coroutine);
                this.coroutine = null;
            }
            this.coroutine = StartCoroutine(this.CoChangeImage());
            this.ShowUI(false);
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
                this.pageTime.text = Mathf.Clamp(waitTime - 1, 1f, 180f).ToString();
            }
        }

        void OnTimeUp()
        {
            if (float.TryParse(this.pageTime.text, out float waitTime))
            {
                this.pageTime.text = Mathf.Clamp(waitTime + 1, 1f, 180f).ToString();
            }
        }

        void OnShaffle()
        {
            if (null == this.indices)
            {
                return;
            }
            this.isShaffle = !this.isShaffle;
            for (int i = 0; i < this.indices.Length; i++)
            {
                this.indices[i] = i;
            }
            if (this.isShaffle)
            {
                Shuffle(this.indices);
            }
        }

        void ShowNextImage()
        {
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
                    Screen.fullScreen = false;
                }
            }
            if (this.folderPath.gameObject.activeSelf)
            {
                return;
            }
            var pos = Input.mousePosition;
            var buttonPos = this.windowFullscreenButton.transform.position;
            if (100 > Vector3.Distance(pos, buttonPos))
            {
                this.ShowUI(true);
            }
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
        }

        const string FOLDER_PATH = "folderPath";
        Coroutine coroutine = null;
        int[] indices;
        bool isShaffle = false;
        bool isPlay = false;
        int index = 0;
        List<FileInfo> files = new List<FileInfo>();
    }
}
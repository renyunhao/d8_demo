using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldValidator : MonoBehaviour
    {
        public static bool GlobalEnable = true;
        public static readonly HashSet<char> InvalidChars = new HashSet<char>{
        '\'','"','~','`','@','$','%','^','&','*','(',')','+','=','>','<',
        '|','{','}','/','\\',':',';','\r','\n'};
        public const char Empty = '\0';

        [HideInInspector]
        public bool hasBlockWords = false;
        [HideInInspector]
        public bool enableLengthLimit = true;
        [HideInInspector]
        public bool enableBlock = true;

        /// <summary>
        /// 当输入内容为空时，需要停响应的控件
        /// </summary>
        public Selectable[] selectableControls;
        /// <summary>
        /// 停止响应时，控件的替换材质
        /// </summary>
        public Material interactableDisabledMaterial;
        /// <summary>
        /// 停止响应时，要替换材质的控件
        /// </summary>
        public Image[] interactableReplaceMaterialImages;

        private TMP_InputField inputField;
        private int inputLengthLimit = -1;
        private int inputBytesLimit = -1;
        private int singleByteCharCountLimit = -1;

        private int singleByteCharCount = 0;
        private bool disabled = false;

        public int InputLengthLimit => inputLengthLimit;
        public int InputBytesLimit => inputBytesLimit;

        public bool Disabled
        {
            get
            {
                return disabled;
            }
            set
            {
                disabled = value;
                if (disabled == false)
                {
                    CheckTextIsEmpty();
                }
            }
        }

        void Start()
        {
            inputField = this.GetComponent<TMP_InputField>();
            inputField.onValidateInput += OnValidateInput;
            inputField.onValueChanged.AddListener(OnValueChanged);
            inputField.onEndEdit.AddListener(OnEndEdit);
            CalculateSingleByteCharCount();
            CheckTextIsEmpty();
        }

        /// <summary>
        /// 限制输入的字符数量
        /// </summary>
        /// <param name="limit"></param>
        public void SetInputLengthLimit(int limit)
        {
            inputLengthLimit = limit;
            inputBytesLimit = -1;
        }

        /// <summary>
        /// 限制输入的字节数
        /// </summary>
        /// <param name="limit">总字节限制</param>
        /// <param name="singleByteCharCountLimit">单字节字符数量限制</param>
        public void SetInputBytesLimit(int limit, int singleByteCharCountLimit = -1)
        {
            inputLengthLimit = -1;
            inputBytesLimit = limit;
            if (singleByteCharCountLimit > 0)
            {
                this.singleByteCharCountLimit = singleByteCharCountLimit;
            }
            else
            {
                this.singleByteCharCountLimit = limit;
            }
        }

        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            if (InvalidChars.Contains(addedChar))
            {
                return Empty;
            }

            if (enableLengthLimit)
            {
                if (inputLengthLimit > 0)
                {
                    if (inputField.textComponent.textInfo.characterCount > inputLengthLimit)
                    {
                        return Empty;
                    }
                }
                else if (inputBytesLimit > 0)
                {
                    int currentBytes = Encoding.UTF8.GetBytes(text).Length;
                    int inputBytes = Encoding.UTF8.GetBytes(addedChar.ToString()).Length;
                    bool inputSingleByte = false;
                    if (inputBytes == 1)
                    {
                        inputSingleByte = true;
                    }
                    if (inputSingleByte)
                    {
                        //如果输入的是单字节字符，且数量超过上限，禁止输入
                        if (singleByteCharCount + 1 > singleByteCharCountLimit)
                        {
                            return Empty;
                        }
                    }
                    else
                    {
                        //如果输入的是多字节字符，但是单字节字符数量已经达到上限，禁止输入
                        if (singleByteCharCount >= singleByteCharCountLimit)
                        {
                            return Empty;
                        }
                    }
                    //最要后要检查总的字节数不能超过上限
                    if (currentBytes + inputBytes > inputBytesLimit)
                    {
                        return Empty;
                    }
                }
            }
            return addedChar;
        }

        private void OnValueChanged(string text)
        {
            //输入文字时，重置hasBlockWords
            hasBlockWords = false;
            CalculateSingleByteCharCount();
            CheckTextIsEmpty();
        }

        private void OnEndEdit(string text)
        {
            //输入完的文字要进行敏感词过滤
            if (enableBlock && GlobalEnable)
            {
                var filterInfo = inputField.text.FilterBlockWords(false);
                inputField.SetTextWithoutNotify(filterInfo.filterResult);
                hasBlockWords = filterInfo.hasBlockWords;
            }
        }

        private void CheckTextIsEmpty()
        {
            if (string.IsNullOrEmpty(inputField.text) || Disabled)
            {
                foreach (var control in selectableControls)
                {
                    control.interactable = false;
                }
                foreach (var image in interactableReplaceMaterialImages)
                {
                    image.material = interactableDisabledMaterial;
                }
            }
            else
            {
                foreach (var control in selectableControls)
                {
                    control.interactable = true;
                }
                foreach (var image in interactableReplaceMaterialImages)
                {
                    image.material = null;
                }
            }
        }

        private void CalculateSingleByteCharCount()
        {
            singleByteCharCount = 0;
            foreach (var c in inputField.text)
            {
                int length = Encoding.UTF8.GetBytes(c.ToString()).Length;
                if (length == 1)
                {
                    singleByteCharCount++;
                }
            }
        }
    }
}
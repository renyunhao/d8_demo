using System;
using System.Collections.Generic;
using System.Text;

namespace GameFramework
{
    public class WebRequestUtil
    {
        public const string ContentType = "Content-Type";
        public const string Accept = "Accept";

        public static event Action OnBeforeSend;
        public static event Action OnAfterReceived;

        public delegate void RequestCallback(ResponseData responseData);

        private static IRequester requester = new RequesterUnityWebRequest();

        public static Dictionary<string, string> basicHeader = new Dictionary<string, string>();


        static WebRequestUtil()
        {
            basicHeader.Add(ContentType, "application/json;charset=utf-8");
        }

        public static void SetBasicHeader(string key, string value)
        {
            basicHeader[key] = value;
        }

        public static void ChangeRequestTool<T>(T requestTool) where T : IRequester
        {
            requester = requestTool;
        }

        public static void Post(string url, string text, Dictionary<string, string> headerDic, RequestCallback callback = null)
        {
            requester?.Post(url, text, headerDic, callback);
        }

        public static void Post(string url, string text, RequestCallback callback = null)
        {
            requester?.Post(url, text, callback);
        }

        public static void Post(string url, byte[] bytes, RequestCallback callback = null)
        {
            requester?.Post(url, bytes, callback);
        }

        public static void Get(string url, List<KeyValuePair<string, object>> param, RequestCallback callback = null)
        {
            StringBuilder newUrlBuilder = new StringBuilder();
            newUrlBuilder.Append(url);
            if (param != null && param.Count > 0)
            {
                newUrlBuilder.Append("?");
                for (int i = 0; i < param.Count; i++)
                {
                    if (i > 0)
                    {
                        newUrlBuilder.Append("&");
                    }
                    newUrlBuilder.Append($"{param[i].Key}={param[i].Value}");
                }
            }
            url = newUrlBuilder.ToString();

            requester?.Get(url, callback);
        }

        public static void OnBeforeEvent()
        {
            OnBeforeSend?.Invoke();
        }
        public static void OnAfterReceiveEvent()
        {
            OnAfterReceived?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Cysharp.Threading.Tasks;
using static GameFramework.WebRequestUtil;

namespace GameFramework
{
    public class RequesterHttpWebRequest : IRequester
    {
        private static Debug logger = new Debug("WebRequestUtil");

        public void Get(string url, RequestCallback callback)
        {
            try
            {
                logger.I($"Get: {url}");
                WebRequestUtil.OnBeforeEvent();
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                foreach (var kvp in basicHeader)
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }

                (HttpWebRequest request, RequestCallback callback) param = new ValueTuple<HttpWebRequest, RequestCallback>();
                request.BeginGetResponse(HttpWebRequestFinish, param);
            }
            catch (Exception e)
            {
                callback?.Invoke(HandleRequestException(e));
                WebRequestUtil.OnAfterReceiveEvent();
            }
        }

        public void Post(string url, string text, RequestCallback callback)
        {
            logger.I($"Post: {url} 参数: {text}");
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            DoPost(url, bytes, null, callback);
        }

        public void Post(string url, byte[] bytes, RequestCallback callback)
        {
            logger.I($"Post: {url}");
            DoPost(url, bytes, null, callback);
        }

        public void Post(string url, string text, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            logger.I($"Post: {url} 参数: {text}");
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            DoPost(url, bytes, headerDic, callback);
        }

        public void Post(string url, byte[] bytes, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            logger.I($"Post: {url}");
            DoPost(url, bytes, headerDic, callback);
        }

        private void DoPost(string url, byte[] bytes, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            try
            {
                WebRequestUtil.OnBeforeEvent();
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "POST";
                foreach (var kvp in basicHeader)
                {
                    if (kvp.Key.Equals(ContentType))
                    {
                        request.ContentType = kvp.Value;
                    }
                    else if (kvp.Key.Equals(Accept))
                    {
                        request.Accept = kvp.Value;
                    }
                    else
                    {
                        request.Headers.Add(kvp.Key, kvp.Value);
                    }
                }
                if (headerDic != null)
                {
                    foreach (var kvp in headerDic)
                    {
                        request.Headers.Add(kvp.Key, kvp.Value);
                    }
                }

                request.ContentLength = bytes.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                (HttpWebRequest request, RequestCallback callback) param = new ValueTuple<HttpWebRequest, RequestCallback>(request, callback);
                request.BeginGetResponse(HttpWebRequestFinish, param);
            }
            catch (Exception e)
            {
                callback?.Invoke(HandleRequestException(e));
                WebRequestUtil.OnAfterReceiveEvent();
            }
        }

        private async void HttpWebRequestFinish(IAsyncResult result)
        {
            (HttpWebRequest request, RequestCallback callback) param = (ValueTuple<HttpWebRequest, RequestCallback>)result.AsyncState;
            try
            {
                var response = param.request.EndGetResponse(result) as HttpWebResponse;
                await UniTask.SwitchToMainThread();
                param.callback?.Invoke(HandleRequestFinish(response));
                WebRequestUtil.OnAfterReceiveEvent();
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                param.callback?.Invoke(HandleRequestException(e));
                WebRequestUtil.OnAfterReceiveEvent();
            }
        }

        private ResponseData HandleRequestFinish(HttpWebResponse response)
        {
            ResponseData data = new ResponseData();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                data.succeed = true;
                string respText;
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    respText = sr.ReadToEnd();
                }
                data.text = respText;
                logger.I($"收到回复: {response.ResponseUri} 参数: {data.text}");
            }
            else
            {
                //发送出了问题
                data.text = $"请求发送失败！StatusCode: {response.StatusCode} StatusDescription: {response.StatusDescription}";
                logger.E(data.text);
            }
            return data;
        }

        private ResponseData HandleRequestException(Exception e)
        {
            ResponseData data = new ResponseData();
            data.text = e.Message;
            logger.E($"网络异常: {data.text}");
            return data;
        }
    }
}

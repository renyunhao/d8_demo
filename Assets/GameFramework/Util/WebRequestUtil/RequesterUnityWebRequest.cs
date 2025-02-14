using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using static GameFramework.WebRequestUtil;

namespace GameFramework
{
    public class RequesterUnityWebRequest : IRequester
    {
        private static Debug logger = new Debug("WebRequestUtil");
        public void Get(string url, RequestCallback callback)
        {
            logger.I($"Get: {url}");
            CoroutineUtil.DoCoroutine(DoGet(url, callback));
        }

        public void Post(string url, string text, RequestCallback callback)
        {
            logger.I($"Post: {url} 参数: {text}");
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            CoroutineUtil.DoCoroutine(DoPost(url, bytes, null, callback));
        }

        public void Post(string url, byte[] bytes, RequestCallback callback)
        {
            logger.I($"Post: {url}");
            CoroutineUtil.DoCoroutine(DoPost(url, bytes, null, callback));
        }

        public void Post(string url, string text, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            logger.I($"Post: {url} 参数: {text}");
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            CoroutineUtil.DoCoroutine(DoPost(url, bytes, headerDic, callback));
        }

        public void Post(string url, byte[] bytes, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            logger.I($"Post: {url}");
            CoroutineUtil.DoCoroutine(DoPost(url, bytes, headerDic, callback));
        }

        IEnumerator DoGet(string url, RequestCallback callback)
        {
            WebRequestUtil.OnBeforeEvent();
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                foreach (var kvp in basicHeader)
                {
                    request.SetRequestHeader(kvp.Key, kvp.Value);
                }
                yield return request.SendWebRequest();
                callback?.Invoke(HandleRequestFinish(request));
                WebRequestUtil.OnAfterReceiveEvent();
            }
        }

        IEnumerator DoPost(string url, byte[] bytes, Dictionary<string, string> headerDic, RequestCallback callback)
        {
            OnBeforeEvent();
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.timeout = 5;
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                foreach (var kvp in basicHeader)
                {
                    request.SetRequestHeader(kvp.Key, kvp.Value);
                }
                if (headerDic != null)
                {
                    foreach (KeyValuePair<string, string> oneData in headerDic)
                    {
                        request.SetRequestHeader(oneData.Key, oneData.Value);
                    }
                }
                yield return request.SendWebRequest();
                callback?.Invoke(HandleRequestFinish(request));
                WebRequestUtil.OnAfterReceiveEvent();
            }
        }

        private ResponseData HandleRequestFinish(UnityWebRequest request)
        {
            ResponseData data = new ResponseData();
            bool isSucceed = request.result == UnityWebRequest.Result.Success;
            if (isSucceed)
            {
                data.succeed = true;
                data.text = request.downloadHandler.text;
                data.bytes = request.downloadHandler.data;
            }
            else
            {
                if (request.isDone)
                {
                    //发送成功，问题出在回复阶段
                    data.text = $"请求发送成功，但返回失败！Result: {(UnityWebRequest.Result)request.result} Error: {request.error}";
                }
                else
                {
                    //发送出了问题
                    data.text = $"请求发送失败！Error: {request.error}";
                }
            }
            if (isSucceed)
            {
                logger.I($"收到回复: {request.url} 参数: {data.text}");
            }
            else
            {
                logger.E($"收到回复: {request.url} 参数: {data.text}");
            }
            return data;
        }
    }

}
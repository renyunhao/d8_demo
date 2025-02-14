using System.Collections.Generic;
using static GameFramework.WebRequestUtil;

namespace GameFramework
{
    public interface IRequester
    {
        void Get(string url, RequestCallback callback);
        void Post(string url, string text, RequestCallback callback);
        void Post(string url, byte[] bytes, RequestCallback callback);
        void Post(string url, string data, Dictionary<string, string> headerDic, RequestCallback callback);
        void Post(string url, byte[] bytes, Dictionary<string, string> headerDic, RequestCallback callback);
    }
}

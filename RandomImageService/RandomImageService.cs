using Furina.RandomImageService.DataDefines;
using Furina.RandomImageService.Utils;
using System.Text.Json.Nodes;

namespace Furina.RandomImageService
{
    public class RandomImageService
    {
        private RequestMode m_RequestMode;
        private Dictionary<string, string> m_HttpHeaders = new Dictionary<string, string>();
        private Dictionary<string, string> m_ApiHttpHeaders = new Dictionary<string, string>();
        private string? m_JsonPathToImgUrl;
        private string? m_NodePathToImgUrl;
        private Uri m_EndPoint;
        public RandomImageService(Uri endPoint, RequestMode requestMode = RequestMode.Raw)
        {
            m_RequestMode = requestMode;
            m_EndPoint = endPoint;
        }
        /// <summary>
        /// 导航到图像url的路径字符串。只在RequestMode.JsonPathName & RequestMode.JsonArray时需要。
        /// 在RequestMode.JsonPathToObjectArray时，需要用%Array%代表JsonArray的路径。
        /// </summary>
        /// <param name="path">e.g. data/img_url</param>
        /// <exception cref="ArgumentNullException">如果 <paramref name="path"/> 为 null。</exception>
        public void SetJsonPath(string path)
        {
            if (path == string.Empty) throw new ArgumentException("JsonPath 不能为空。");
            if (path.Contains("%Array%") && m_RequestMode == RequestMode.JsonPathToObjectArray)
            {
                m_JsonPathToImgUrl = path.Split("%Array%")[0];
                m_NodePathToImgUrl = path.Split("%Array%")[1];
            }
            else
            {
                m_JsonPathToImgUrl = path;
            }
        }
        /// <summary>
        /// 添加一个HttpHeader。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>是否成功添加Header键值对。</returns>
        public bool AddHttpHeader(string key,string value) => m_HttpHeaders.TryAdd(key, value);
        /// <summary>
        /// 添加一个HttpHeader。用于Api请求。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>是否成功添加Header键值对。</returns>
        public bool AddApiHttpHeader(string key, string value) => m_ApiHttpHeaders.TryAdd(key, value);
        /// <summary>
        /// 设置完整的HttpHeaders。
        /// </summary>
        /// <param name="headers"></param>
        public void SetHttpHeaders(Dictionary<string, string> headers) => m_HttpHeaders = headers;
        /// <summary>
        /// 设置完整的HttpHeaders。用于Api请求。
        /// </summary>
        /// <param name="headers"></param>
        public void SetApiHttpHeaders(Dictionary<string, string> headers) => m_ApiHttpHeaders = headers;

        private List<(JsonNode?, string)> m_ResultsTemp = new List<(JsonNode?, string)> ();//缓存

        /// <summary>
        /// 获取一次随机图像。
        /// </summary>
        /// <returns></returns>
        public async Task<ImageResult> GetNext()
        {
            if(m_RequestMode == RequestMode.JsonArray || m_RequestMode == RequestMode.JsonPathName || m_RequestMode == RequestMode.JsonPathToArray
                || m_RequestMode == RequestMode.JsonPathToObjectArray)
            {
                if (m_JsonPathToImgUrl is null) throw new ArgumentNullException("必须先调用 SetJsonPath() 以设置图像路径。");
            }
            //获取图片
            //检查缓存，无缓存则先填充缓存（Raw和Image模式下无效。）
            
            //不支持缓存
            if(m_RequestMode == RequestMode.Raw || m_RequestMode == RequestMode.Image)
            {
                (int, MemoryStream?) resp = await Downloader.Download(m_EndPoint, m_HttpHeaders);
                if (resp.Item1 != 200) throw new Exception("服务器返回了不正确的HttpCode：" + resp.Item1.ToString());
                return new ImageResult(resp.Item2!, null);
            }
            //没有缓存，添加缓存
            while (m_ResultsTemp.Count == 0)
            {
                //先获取一次服务器数据(API)
                (int, MemoryStream?) resp = await Downloader.Download(m_EndPoint, m_ApiHttpHeaders);
                if (resp.Item1 != 200) throw new Exception("服务器返回了不正确的HttpCode：" + resp.Item1.ToString());
                switch (m_RequestMode)
                {
                    case RequestMode.JsonArray:
                        //根节点转化为Array后下载
                        JsonArray rawarray = JsonNode.Parse(resp.Item2!)!.AsArray();
                        foreach (var item in rawarray)
                        {
                            string urltodown = item!.NavigateTo(m_JsonPathToImgUrl!).ToString();
                            m_ResultsTemp.Add((item,urltodown));
                        }
                        break;
                    case RequestMode.JsonPathName:
                        JsonNode raw = JsonNode.Parse(resp.Item2!)!;
                        string urltodown1 = raw.NavigateTo(m_JsonPathToImgUrl!).ToString();
                        m_ResultsTemp.Add((raw,urltodown1));
                        break;
                    case RequestMode.JsonPathToArray:
                        JsonNode raww = JsonNode.Parse(resp.Item2!)!;
                        JsonArray resurls = raww.NavigateTo(m_JsonPathToImgUrl!).AsArray();
                        foreach (var item in resurls)
                        {
                            m_ResultsTemp.Add((JsonNode.Parse($"{{\"url\" : \"{item!.ToString()}\"}}"), item.ToString()));
                        }
                        break;
                    case RequestMode.JsonList:
                        JsonArray ja = JsonNode.Parse(resp.Item2!)!.AsArray();
                        foreach (var jn in ja)
                        {
                            m_ResultsTemp.Add((JsonNode.Parse($"{{\"url\" : \"{jn!.ToString()}\"}}"), jn!.ToString()));
                        }
                        break;
                    case RequestMode.UrlLines:
                        StreamReader streamReader = new StreamReader(resp.Item2!);
                        string? rurl = streamReader.ReadLine();
                        while (rurl != null)
                        {
                            if (!rurl.StartsWith("http")) continue;//跳过非http的行
                            m_ResultsTemp.Add((JsonNode.Parse($"{{\"url\" : \"{rurl}\"}}"), rurl));
                            streamReader.ReadLine();
                        }
                        break;
                    case RequestMode.JsonPathToObjectArray:
                        JsonNode jraw = JsonNode.Parse(resp.Item2!)!;
                        JsonArray navtoarray = jraw.NavigateTo(m_JsonPathToImgUrl!).AsArray();
                        foreach(var item in navtoarray)
                        {
                            JsonNode finalnav = item!.NavigateTo(m_NodePathToImgUrl!);
                            m_ResultsTemp.Add((item, finalnav!.ToString()));
                        }
                        break;
                    default:
                        throw new Exception("未知的 RequestMode。");
                }
            }
            //从缓存返回内容并删除缓存
            (JsonNode?, string) fromtemp = m_ResultsTemp[0];
            m_ResultsTemp.RemoveAt(0);

            //下载并返回
            (int, MemoryStream?) img = await Downloader.Download(new Uri(fromtemp.Item2), m_HttpHeaders);
            if (img.Item1 != 200) throw new Exception("服务器返回了不正确的HttpCode：" + img.Item1.ToString());
            return new ImageResult(img.Item2!, fromtemp.Item1);
        }
        public void ReleaseResult(ImageResult imageresult) => imageresult.ImageData.Dispose();
        public int GetTempLength() => m_ResultsTemp.Count;
        public void ClearTemp() => m_ResultsTemp.Clear();
    }
}

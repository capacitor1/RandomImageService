using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furina.RandomImageService
{
    internal class Downloader
    {
        /// <summary>
        /// 下载数据。(非http错误无限重试）
        /// </summary>
        /// <param name="url">访问的URL</param>
        /// <param name="httpheaders">额外请求头</param>
        /// <returns>(Http的返回值，实际二进制内容)，如果Http返回值不为200则二进制内容为null</returns>
        public static async Task<(int,MemoryStream?)> Download(Uri url,Dictionary<string,string>? httpheaders)
        {
            MemoryStream fs = new();
            int hr = -1;
            while (true)
            {
                var httpClient = new HttpClient();
                if(httpheaders != null)
                {
                    foreach (var header in httpheaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                try
                {
                    fs.Position = 0;
                    HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = url,
                    });
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)//不为200说明服务器没返回正常数据，应直接返回
                    {
                        return ((int)response.StatusCode, null);
                    }
                    hr = (int)response.StatusCode;
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[65536];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                    break;
                }
                catch
                {
                    //忽略错误，直接重试（此处只会重试网络连接错误，如因切换VPN节点导致连接中断。因用户操作不当或服务器爆炸导致返回429 404 503之类错误直接返回。
                }
            }
            fs.Position = 0;
            return (hr,fs);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;

namespace Furina.RandomImageService.DataDefines
{
    public class ImageResult
    {
        public ImageResult(MemoryStream ms, JsonNode? jn)
        {
            ImageData = ms;
            MetaData = jn is null ? new JsonObject() : jn;
            //流的位置需设置为0，避免读取时异常偏移
            ImageData.Position = 0;
        }
        /// <summary>
        /// 图像数据流。
        /// </summary>
        public MemoryStream ImageData { get; }
        /// <summary>
        /// 元数据。不为null，但仅在Json请求模式下才有内容。
        /// </summary>
        public JsonNode MetaData { get; }
        public string Sha1
        {
            get
            {
                using (var h = SHA1.Create())
                {
                    byte[] hashBytes = h.ComputeHash(ImageData);
                    ImageData.Position = 0;
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
        public string Sha256
        {
            get
            {
                using (var h = SHA256.Create())
                {
                    byte[] hashBytes = h.ComputeHash(ImageData);
                    ImageData.Position = 0;
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
        /// <summary>
        /// 确认该图片数据为.jpg .png .webp .gif .avif中的哪一种。不能确定则返回.bin。
        /// </summary>
        public string FileExtension
        {
            get
            {
                byte[] magicnum = new byte[12];
                ImageData.Read(magicnum);
                ImageData.Position = 0;
                if (magicnum[..8].SequenceEqual(Consts.FileMagic["png"])) return ".png";
                else if (magicnum[..2].SequenceEqual(Consts.FileMagic["jpg"])) return ".jpg";
                else if (magicnum[..4].SequenceEqual(Consts.FileMagic["webp"])) return ".webp";
                else if (magicnum[..6].SequenceEqual(Consts.FileMagic["gif"])) return ".gif";
                else if (magicnum[4..12].SequenceEqual(Consts.FileMagic["avif"])) return ".avif";
                else return ".bin";
            }
        }
    }
    /// <summary>
    /// 请求模式。
    /// </summary>
    public enum RequestMode
    {
        /// <summary>
        /// 返回原始数据，不考虑解析和检查。
        /// </summary>
        Raw = 0,//default
        /// <summary>
        /// 直接返回二进制图像数据。
        /// </summary>
        Image = 1,
        /// <summary>
        /// 返回JSON，根为包含多个返回值的array，并通过path导航到实际图像。
        /// </summary>
        JsonArray = 2,
        /// <summary>
        /// 返回JSON，根为单个Object，通过path导航到实际图像。
        /// </summary>
        JsonPathName = 3,
        /// <summary>
        /// 返回JSON，根为单个Object，通过path导航到一个包含多个实际图像的列表。
        /// </summary>
        JsonPathToArray = 4,
        /// <summary>
        /// 返回JSON，根为一个包含多个实际图像的列表。
        /// </summary>
        JsonList = 5,
        /// <summary>
        /// 返回JSON，根为单个Object，先通过path导航到包含多个返回体的JsonNode，再通过导航到实际图像URL。
        /// </summary>
        JsonPathToObjectArray = 6,
        /// <summary>
        /// 返回纯文本，每行一个实际图像URL（允许包含非URL的描述行，处理时自动跳过）。
        /// </summary>
        UrlLines = 7,

    }
}

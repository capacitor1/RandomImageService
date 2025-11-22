using Furina.RandomImageService;
using Furina.RandomImageService.DataDefines;
using System.Text.Json;

Directory.CreateDirectory("Test");
//创建新的RandomImageService，需要输入随机图床的API接口，并根据情况选择对应的RequestMode。
RandomImageService randomImageService = new(new Uri("https://cnmiw.com/api.php?sort=random&type=json&num=100"), RequestMode.JsonPathToArray);
//RandomImageService randomImageService = new(new Uri("https://www.dmoe.cc/random.php"), RequestMode.Image);

//如果是Json返回模式，可能需要设置获取图片的路径。在Raw和Image模式下，该操作会被忽略。
randomImageService.SetJsonPath("/pic");

//有些图源是对请求有限制的，比如需要Referer，或Cookie。AddHttpHeader用于添加请求头（模式和HttpClient一致）。
randomImageService.AddHttpHeader("Referer", "https://weibo.com/");

//有些图源API是对请求有限制的，比如需要Referer，或Cookie。AddApiHttpHeader用于添加请求头（模式和HttpClient一致）。
//randomImageService.AddApiHttpHeader("Referer", "https://weibo.com/");


//示例：用于批量测试随机图床的返回图片。！注意！：长时间运行以下代码可能导致IP被服务器封禁，仅作示例，不可照抄！
while (true)
{
    try
    {
        //GetNext获取一张随机图片。
        ImageResult ir = await randomImageService.GetNext();
        //支持Sha1/Sha256校验图片（用于查重）。FileExtension用于判断基本的图片文件名（无视服务器返回的MIME）。
        string savename = $"{ir.Sha1}{ir.FileExtension}";
        //GetTempLength()获取此RIS实例内的图片缓存。此缓存在批量获取JSON返回值时才有效。MetaData是JSON返回时的原始数据，可用于进一步解析图片消息。
        Console.WriteLine($"\r\n\r\n[ImageResult] Temp Remains = {randomImageService.GetTempLength()}\r\nGet Image with Node:\r\n{ir.MetaData.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}\r\nData:\r\n[Stream] Length = {ir.ImageData.Length}\r\n{savename}");
        //保存图片（此示例程序未考虑图片重复下载的处理，覆盖写入。正式项目不可如此操作）
        var f = File.OpenWrite(Path.Combine("Test",savename));
        ir.ImageData.CopyTo(f);
        f.Dispose();
        //释放ImageResult中的MemoryStream。
        randomImageService.ReleaseResult(ir);

    }
    catch (Exception e)
    {
        //在GetNext时，如遇服务器404/429之类的错误，会直接报错，故需要catch。
        Console.WriteLine(e.Message);
    }
}
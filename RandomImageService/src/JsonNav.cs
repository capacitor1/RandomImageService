using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Furina.RandomImageService.Utils
{
    internal static class JsonPathNav
    {
        /// <summary>
        /// 根据导航路径查找JsonNode的子节点
        /// </summary>
        /// <param name="rootNode">根JsonNode节点</param>
        /// <param name="navpath">导航路径</param>
        /// <returns>目标JsonNode，路径不存在则返回空JsonObject</returns>
        /// <exception cref="ArgumentNullException">根节点或导航路径为null时抛出</exception>
        /// <exception cref="InvalidOperationException">导航过程中遇到JsonArray时抛出</exception>
        public static JsonNode NavigateTo(this JsonNode rootNode, string navpath)
        {
            // 空值校验
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode), "根JsonNode节点不能为null");

            // 分割路径并过滤空段（处理首尾/和连续/的情况）
            var pathSegments = navpath.Split('/')
                                      .Where(segment => !string.IsNullOrWhiteSpace(segment))
                                      .ToList();

            // 无路径段时直接返回根节点
            if (pathSegments.Count == 0)
                return rootNode;

            JsonNode currentNode = rootNode;

            // 逐级遍历路径段
            foreach (var segment in pathSegments)
            {
                // 仅当当前节点是JsonObject时，才能通过键查找子节点
                if (currentNode is not JsonObject currentObj)
                {
                    // 非对象且非数组，视为路径不存在，返回空JsonObject
                    return new JsonObject();
                }

                // 查找当前路径段的子节点，不存在则返回空JsonObject
                if (!currentObj.TryGetPropertyValue(segment, out currentNode!))
                {
                    return new JsonObject();
                }
            }

            // 导航成功，返回目标节点
            return currentNode;
        }
    }
}

# RandomImageService 随机图床服务（客户端）

支持多种随机图片API接口的随机图床客户端程序。适配二进制图片返回/json列表返回，支持结果缓存，提升响应速度，减少API请求次数。

以下简称RIS。

## 功能特性

- 自由地适配网络上**大多数**随机图床API接口。没有特殊调整或者限制，全部由用户配置。RIS内并没有存储任何预设的图床接口URL，也没有针对某个图床的RequestMode，*但是这些功能的编写是基于对多个图床的观察和总结的，实际测试也没有问题。*

- 支持应对需要Referer/Cookie之类Http请求头的图床API。

- 支持**内容缓存**，在获取JSON返回值时，允许解析并缓存结果，加快调用时响应速度（建议配合容量较大的API请求，比如把API请求中的num设置到100，以便缓存），同时避免因访问过快而发生IP封禁。

- 支持用户配置JSON解析器，使用**路径**（例如`data/img/url`，但仅可使用`/`作为分隔符）**导航**到图像内容，灵活性强。

- 支持**校验基本文件类型**，避免服务器返回错误的图像类型（支持检查`jpg png avif webp gif`）

- 支持直接**校验图片SHA1/SHA256**以便防止重复。*不再支持校验Md5，因为Md5的碰撞可能性远比SHA系列大，不再可靠。*

- 尽可能使用原生方法处理API请求和JSON解析，减小代码量和发生错误的可能性。

## 使用说明

### 1.初始化和基本调用

```c#
//初始化一个RIS实例
RandomImageService randomImageService = new(new Uri("https://this.is.random.api/xxx/xxx?params=xxx"), RequestMode.Image);
//获取一张随机图片（使用async/await模式）
ImageResult ir = await randomImageService.GetNext();
```
代码极少，方便使用。

### 2.设置RequestMode和JsonPath

RequestMode是一个Enum枚举，必须正确设置其值以正确应对不同的API返回值。

- **`RequestMode.Raw`**

在不确定API会如何返回内容时，或API返回的内容显得特殊，而无法被RIS处理时使用。此方法不处理数据，需要用户后续自行处理。

- **`RequestMode.Image`**

在图床API自动跳转到目标图片URL，即直接返回二进制图像时设置为该值。

- **`RequestMode.JsonArray`**

适用于JSON返回的API，并且其返回值为一整个包含大量数据体的JsonArray时。

示例[^1]：
```json5
[//根节点为Array
    {
        "name": "这是图片",
        "url": "http://img.com/111.jpg"//图片URL
    },
    {
        "name": "这是图片",
        "url": "http://img.com/112.jpg"
    },
    {
        "name": "这是图片",
        "url": "http://img.com/113.jpg"
    }
]
```
此时，JsonPath应为 `/url`。

- **`RequestMode.JsonPathName`**

适用于一次只返回一张图片数据的JSON返回值，直接通过路径导航到图片url。

示例：
```json5
{
    "name": "Test",
    "data": {
        "img": {
            "name": "Google",
            "url": "http://img.com/google_snapshot.webp"//图片URL在这
        }
    }
}
```
此时，JsonPath应为 `/data/img/url`。

- **`RequestMode.JsonPathToArray`**

适用于返回一个Object根节点，但其中用一个Array存储多张图片的JSON返回值。

示例：
```json5
{
    "name": "Test",
    "data": {
        "img": [//一个Array
            "http://img.com/112.jpg",//多张图片URL
            "http://img.com/113.jpg"
        ]
    }
}
```
此时，JsonPath应为 `/data/img`。

- **`RequestMode.JsonList`**

适用于返回一个Array，其中存储多张图片的JSON返回值。

示例：
```json5
[//一个Array
    "http://img.com/112.jpg",//多张图片URL
    "http://img.com/113.jpg"
]
```
此模式不需要path。

- **`RequestMode.JsonPathToObjectArray`**

适用于返回一个Json结构，返回图片集是Array，并且图片URL在此Array中的Object里。

这也是目前使用较广泛、结构较规范的一种返回模式。

示例：
```json5
{
    "name": "Imgs",
    "page": 1,
    "data": [//注意这是个Array
        {
            "name": "A",
            "url": "http://img.com/113.jpg"//URL
        },
        {
            "name": "B",
            "url": "http://img.com/111.jpg"
        },
        {
            "name": "C",
            "url": "http://img.com/112.jpg"
        }
    ]
}
```
此时，JsonPath应为 `/data/%Array%/url`。

> 注：使用`%Array%`代表JsonArray的路径。只能使用一次。

- **`RequestMode.UrlLines`**

适用于返回纯文本的API，文本内容为一行一个图片URL。

示例：
```
[本次共随机到5张图片。]
http://img.com/113.jpg
http://img.com/114.jpg
http://img.com/115.jpg
http://img.com/116.jpg
http://img.com/117.jpg
```
注：支持存在一些不是URL的行。RIS会跳过这些行，比如示例中的第一行（example[0]）。

### 3.JsonPath特性

- 支持误输入多个斜杠的处理，比如`/path//to/img`

- 适配path首尾斜杠的不同习惯，比如`/path/to/img`,`path/to/img`,`/path/to/img/`均可。

- 如果输入的path有误，返回的JSON并不包含path导航到的目标，则返回空值。

### 4.图片校验

1. SHA1/SHA256校验：对于需要保存为文件的用户，这可以避免保存的文件出现重复。*但这并不能节约访问随机图床时的请求次数和网络流量。*

2. 文件类型校验：防止服务器传递的MIME类型错误，或者由于跳转策略问题导致源URL不包含文件类型。

> 注意：此文件类型校验功能仅基于文件头部的12个字节，但对于一般图床接口，此方法已经够用。支持检查出`jpg png avif webp gif`5种格式，其余格式默认为`bin`。

### 5.缓存控制

**缓存控制不适用于Raw和Image两种请求类型。**

此功能默认保持开启，目的是防止API请求次数过多或过快，导致触发429错误，甚至封禁IP。

建议配合容量较大的API请求使用，比如把API请求中的num设置到100，以便更好地缓存。

使用`RIS.GetTempLength()`获取当前实例中的缓存长度，如果缓存出现问题，可用`RIS.ClearTemp()`清空缓存。

## 程序示例

在`\RandomImageService.Example`文件夹下有一个可用的示例。

示例中已对所有重要的步骤做出注释。

[^1]:为了方便编写示例，所有示例采用非严格的json5格式以便直接注释。RIS实际上不支持json5解析。
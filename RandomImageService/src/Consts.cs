using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furina.RandomImageService
{
    internal static class Consts
    {
        public static readonly Dictionary<string, byte[]> FileMagic = new Dictionary<string, byte[]> {
            {"jpg",new byte[]{ 0xff,0xd8}},//2
            {"png",new byte[]{ 0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a}},//8
            {"gif",new byte[]{ 0x47,0x49,0x46,0x38,0x39,0x61}},//6
            {"webp",new byte[]{ 0x52,0x49,0x46,0x46}},//4
            {"avif",new byte[]{ 0x66,0x74,0x79,0x70,0x61,0x76,0x69,0x66}},//4-12
        };
    }
}

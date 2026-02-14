using System.Collections.Generic;
using UnityEngine;
using SharedTexHub.Data;

namespace SharedTexHub.Logic.Scanner
{
    public interface ITextureScanner
    {
        IEnumerable<TextureInfo> Scan(Material material);
    }
}

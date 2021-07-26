using ColossalFramework.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.Utilities
{
    public static class NetworkMultitoolTextures
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        public static string UUINormal => nameof(UUINormal);
        public static string UUIHovered => nameof(UUIHovered);
        public static string UUIPressed => nameof(UUIPressed);
        //public static string UUIDisabled => nameof(UUIDisabled);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(UUIButton), UUIButton},
        };

        static NetworkMultitoolTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NetworkMultitool), Files);
        }

        private static UITextureAtlas.SpriteInfo[] UUIButton(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 40, 40, UUINormal, UUIHovered, UUIPressed/*, UUIDisabled*/).ToArray();
    }
}

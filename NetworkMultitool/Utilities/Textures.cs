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

        public static string ActivationButtonNormal => nameof(ActivationButtonNormal);
        public static string ActivationButtonActive => nameof(ActivationButtonActive);
        public static string ActivationButtonHover => nameof(ActivationButtonHover);
        public static string ActivationButtonIconNormal => nameof(ActivationButtonIconNormal);
        public static string ActivationButtonIconActive => nameof(ActivationButtonIconActive);
        public static string ActivationButtonIconHover => nameof(ActivationButtonIconHover);

        public static string UUIButtonNormal => nameof(UUIButtonNormal);
        public static string UUIButtonHovered => nameof(UUIButtonHovered);
        public static string UUIButtonPressed => nameof(UUIButtonPressed);
        //public static string UUIDisabled => nameof(UUIDisabled);

        static NetworkMultitoolTextures()
        {
            var spriteParams = new Dictionary<string, RectOffset>();

            //ActivationButton
            spriteParams[ActivationButtonNormal] = new RectOffset();
            spriteParams[ActivationButtonActive] = new RectOffset();
            spriteParams[ActivationButtonHover] = new RectOffset();
            spriteParams[ActivationButtonIconNormal] = new RectOffset();
            spriteParams[ActivationButtonIconActive] = new RectOffset();
            spriteParams[ActivationButtonIconHover] = new RectOffset();

            //UUIButton
            spriteParams[UUIButtonNormal] = new RectOffset();
            spriteParams[UUIButtonHovered] = new RectOffset();
            spriteParams[UUIButtonPressed] = new RectOffset();

            foreach (var mode in EnumExtension.GetEnumValues<ToolModeType>().IsItem())
                spriteParams.Add(mode.ToString(), new RectOffset());

            Atlas = TextureHelper.CreateAtlas(nameof(NetworkMultitool), spriteParams);
        }
    }
}

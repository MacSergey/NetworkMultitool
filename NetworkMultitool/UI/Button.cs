using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using UnityEngine;
using NetworkMultitool.Utilities;
using static NetworkMultitool.Utilities.NetworkMultitoolTextures;

namespace NetworkMultitool.UI
{
    public class NetworkMultitoolButton : ToolButton<NetworkMultitoolTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(129, 38);
        protected override UITextureAtlas DefaultAtlas => NetworkMultitoolTextures.Atlas;

        protected override SpriteSet DefaultBgSprite => new SpriteSet(ActivationButtonNormal, ActivationButtonHover, ActivationButtonHover, ActivationButtonActive, string.Empty);
        protected override SpriteSet DefaultFgSprite => new SpriteSet(ActivationButtonIconNormal, ActivationButtonIconHover, ActivationButtonIconNormal, ActivationButtonIconNormal, string.Empty);


        public override void Start()
        {
            base.Start();
            ModesPanel.Add(this);
        }
    }
}

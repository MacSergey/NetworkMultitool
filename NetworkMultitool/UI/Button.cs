using ColossalFramework.UI;
using ModsCommon.UI;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.UI
{
    public class NetworkMultitoolButton : NetToolButton<NetworkMultitoolTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(129, 38);
        protected override UITextureAtlas Atlas => NetworkMultitoolTextures.Atlas;

        protected override string NormalBgSprite => NetworkMultitoolTextures.ButtonNormal;
        protected override string HoveredBgSprite => NetworkMultitoolTextures.ButtonHover;
        protected override string PressedBgSprite => NetworkMultitoolTextures.ButtonHover;
        protected override string FocusedBgSprite => NetworkMultitoolTextures.ButtonActive;
        protected override string NormalFgSprite => NetworkMultitoolTextures.Icon;
        protected override string HoveredFgSprite => NetworkMultitoolTextures.IconHover;
        protected override string PressedFgSprite => NetworkMultitoolTextures.Icon;
        protected override string FocusedFgSprite => NetworkMultitoolTextures.Icon;

        public override void Start()
        {
            base.Start();
            AddUIComponent<ModesPanel>();
        }
    }
}

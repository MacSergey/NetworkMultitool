using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.UI
{
    public class NetworkMultitoolButton : UUINetToolButton<Mod, NetworkMultitoolTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(129, 38);
        protected override UITextureAtlas Atlas => NetworkMultitoolTextures.Atlas;

        protected override string NormalBgSprite => NetworkMultitoolTextures.ActivationButtonNormal;
        protected override string HoveredBgSprite => NetworkMultitoolTextures.ActivationButtonHover;
        protected override string PressedBgSprite => NetworkMultitoolTextures.ActivationButtonHover;
        protected override string FocusedBgSprite => NetworkMultitoolTextures.ActivationButtonActive;
        protected override string NormalFgSprite => NetworkMultitoolTextures.ActivationButtonIconNormal;
        protected override string HoveredFgSprite => NetworkMultitoolTextures.ActivationButtonIconHover;
        protected override string PressedFgSprite => NetworkMultitoolTextures.ActivationButtonIconNormal;
        protected override string FocusedFgSprite => NetworkMultitoolTextures.ActivationButtonIconNormal;

        public override void Start()
        {
            base.Start();
            ModesPanel.Add(this);
        }
    }
}

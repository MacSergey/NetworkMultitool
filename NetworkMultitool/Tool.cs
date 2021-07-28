using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class NetworkMultitoolTool : BaseTool<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public static NetworkMultitoolShortcut ActivationShortcut { get; } = new NetworkMultitoolShortcut(nameof(ActivationShortcut), nameof(CommonLocalize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.T, true, false, false));
        public static NetworkMultitoolShortcut SelectionStepOverShortcut { get; } = new NetworkMultitoolShortcut(nameof(SelectionStepOverShortcut), nameof(CommonLocalize.Settings_ShortcutSelectionStepOver), SavedInputKey.Encode(KeyCode.Space, true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SelectionStepOver());

        public static IEnumerable<ToolModeType> ModeTypes => EnumExtension.GetEnumValues<ToolModeType>(m => m.IsItem());
        public static Dictionary<ToolModeType, NetworkMultitoolShortcut> ModeShortcuts { get; } = InitModeShortcuts();
        private static Dictionary<ToolModeType, NetworkMultitoolShortcut> InitModeShortcuts()
        {
            var dictionary = new Dictionary<ToolModeType, NetworkMultitoolShortcut>();

            foreach (var mode in ModeTypes)
            {
                var shortcut = new NetworkMultitoolShortcut(mode.ToString(), mode.GetAttr<DescriptionAttribute, ToolModeType>().Description, SavedInputKey.Encode((KeyCode)((int)KeyCode.Alpha1 + dictionary.Count), true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SetMode(mode));
                dictionary[mode] = shortcut;
            }

            return dictionary;
        }

        public static IEnumerable<Shortcut> ToolShortcuts
        {
            get
            {
                yield return SelectionStepOverShortcut;
                foreach (var shortcut in ModeShortcuts.Values)
                    yield return shortcut;
            }
        }
        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;
                if (Mode is BaseNetworkMultitoolMode mode)
                {
                    foreach (var shortcut in mode.Shortcuts)
                        yield return shortcut;
                }
            }
        }

        public override Shortcut Activation => ActivationShortcut;
        protected override bool ShowToolTip => ModesPanel?.IsHoverAllParents(MousePosition) != true;
        private IToolMode LastMode { get; set; }
        protected override IToolMode DefaultMode => LastMode ?? ToolModes[ToolModeType.AddNode];

        protected override UITextureAtlas UUIAtlas => NetworkMultitoolTextures.Atlas;
        protected override string UUINormalSprite => NetworkMultitoolTextures.UUINormal;
        protected override string UUIHoveredSprite => NetworkMultitoolTextures.UUIHovered;
        protected override string UUIPressedSprite => NetworkMultitoolTextures.UUIPressed;
        protected override string UUIDisabledSprite => /*NodeControllerTextures.UUIDisabled;*/string.Empty;
        protected ModesPanel ModesPanel { get; set; }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<AddNodeMode>();
            yield return CreateToolMode<RemoveNodeMode>();
            yield return CreateToolMode<UnionNodeMode>();
            yield return CreateToolMode<IntersectSegmentMode>();
            yield return CreateToolMode<SlopeNodeMode>();
            yield return CreateToolMode<ArrangeLineMode>();
            yield return CreateToolMode<CreateLoopMode>();
            yield return CreateToolMode<CreateConnectionMode>();
        }
        protected override void OnReset()
        {
            base.OnReset();
            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            ModesPanel.SetState(true);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            ModesPanel.SetState(false);
        }
        protected override void InitProcess()
        {
            base.InitProcess();
            AddModePanel();
        }
        protected override void SetModeNow(IToolMode mode)
        {
            base.SetModeNow(mode);

            if (mode != null)
                LastMode = Mode;
        }
        protected override bool CheckInfoMode(InfoManager.InfoMode mode, InfoManager.SubInfoMode subInfo) => (mode == InfoManager.InfoMode.None || mode == InfoManager.InfoMode.Underground) && subInfo == InfoManager.SubInfoMode.Default;

        public override void RegisterUUI()
        {
            base.RegisterUUI();
            if (IsInit)
                AddModePanel();
        }
        private void AddModePanel()
        {
            if (UUIRegistered)
            {
                ModesPanel = UUIButton.AddUIComponent<ModesPanel>();

                foreach (var mode in ToolModes.Values.OfType<BaseNetworkMultitoolMode>())
                    mode.AttachButton(ModesPanel);
            }
        }

        private void SelectionStepOver()
        {
            if (Mode is ISelectToolMode selectMode)
                selectMode.IgnoreSelected();
        }
        public bool SetSlope(ushort[] nodeIds)
        {
            var startY = nodeIds.First().GetNode().m_position.y;
            var endY = nodeIds.Last().GetNode().m_position.y;

            var list = new List<ITrajectory>();

            for (var i = 1; i < nodeIds.Length; i += 1)
            {
                var firstId = nodeIds[i - 1];
                var secondId = nodeIds[i];

                if (!NetExtension.GetCommon(firstId, secondId, out var commonSegmentId))
                    return false;
                else
                {
                    var segment = commonSegmentId.GetSegment();

                    var startPos = segment.m_startNode.GetNode().m_position;
                    var endPos = segment.m_endNode.GetNode().m_position;
                    var startDir = segment.m_startDirection.MakeFlatNormalized();
                    var endDir = segment.m_endDirection.MakeFlatNormalized();

                    startPos.y = 0;
                    endPos.y = 0;

                    list.Add(new BezierTrajectory(startPos, startDir, endPos, endDir));
                }
            }

            var sumLenght = list.Sum(t => t.Length);
            var currentLenght = 0f;
            for (var i = 1; i < nodeIds.Length - 1; i += 1)
            {
                currentLenght += list[i - 1].Length;
                var position = nodeIds[i].GetNode().m_position;
                position.y = Mathf.Lerp(startY, endY, currentLenght / sumLenght);
                NetManager.instance.MoveNode(nodeIds[i], position);
            }
            return true;
        }
    }

    public class NetworkMultitoolShortcut : ToolShortcut<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public NetworkMultitoolShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.Any) : base(name, labelKey, key, action, modeType) { }
    }
    public class NetworkMultitoolThreadingExtension : BaseUUIThreadingExtension<NetworkMultitoolTool> { }
    public class NetworkMultitoolLoadingExtension : BaseToolLoadingExtension<NetworkMultitoolTool> { }
}

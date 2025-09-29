namespace NetworkMultitool
{
	public class Localize
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static ModsCommon.LocalizeManager LocaleManager {get;} = new ModsCommon.LocalizeManager("Localize", typeof(Localize).Assembly);

		/// <summary>
		/// Add node mode
		/// </summary>
		public static string Mode_AddNode => LocaleManager.GetString("Mode_AddNode", Culture);

		/// <summary>
		/// Click to add node
		/// </summary>
		public static string Mode_AddNode_Info_ClickToAdd => LocaleManager.GetString("Mode_AddNode_Info_ClickToAdd", Culture);

		/// <summary>
		/// Hold {0} for precise measurement
		/// </summary>
		public static string Mode_AddNode_Info_PreciseMeasurement => LocaleManager.GetString("Mode_AddNode_Info_PreciseMeasurement", Culture);

		/// <summary>
		/// Select segment to add node
		/// </summary>
		public static string Mode_AddNode_Info_SelectToAdd => LocaleManager.GetString("Mode_AddNode_Info_SelectToAdd", Culture);

		/// <summary>
		/// Can't add node
		/// </summary>
		public static string Mode_AddNode_Info_TooCloseNode => LocaleManager.GetString("Mode_AddNode_Info_TooCloseNode", Culture);

		/// <summary>
		/// {0}°
		/// </summary>
		public static string Mode_AngleFormat => LocaleManager.GetString("Mode_AngleFormat", Culture);

		/// <summary>
		/// Arrange at circle mode
		/// </summary>
		public static string Mode_ArrangeAtCircle => LocaleManager.GetString("Mode_ArrangeAtCircle", Culture);

		/// <summary>
		/// Arrange at line mode
		/// </summary>
		public static string Mode_ArrangeAtLine => LocaleManager.GetString("Mode_ArrangeAtLine", Culture);

		/// <summary>
		/// Click to complete circle selection
		/// </summary>
		public static string Mode_ArrangeCircle_Info_ClickToComplite => LocaleManager.GetString("Mode_ArrangeCircle_Info_ClickToComplite", Culture);

		/// <summary>
		/// Click to select arrange direction
		/// </summary>
		public static string Mode_ArrangeLine_Info_ClickToSelectDirection => LocaleManager.GetString("Mode_ArrangeLine_Info_ClickToSelectDirection", Culture);

		/// <summary>
		/// Select segment to set arrange direction
		/// </summary>
		public static string Mode_ArrangeLine_Info_SelectDirection => LocaleManager.GetString("Mode_ArrangeLine_Info_SelectDirection", Culture);

		/// <summary>
		/// Double click on circle center
		/// </summary>
		public static string Mode_Connection_Info_DoubleClickOnCenterToChangeDir => LocaleManager.GetString("Mode_Connection_Info_DoubleClickOnCenterToChangeDir", Culture);

		/// <summary>
		/// Double click to add curve to this line
		/// </summary>
		public static string Mode_Connection_Info_DoubleClickToAdd => LocaleManager.GetString("Mode_Connection_Info_DoubleClickToAdd", Culture);

		/// <summary>
		/// Double right click to remove this curve
		/// </summary>
		public static string Mode_Connection_Info_DoubleClickToRemove => LocaleManager.GetString("Mode_Connection_Info_DoubleClickToRemove", Culture);

		/// <summary>
		/// Drag to change radius
		/// </summary>
		public static string Mode_Connection_Info_DragToChangeRadius => LocaleManager.GetString("Mode_Connection_Info_DragToChangeRadius", Culture);

		/// <summary>
		/// Drag to move circle
		/// </summary>
		public static string Mode_Connection_Info_DragToMove => LocaleManager.GetString("Mode_Connection_Info_DragToMove", Culture);

		/// <summary>
		/// Create connection mode
		/// </summary>
		public static string Mode_CreateConnection => LocaleManager.GetString("Mode_CreateConnection", Culture);

		/// <summary>
		/// Create curve mode
		/// </summary>
		public static string Mode_CreateCurve => LocaleManager.GetString("Mode_CreateCurve", Culture);

		/// <summary>
		/// Create loop mode
		/// </summary>
		public static string Mode_CreateLoop => LocaleManager.GetString("Mode_CreateLoop", Culture);

		/// <summary>
		/// Press {0} to change loop
		/// </summary>
		public static string Mode_CreateLoop_Info_Change => LocaleManager.GetString("Mode_CreateLoop_Info_Change", Culture);

		/// <summary>
		/// Create parallel mode
		/// </summary>
		public static string Mode_CreateParallerl => LocaleManager.GetString("Mode_CreateParallerl", Culture);

		/// <summary>
		/// Press {0} to apply
		/// </summary>
		public static string Mode_Info_Apply => LocaleManager.GetString("Mode_Info_Apply", Culture);

		/// <summary>
		/// Press {0} to arrange at circle
		/// </summary>
		public static string Mode_Info_ArrangeCircle_Apply => LocaleManager.GetString("Mode_Info_ArrangeCircle_Apply", Culture);

		/// <summary>
		/// Too big angle between nodes
		/// </summary>
		public static string Mode_Info_ArrangeCircle_BigDelta => LocaleManager.GetString("Mode_Info_ArrangeCircle_BigDelta", Culture);

		/// <summary>
		/// Double click to reset center position
		/// </summary>
		public static string Mode_Info_ArrangeCircle_DoubleClickToResetCenter => LocaleManager.GetString("Mode_Info_ArrangeCircle_DoubleClickToResetCenter", Culture);

		/// <summary>
		/// Double click to reset node position
		/// </summary>
		public static string Mode_Info_ArrangeCircle_DoubleClickToResetNode => LocaleManager.GetString("Mode_Info_ArrangeCircle_DoubleClickToResetNode", Culture);

		/// <summary>
		/// Drag to change radius
		/// </summary>
		public static string Mode_Info_ArrangeCircle_DragToChangeRadius => LocaleManager.GetString("Mode_Info_ArrangeCircle_DragToChangeRadius", Culture);

		/// <summary>
		/// Drag to move circle
		/// </summary>
		public static string Mode_Info_ArrangeCircle_DragToMoveCenter => LocaleManager.GetString("Mode_Info_ArrangeCircle_DragToMoveCenter", Culture);

		/// <summary>
		/// Drag to move node
		/// </summary>
		public static string Mode_Info_ArrangeCircle_DragToMoveNode => LocaleManager.GetString("Mode_Info_ArrangeCircle_DragToMoveNode", Culture);

		/// <summary>
		/// Hold {0} to move all nodes
		/// </summary>
		public static string Mode_Info_ArrangeCircle_MoveAll => LocaleManager.GetString("Mode_Info_ArrangeCircle_MoveAll", Culture);

		/// <summary>
		/// Press {0} to distribute nodes
		/// </summary>
		public static string Mode_Info_ArrangeCircle_PressToDistributeBetweenIntersections => LocaleManager.GetString("Mode_Info_ArrangeCircle_PressToDistributeBetweenIntersections", Culture);

		/// <summary>
		/// Press {0} to distribute nodes evenly on circle
		/// </summary>
		public static string Mode_Info_ArrangeCircle_PressToDistributeEvenly => LocaleManager.GetString("Mode_Info_ArrangeCircle_PressToDistributeEvenly", Culture);

		/// <summary>
		/// Press {0} to distribute
		/// </summary>
		public static string Mode_Info_ArrangeCircle_PressToDistributeIntersections => LocaleManager.GetString("Mode_Info_ArrangeCircle_PressToDistributeIntersections", Culture);

		/// <summary>
		/// Press {0} to reset parameters
		/// </summary>
		public static string Mode_Info_ArrangeCircle_PressToReset => LocaleManager.GetString("Mode_Info_ArrangeCircle_PressToReset", Culture);

		/// <summary>
		/// Wrong nodes order
		/// </summary>
		public static string Mode_Info_ArrangeCircle_WrongOrder => LocaleManager.GetString("Mode_Info_ArrangeCircle_WrongOrder", Culture);

		/// <summary>
		/// Press {0} to arrange at line
		/// </summary>
		public static string Mode_Info_ArrangeLine_Apply => LocaleManager.GetString("Mode_Info_ArrangeLine_Apply", Culture);

		/// <summary>
		/// Press {0} or {1} to change all radii
		/// </summary>
		public static string Mode_Info_ChangeAllRadius => LocaleManager.GetString("Mode_Info_ChangeAllRadius", Culture);

		/// <summary>
		/// Press {0} or {1} to change both radii
		/// </summary>
		public static string Mode_Info_ChangeBothRadius => LocaleManager.GetString("Mode_Info_ChangeBothRadius", Culture);

		/// <summary>
		/// Press {0} to switch selected circle
		/// </summary>
		public static string Mode_Info_ChangeCircle => LocaleManager.GetString("Mode_Info_ChangeCircle", Culture);

		/// <summary>
		/// Press {0} or {1} to change curve start offset
		/// </summary>
		public static string Mode_Info_ChangeOffset => LocaleManager.GetString("Mode_Info_ChangeOffset", Culture);

		/// <summary>
		/// Press {0} or {1} to change one radius
		/// </summary>
		public static string Mode_Info_ChangeOneRadius => LocaleManager.GetString("Mode_Info_ChangeOneRadius", Culture);

		/// <summary>
		/// Press {0} or {1} to change radius
		/// </summary>
		public static string Mode_Info_ChangeRadius => LocaleManager.GetString("Mode_Info_ChangeRadius", Culture);

		/// <summary>
		/// Press {0} or {1} to change offset
		/// </summary>
		public static string Mode_Info_ChangeShift => LocaleManager.GetString("Mode_Info_ChangeShift", Culture);

		/// <summary>
		/// Click to select first segment
		/// </summary>
		public static string Mode_Info_ClickFirstSegment => LocaleManager.GetString("Mode_Info_ClickFirstSegment", Culture);

		/// <summary>
		/// Click on node to change create direction
		/// </summary>
		public static string Mode_Info_ClickOnNodeToChangeCreateDir => LocaleManager.GetString("Mode_Info_ClickOnNodeToChangeCreateDir", Culture);

		/// <summary>
		/// Click to select second segment
		/// </summary>
		public static string Mode_Info_ClickSecondSegment => LocaleManager.GetString("Mode_Info_ClickSecondSegment", Culture);

		/// <summary>
		/// Click to select node
		/// </summary>
		public static string Mode_Info_ClickSelectNode => LocaleManager.GetString("Mode_Info_ClickSelectNode", Culture);

		/// <summary>
		/// Click to change create direction
		/// </summary>
		public static string Mode_Info_ClickToChangeCreateDir => LocaleManager.GetString("Mode_Info_ClickToChangeCreateDir", Culture);

		/// <summary>
		/// Click to unselect node
		/// </summary>
		public static string Mode_Info_ClickUnselectNode => LocaleManager.GetString("Mode_Info_ClickUnselectNode", Culture);

		/// <summary>
		/// Press {0} to create connection
		/// </summary>
		public static string Mode_Info_Connection_Create => LocaleManager.GetString("Mode_Info_Connection_Create", Culture);

		/// <summary>
		/// Construction cost: {0}
		/// </summary>
		public static string Mode_Info_ConstructionCost => LocaleManager.GetString("Mode_Info_ConstructionCost", Culture);

		/// <summary>
		/// Press {0} to create
		/// </summary>
		public static string Mode_Info_Create => LocaleManager.GetString("Mode_Info_Create", Culture);

		/// <summary>
		/// Hold {0} to move X{1} slower
		/// </summary>
		public static string Mode_Info_HoldToMoveSlower => LocaleManager.GetString("Mode_Info_HoldToMoveSlower", Culture);

		/// <summary>
		/// Hold {0} to step {1}
		/// </summary>
		public static string Mode_Info_HoldToStep => LocaleManager.GetString("Mode_Info_HoldToStep", Culture);

		/// <summary>
		/// Press {0} to invert network
		/// </summary>
		public static string Mode_Info_InvertNetwork => LocaleManager.GetString("Mode_Info_InvertNetwork", Culture);

		/// <summary>
		/// Press {0} to create loop
		/// </summary>
		public static string Mode_Info_Loop_Create => LocaleManager.GetString("Mode_Info_Loop_Create", Culture);

		/// <summary>
		/// Not enough money
		/// </summary>
		public static string Mode_Info_NotEnoughMoney => LocaleManager.GetString("Mode_Info_NotEnoughMoney", Culture);

		/// <summary>
		/// Out of map
		/// </summary>
		public static string Mode_Info_OutOfMap => LocaleManager.GetString("Mode_Info_OutOfMap", Culture);

		/// <summary>
		/// Press {0} or {1} to change height
		/// </summary>
		public static string Mode_Info_Parallel_ChangeHeight => LocaleManager.GetString("Mode_Info_Parallel_ChangeHeight", Culture);

		/// <summary>
		/// Press {0} to switch creation side
		/// </summary>
		public static string Mode_Info_Parallel_ChangeShift => LocaleManager.GetString("Mode_Info_Parallel_ChangeShift", Culture);

		/// <summary>
		/// Press {0} to create parallel
		/// </summary>
		public static string Mode_Info_Parallel_Create => LocaleManager.GetString("Mode_Info_Parallel_Create", Culture);

		/// <summary>
		/// Radius too big
		/// </summary>
		public static string Mode_Info_RadiusTooBig => LocaleManager.GetString("Mode_Info_RadiusTooBig", Culture);

		/// <summary>
		/// Radius too small
		/// </summary>
		public static string Mode_Info_RadiusTooSmall => LocaleManager.GetString("Mode_Info_RadiusTooSmall", Culture);

		/// <summary>
		/// Refund: {0}
		/// </summary>
		public static string Mode_Info_Refund => LocaleManager.GetString("Mode_Info_Refund", Culture);

		/// <summary>
		/// Select first segment
		/// </summary>
		public static string Mode_Info_SelectFirstSegment => LocaleManager.GetString("Mode_Info_SelectFirstSegment", Culture);

		/// <summary>
		/// Select second segment
		/// </summary>
		public static string Mode_Info_SelectSecondSegment => LocaleManager.GetString("Mode_Info_SelectSecondSegment", Culture);

		/// <summary>
		/// Select segment
		/// </summary>
		public static string Mode_Info_SelectSegment => LocaleManager.GetString("Mode_Info_SelectSegment", Culture);

		/// <summary>
		/// Press {0} to apply slope
		/// </summary>
		public static string Mode_Info_Slope_Apply => LocaleManager.GetString("Mode_Info_Slope_Apply", Culture);

		/// <summary>
		/// Shift - X10, Ctrl - X0.1, Alt - X0.01
		/// </summary>
		public static string Mode_Info_Step => LocaleManager.GetString("Mode_Info_Step", Culture);

		/// <summary>
		/// Press {0} to switch follow terrain
		/// </summary>
		public static string Mode_Info_SwitchFollowTerrain => LocaleManager.GetString("Mode_Info_SwitchFollowTerrain", Culture);

		/// <summary>
		/// Press {0} to switch selected offset
		/// </summary>
		public static string Mode_Info_SwitchOffset => LocaleManager.GetString("Mode_Info_SwitchOffset", Culture);

		/// <summary>
		/// Hold {0} to underground mode
		/// </summary>
		public static string Mode_Info_UndergroundMode => LocaleManager.GetString("Mode_Info_UndergroundMode", Culture);

		/// <summary>
		/// Wrong shape
		/// </summary>
		public static string Mode_Info_WrongShape => LocaleManager.GetString("Mode_Info_WrongShape", Culture);

		/// <summary>
		/// Intersect segments mode
		/// </summary>
		public static string Mode_IntersectSegment => LocaleManager.GetString("Mode_IntersectSegment", Culture);

		/// <summary>
		/// These segments have common node
		/// </summary>
		public static string Mode_IntersectSegment_Info_CommonNode => LocaleManager.GetString("Mode_IntersectSegment_Info_CommonNode", Culture);

		/// <summary>
		/// Edge of one segment is too close to end of second segment
		/// </summary>
		public static string Mode_IntersectSegment_Info_EdgeTooClose => LocaleManager.GetString("Mode_IntersectSegment_Info_EdgeTooClose", Culture);

		/// <summary>
		/// These segments don’t intersect
		/// </summary>
		public static string Mode_IntersectSegment_Info_NotIntersect => LocaleManager.GetString("Mode_IntersectSegment_Info_NotIntersect", Culture);

		/// <summary>
		/// Invert segment mode
		/// </summary>
		public static string Mode_InvertSegment => LocaleManager.GetString("Mode_InvertSegment", Culture);

		/// <summary>
		/// Click to invert segment
		/// </summary>
		public static string Mode_InvertSegment_Info_ClickToReverse => LocaleManager.GetString("Mode_InvertSegment_Info_ClickToReverse", Culture);

		/// <summary>
		/// Select segment to invert it
		/// </summary>
		public static string Mode_InvertSegment_Info_SelectToReverse => LocaleManager.GetString("Mode_InvertSegment_Info_SelectToReverse", Culture);

		/// <summary>
		/// This node can't be selected
		/// </summary>
		public static string Mode_NodeLine_Info_NotConnected => LocaleManager.GetString("Mode_NodeLine_Info_NotConnected", Culture);

		/// <summary>
		/// Select node to add it to order
		/// </summary>
		public static string Mode_NodeLine_Info_SelectNode => LocaleManager.GetString("Mode_NodeLine_Info_SelectNode", Culture);

		/// <summary>
		/// {0}%
		/// </summary>
		public static string Mode_PercentagesFormat => LocaleManager.GetString("Mode_PercentagesFormat", Culture);

		/// <summary>
		/// {0}m
		/// </summary>
		public static string Mode_RadiusFormat => LocaleManager.GetString("Mode_RadiusFormat", Culture);

		/// <summary>
		/// Remove node mode
		/// </summary>
		public static string Mode_RemoveNode => LocaleManager.GetString("Mode_RemoveNode", Culture);

		/// <summary>
		/// Not possible to remove this node
		/// </summary>
		public static string Mode_RemoveNode_Info_NotAllow => LocaleManager.GetString("Mode_RemoveNode_Info_NotAllow", Culture);

		/// <summary>
		/// Set slope mode
		/// </summary>
		public static string Mode_SlopeNode => LocaleManager.GetString("Mode_SlopeNode", Culture);

		/// <summary>
		/// Split node mode
		/// </summary>
		public static string Mode_SplitNode => LocaleManager.GetString("Mode_SplitNode", Culture);

		/// <summary>
		/// Click to remove segment from split order
		/// </summary>
		public static string Mode_SplitNode_Info_ClickFromOrder => LocaleManager.GetString("Mode_SplitNode_Info_ClickFromOrder", Culture);

		/// <summary>
		/// Click to add segment to split order
		/// </summary>
		public static string Mode_SplitNode_Info_ClickToOrder => LocaleManager.GetString("Mode_SplitNode_Info_ClickToOrder", Culture);

		/// <summary>
		/// Click to split node and align to terrain height
		/// </summary>
		public static string Mode_SplitNode_Info_ClickToSplit => LocaleManager.GetString("Mode_SplitNode_Info_ClickToSplit", Culture);

		/// <summary>
		/// Not possible split node with only one segment
		/// </summary>
		public static string Mode_SplitNode_Info_NotAllowedSplit => LocaleManager.GetString("Mode_SplitNode_Info_NotAllowedSplit", Culture);

		/// <summary>
		/// Not possible add more segments to split order
		/// </summary>
		public static string Mode_SplitNode_Info_OrderIsFull => LocaleManager.GetString("Mode_SplitNode_Info_OrderIsFull", Culture);

		/// <summary>
		/// Select segments to split
		/// </summary>
		public static string Mode_SplitNode_Info_SelectToSplit => LocaleManager.GetString("Mode_SplitNode_Info_SelectToSplit", Culture);

		/// <summary>
		/// Position is too far from source node
		/// </summary>
		public static string Mode_SplitNode_Info_TooFar => LocaleManager.GetString("Mode_SplitNode_Info_TooFar", Culture);

		/// <summary>
		/// Union nodes mode
		/// </summary>
		public static string Mode_UnionNode => LocaleManager.GetString("Mode_UnionNode", Culture);

		/// <summary>
		/// Click to select source node
		/// </summary>
		public static string Mode_UnionNode_Info_ClickSource => LocaleManager.GetString("Mode_UnionNode_Info_ClickSource", Culture);

		/// <summary>
		/// Click to unite nodes
		/// </summary>
		public static string Mode_UnionNode_Info_ClickUnion => LocaleManager.GetString("Mode_UnionNode_Info_ClickUnion", Culture);

		/// <summary>
		/// These nodes have common segment
		/// </summary>
		public static string Mode_UnionNode_Info_NoCommon => LocaleManager.GetString("Mode_UnionNode_Info_NoCommon", Culture);

		/// <summary>
		/// Total number of nodes must be no more then 8
		/// </summary>
		public static string Mode_UnionNode_Info_Overflow => LocaleManager.GetString("Mode_UnionNode_Info_Overflow", Culture);

		/// <summary>
		/// Select source node
		/// </summary>
		public static string Mode_UnionNode_Info_SelectSource => LocaleManager.GetString("Mode_UnionNode_Info_SelectSource", Culture);

		/// <summary>
		/// Select target node
		/// </summary>
		public static string Mode_UnionNode_Info_SelectTarget => LocaleManager.GetString("Mode_UnionNode_Info_SelectTarget", Culture);

		/// <summary>
		/// Nodes too far from each other
		/// </summary>
		public static string Mode_UnionNode_Info_TooFar => LocaleManager.GetString("Mode_UnionNode_Info_TooFar", Culture);

		/// <summary>
		/// {0}U
		/// </summary>
		public static string Mode_UnitsFormat => LocaleManager.GetString("Mode_UnitsFormat", Culture);

		/// <summary>
		/// Unlock segment mode
		/// </summary>
		public static string Mode_UnlockSegment => LocaleManager.GetString("Mode_UnlockSegment", Culture);

		/// <summary>
		/// Select segment to
		/// </summary>
		public static string Mode_UnlockSegment_Info_ChangeLock => LocaleManager.GetString("Mode_UnlockSegment_Info_ChangeLock", Culture);

		/// <summary>
		/// Click to lock this segment
		/// </summary>
		public static string Mode_UnlockSegment_Info_ClickToLock => LocaleManager.GetString("Mode_UnlockSegment_Info_ClickToLock", Culture);

		/// <summary>
		/// Click to unlock this segment
		/// </summary>
		public static string Mode_UnlockSegment_Info_ClickToUnlock => LocaleManager.GetString("Mode_UnlockSegment_Info_ClickToUnlock", Culture);

		/// <summary>
		/// Many different tools for working with networks
		/// </summary>
		public static string Mod_Description => LocaleManager.GetString("Mod_Description", Culture);

		/// <summary>
		/// [NEW] Added Split node mode: Select segments and split it to new node.
		/// </summary>
		public static string Mod_WhatsNewMessage1_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_1", Culture);

		/// <summary>
		/// [NEW] Added Create parallel mode: Select exist network nodes and create new network parallel them. P
		/// </summary>
		public static string Mod_WhatsNewMessage1_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_2", Culture);

		/// <summary>
		/// [NEW] Added option for auto connecting ends of new parallel network to existing network of same type
		/// </summary>
		public static string Mod_WhatsNewMessage1_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_3", Culture);

		/// <summary>
		/// [FIXED] Returned the ability to invert any network, not only one direction roads.
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_1 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_1", Culture);

		/// <summary>
		/// [UPDATED] Added Plazas & Promenades DLC Support.
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_2 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_2", Culture);

		/// <summary>
		/// [NEW] Added Network Anarchy Support.
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_3 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_3", Culture);

		/// <summary>
		/// [UPDATED] New settings UI
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_4 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_4", Culture);

		/// <summary>
		/// [UPDATED] Added Hotels&Retreats DLC support.
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_5 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_5", Culture);

		/// <summary>
		/// [UPDATED] Updated required game version to 1.18.1-f3
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_6 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_6", Culture);

		/// <summary>
		/// [UPDATED] Updated required game version to 1.19.2-f3
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_7 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_7", Culture);

		/// <summary>
		/// [UPDATED] Updated required game version to 1.20.1-f1
		/// </summary>
		public static string Mod_WhatsNewMessage1_3_8 => LocaleManager.GetString("Mod_WhatsNewMessage1_3_8", Culture);

		/// <summary>
		/// Activation shortcuts
		/// </summary>
		public static string Settings_ActivationShortcuts => LocaleManager.GetString("Settings_ActivationShortcuts", Culture);

		/// <summary>
		/// Connect parallel road to nearest nodes automatically
		/// </summary>
		public static string Settings_AutoConnect => LocaleManager.GetString("Settings_AutoConnect", Culture);

		/// <summary>
		/// Auto hide modes panel
		/// </summary>
		public static string Settings_AutoHideModePanel => LocaleManager.GetString("Settings_AutoHideModePanel", Culture);

		/// <summary>
		/// Creation modes' common shortcuts
		/// </summary>
		public static string Settings_CommonCreateShortcuts => LocaleManager.GetString("Settings_CommonCreateShortcuts", Culture);

		/// <summary>
		/// Modes' common shortcuts
		/// </summary>
		public static string Settings_CommonShortcuts => LocaleManager.GetString("Settings_CommonShortcuts", Culture);

		/// <summary>
		/// Ground roads follow terrain
		/// </summary>
		public static string Settings_FollowTerrain => LocaleManager.GetString("Settings_FollowTerrain", Culture);

		/// <summary>
		/// Gameplay
		/// </summary>
		public static string Settings_Gameplay => LocaleManager.GetString("Settings_Gameplay", Culture);

		/// <summary>
		/// Interface
		/// </summary>
		public static string Settings_Interface => LocaleManager.GetString("Settings_Interface", Culture);

		/// <summary>
		/// Length unit of measurement
		/// </summary>
		public static string Settings_LengthUnit => LocaleManager.GetString("Settings_LengthUnit", Culture);

		/// <summary>
		/// Meters
		/// </summary>
		public static string Settings_LengthUniteMeters => LocaleManager.GetString("Settings_LengthUniteMeters", Culture);

		/// <summary>
		/// Game units (1U = 8m)
		/// </summary>
		public static string Settings_LengthUniteUnits => LocaleManager.GetString("Settings_LengthUniteUnits", Culture);

		/// <summary>
		/// Requires money to build
		/// </summary>
		public static string Settings_NeedMoney => LocaleManager.GetString("Settings_NeedMoney", Culture);

		/// <summary>
		/// Panel columns count
		/// </summary>
		public static string Settings_PanelColumns => LocaleManager.GetString("Settings_PanelColumns", Culture);

		/// <summary>
		/// Panel opening side
		/// </summary>
		public static string Settings_PanelOpenSide => LocaleManager.GetString("Settings_PanelOpenSide", Culture);

		/// <summary>
		/// Down
		/// </summary>
		public static string Settings_PanelOpenSideDown => LocaleManager.GetString("Settings_PanelOpenSideDown", Culture);

		/// <summary>
		/// Up
		/// </summary>
		public static string Settings_PanelOpenSideUp => LocaleManager.GetString("Settings_PanelOpenSideUp", Culture);

		/// <summary>
		/// Play audio and visual effects
		/// </summary>
		public static string Settings_PlayEffects => LocaleManager.GetString("Settings_PlayEffects", Culture);

		/// <summary>
		/// Creation preview
		/// </summary>
		public static string Settings_PreviewType => LocaleManager.GetString("Settings_PreviewType", Culture);

		/// <summary>
		/// Both
		/// </summary>
		public static string Settings_PreviewTypeBoth => LocaleManager.GetString("Settings_PreviewTypeBoth", Culture);

		/// <summary>
		/// Network visualization
		/// </summary>
		public static string Settings_PreviewTypeMesh => LocaleManager.GetString("Settings_PreviewTypeMesh", Culture);

		/// <summary>
		/// Overlay
		/// </summary>
		public static string Settings_PreviewTypeOverlay => LocaleManager.GetString("Settings_PreviewTypeOverlay", Culture);

		/// <summary>
		/// Max Segment Length
		/// </summary>
		public static string Settings_SegmentLength => LocaleManager.GetString("Settings_SegmentLength", Culture);

		/// <summary>
		/// Apply mode action
		/// </summary>
		public static string Settings_Shortcut_Apply => LocaleManager.GetString("Settings_Shortcut_Apply", Culture);

		/// <summary>
		/// Decrease start angle
		/// </summary>
		public static string Settings_Shortcut_DecreaseAngle => LocaleManager.GetString("Settings_Shortcut_DecreaseAngle", Culture);

		/// <summary>
		/// Decrease height
		/// </summary>
		public static string Settings_Shortcut_DecreaseHeight => LocaleManager.GetString("Settings_Shortcut_DecreaseHeight", Culture);

		/// <summary>
		/// Decrease circle start offset
		/// </summary>
		public static string Settings_Shortcut_DecreaseOffset => LocaleManager.GetString("Settings_Shortcut_DecreaseOffset", Culture);

		/// <summary>
		/// Decrease one radius
		/// </summary>
		public static string Settings_Shortcut_DecreaseOneRadius => LocaleManager.GetString("Settings_Shortcut_DecreaseOneRadius", Culture);

		/// <summary>
		/// Decrease radii
		/// </summary>
		public static string Settings_Shortcut_DecreaseRadii => LocaleManager.GetString("Settings_Shortcut_DecreaseRadii", Culture);

		/// <summary>
		/// Decrease radius
		/// </summary>
		public static string Settings_Shortcut_DecreaseRadius => LocaleManager.GetString("Settings_Shortcut_DecreaseRadius", Culture);

		/// <summary>
		/// Decrease offset
		/// </summary>
		public static string Settings_Shortcut_DecreaseShift => LocaleManager.GetString("Settings_Shortcut_DecreaseShift", Culture);

		/// <summary>
		/// Distribute nodes evenly between intersections
		/// </summary>
		public static string Settings_Shortcut_DistributeBetweenIntersections => LocaleManager.GetString("Settings_Shortcut_DistributeBetweenIntersections", Culture);

		/// <summary>
		/// Distribute nodes evenly on circle
		/// </summary>
		public static string Settings_Shortcut_DistributeEvenly => LocaleManager.GetString("Settings_Shortcut_DistributeEvenly", Culture);

		/// <summary>
		/// Distribute intersections evenly on circle
		/// </summary>
		public static string Settings_Shortcut_DistributeIntersections => LocaleManager.GetString("Settings_Shortcut_DistributeIntersections", Culture);

		/// <summary>
		/// Increase start angle
		/// </summary>
		public static string Settings_Shortcut_IncreaseAngle => LocaleManager.GetString("Settings_Shortcut_IncreaseAngle", Culture);

		/// <summary>
		/// Increase height
		/// </summary>
		public static string Settings_Shortcut_IncreaseHeight => LocaleManager.GetString("Settings_Shortcut_IncreaseHeight", Culture);

		/// <summary>
		/// Increase circle start offset
		/// </summary>
		public static string Settings_Shortcut_IncreaseOffset => LocaleManager.GetString("Settings_Shortcut_IncreaseOffset", Culture);

		/// <summary>
		/// Increase one radius
		/// </summary>
		public static string Settings_Shortcut_IncreaseOneRadius => LocaleManager.GetString("Settings_Shortcut_IncreaseOneRadius", Culture);

		/// <summary>
		/// Increase radii
		/// </summary>
		public static string Settings_Shortcut_IncreaseRadii => LocaleManager.GetString("Settings_Shortcut_IncreaseRadii", Culture);

		/// <summary>
		/// Increase radius
		/// </summary>
		public static string Settings_Shortcut_IncreaseRadius => LocaleManager.GetString("Settings_Shortcut_IncreaseRadius", Culture);

		/// <summary>
		/// Increase offset
		/// </summary>
		public static string Settings_Shortcut_IncreaseShift => LocaleManager.GetString("Settings_Shortcut_IncreaseShift", Culture);

		/// <summary>
		/// Invert network
		/// </summary>
		public static string Settings_Shortcut_InvertNetwork => LocaleManager.GetString("Settings_Shortcut_InvertNetwork", Culture);

		/// <summary>
		/// Switch creation side
		/// </summary>
		public static string Settings_Shortcut_InvertShift => LocaleManager.GetString("Settings_Shortcut_InvertShift", Culture);

		/// <summary>
		/// Reset to default circle
		/// </summary>
		public static string Settings_Shortcut_ResetArrangeCircle => LocaleManager.GetString("Settings_Shortcut_ResetArrangeCircle", Culture);

		/// <summary>
		/// Switch follow terrain
		/// </summary>
		public static string Settings_Shortcut_SwitchFollowTerrain => LocaleManager.GetString("Settings_Shortcut_SwitchFollowTerrain", Culture);

		/// <summary>
		/// Switch loop direction
		/// </summary>
		public static string Settings_Shortcut_SwitchIsLoop => LocaleManager.GetString("Settings_Shortcut_SwitchIsLoop", Culture);

		/// <summary>
		/// Switch selected offset
		/// </summary>
		public static string Settings_Shortcut_SwitchOffset => LocaleManager.GetString("Settings_Shortcut_SwitchOffset", Culture);

		/// <summary>
		/// Switch selected circle
		/// </summary>
		public static string Settings_Shortcut_SwitchSelect => LocaleManager.GetString("Settings_Shortcut_SwitchSelect", Culture);

		/// <summary>
		/// Set color to slope label depending on the value
		/// </summary>
		public static string Settings_SlopeColors => LocaleManager.GetString("Settings_SlopeColors", Culture);

		/// <summary>
		/// Slope unit of measurement
		/// </summary>
		public static string Settings_SlopeUnit => LocaleManager.GetString("Settings_SlopeUnit", Culture);

		/// <summary>
		/// Degrees
		/// </summary>
		public static string Settings_SlopeUnitDegrees => LocaleManager.GetString("Settings_SlopeUnitDegrees", Culture);

		/// <summary>
		/// Percentages
		/// </summary>
		public static string Settings_SlopeUnitPercentages => LocaleManager.GetString("Settings_SlopeUnitPercentages", Culture);

		/// <summary>
		/// Click to remove
		/// </summary>
		public static string Tool_RemoveNode_Info_ClickToRemove => LocaleManager.GetString("Tool_RemoveNode_Info_ClickToRemove", Culture);

		/// <summary>
		/// Select node to remove it
		/// </summary>
		public static string Tool_RemoveNode_Info_Select => LocaleManager.GetString("Tool_RemoveNode_Info_Select", Culture);
	}
}
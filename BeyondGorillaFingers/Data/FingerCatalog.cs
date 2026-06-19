namespace BeyondGorillaFingers.Data;

internal enum HandSide
{
    Left,
    Right,
}

internal enum PhantomOwner
{
    Neutral,
    LeftHand,
    RightHand,
}

internal sealed class FingerDefinition
{
    internal readonly string[] BonePaths;
    internal readonly string   Name;
    internal readonly HandSide Side;

    internal FingerDefinition(string name, HandSide side, params string[] bonePaths)
    {
        Name      = name;
        Side      = side;
        BonePaths = bonePaths;
    }
}

internal static class FingerCatalog
{
    internal static readonly FingerDefinition[] All =
    [
            new(
                    "LeftIndex",
                    HandSide.Left,
                    "rig/hand.L/palm.01.L/f_index.01.L",
                    "rig/hand.L/palm.01.L/f_index.01.L/f_index.02.L",
                    "rig/hand.L/palm.01.L/f_index.01.L/f_index.02.L/f_index.03.L",
                    "rig/hand.L/palm.01.L/f_index.01.L/f_index.02.L/f_index.03.L/f_index.03.L_end"
            ),

            new(
                    "LeftMiddle",
                    HandSide.Left,
                    "rig/hand.L/palm.02.L/f_middle.01.L",
                    "rig/hand.L/palm.02.L/f_middle.01.L/f_middle.02.L",
                    "rig/hand.L/palm.02.L/f_middle.01.L/f_middle.02.L/f_middle.03.L",
                    "rig/hand.L/palm.02.L/f_middle.01.L/f_middle.02.L/f_middle.03.L/f_middle.03.L_end"
            ),

            new(
                    "LeftThumb",
                    HandSide.Left,
                    "rig/hand.L/palm.01.L/thumb.01.L",
                    "rig/hand.L/palm.01.L/thumb.01.L/thumb.02.L",
                    "rig/hand.L/palm.01.L/thumb.01.L/thumb.02.L/thumb.03.L",
                    "rig/hand.L/palm.01.L/thumb.01.L/thumb.02.L/thumb.03.L/thumb.03.L_end"
            ),

            new(
                    "RightIndex",
                    HandSide.Right,
                    "rig/hand.R/palm.01.R/f_index.01.R",
                    "rig/hand.R/palm.01.R/f_index.01.R/f_index.02.R",
                    "rig/hand.R/palm.01.R/f_index.01.R/f_index.02.R/f_index.03.R",
                    "rig/hand.R/palm.01.R/f_index.01.R/f_index.02.R/f_index.03.R/f_index.03.R_end"
            ),

            new(
                    "RightMiddle",
                    HandSide.Right,
                    "rig/hand.R/palm.02.R/f_middle.01.R",
                    "rig/hand.R/palm.02.R/f_middle.01.R/f_middle.02.R",
                    "rig/hand.R/palm.02.R/f_middle.01.R/f_middle.02.R/f_middle.03.R",
                    "rig/hand.R/palm.02.R/f_middle.01.R/f_middle.02.R/f_middle.03.R/f_middle.03.R_end"
            ),

            new(
                    "RightThumb",
                    HandSide.Right,
                    "rig/hand.R/palm.01.R/thumb.01.R",
                    "rig/hand.R/palm.01.R/thumb.01.R/thumb.02.R",
                    "rig/hand.R/palm.01.R/thumb.01.R/thumb.02.R/thumb.03.R",
                    "rig/hand.R/palm.01.R/thumb.01.R/thumb.02.R/thumb.03.R/thumb.03.R_end"
            ),
    ];

    internal static PhantomOwner OwnerFor(HandSide side) =>
            side == HandSide.Left ? PhantomOwner.LeftHand : PhantomOwner.RightHand;
}
public sealed class SummonSpawnGroup
{
    public SummonBodySize Size { get; private set; }

    public int TotalMembers { get; private set; }

    public int AliveMembers { get; private set; }

    public int CapacityCost { get; private set; }

    private bool capacityReleased;

    public SummonSpawnGroup(
        SummonBodySize size,
        int totalMembers,
        int capacityCost)
    {
        Size = size;

        TotalMembers =
            System.Math.Max(
                1,
                totalMembers
            );

        AliveMembers =
            TotalMembers;

        CapacityCost =
            System.Math.Max(
                0,
                capacityCost
            );

        capacityReleased =
            false;
    }

    public void NotifyMemberDied()
    {
        if (AliveMembers <= 0)
        {
            return;
        }

        AliveMembers--;

        if (AliveMembers > 0)
        {
            return;
        }

        AliveMembers = 0;

        ReleaseCapacity();
    }

    private void ReleaseCapacity()
    {
        if (capacityReleased)
        {
            return;
        }

        capacityReleased =
            true;

        if (SummonManager.Instance != null)
        {
            SummonManager.Instance.ReleaseCapacity(
                CapacityCost
            );
        }
    }
}
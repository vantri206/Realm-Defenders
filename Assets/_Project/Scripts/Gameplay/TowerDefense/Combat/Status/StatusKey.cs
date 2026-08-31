public readonly struct StatusKey
{
    public string StatusId { get; }
    public int SourceId { get; }

    public StatusKey(string statusId, int sourceId)
    {
        StatusId = statusId;
        SourceId = sourceId;
    }

    public bool IsStatus(StatusKey other)
    {
        return SourceId == other.SourceId && StatusId == other.StatusId;
    }
}

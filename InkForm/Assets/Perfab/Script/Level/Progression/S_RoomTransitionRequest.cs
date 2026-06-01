public enum S_RoomEntryMode
{
    InkPod,
    Door
}

public struct S_RoomTransitionRequest
{
    public RoomId SourceRoom { get; private set; }
    public RoomId TargetRoom { get; private set; }
    public S_RoomEntryMode EntryMode { get; private set; }

    public S_RoomTransitionRequest(RoomId targetRoom, S_RoomEntryMode entryMode, RoomId sourceRoom = RoomId.None)
    {
        SourceRoom = sourceRoom;
        TargetRoom = targetRoom;
        EntryMode = entryMode;
    }
}

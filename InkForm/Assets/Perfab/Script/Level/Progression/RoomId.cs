/// <summary>
/// Facility room identifiers. Used by S_RoomGraph and S_RoomExit for graph navigation.
/// Training rooms are not graph nodes; they live in a separate scene pool on the controller.
/// </summary>
public enum RoomId
{
    None = 0,
    TR,    // training room hub
    ComR,  // computer room
    PS,    // power station
    BF,    // bio-freeze lab (contains Dr.R's hidden room)
    LivA,  // living area
    For    // factory
}

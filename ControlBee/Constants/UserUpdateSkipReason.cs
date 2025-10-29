namespace ControlBee.Constants;

public enum UserUpdateSkipReason
{
    TargetNotFound,
    CannotEditPeerOrHigher,
    LevelMustBeLowerThanCurrentUser,
    SelfLevelChangeNotAllowed
}
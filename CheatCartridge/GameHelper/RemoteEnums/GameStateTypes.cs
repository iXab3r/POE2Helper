namespace CheatCartridge.GameHelper.RemoteEnums;

/// <summary>
///     Gets all known states of the game.
/// </summary>
public enum GameStateTypes
{
    /// <summary>
    ///     When user is on the Area Loading Screen.
    /// </summary>
    AreaLoadingState = 0,

    /// <summary>
    ///     Game State.
    /// </summary>
    ChangePasswordState = 1,

    /// <summary>
    ///     When user is viewing the credit screen window (accessable from login screen)
    /// </summary>
    CreditsState = 2,

    /// <summary>
    ///     When user has opened the escape menu.
    /// </summary>
    EscapeState = 3,

    /// <summary>
    ///     When User is in Town/Hideout/Area/Zone etc.
    /// </summary>
    InGameState = 4,

    /// <summary>
    ///     The user is watching the GGG animation that comes before the login screen.
    /// </summary>
    PreGameState = 5,

    /// <summary>
    ///     When user is on the Login screen.
    /// </summary>
    LoginState = 6,

    /// <summary>
    ///     When user is transitioning from <see cref="PreGameState"/> to <see cref="LoginState"/>.
    /// </summary>
    WaitingState = 7,

    /// <summary>
    ///     When user is on the create new character screen.
    /// </summary>
    CreateCharacterState = 8,

    /// <summary>
    ///     When user is on the select character screen.
    /// </summary>
    SelectCharacterState = 9,

    /// <summary>
    ///     When user is on the delete character screen.
    /// </summary>
    DeleteCharacterState = 10,

    /// <summary>
    ///     When user is transitioning from <see cref="SelectCharacterState"/> to <see cref="InGameState"/>.
    /// </summary>
    LoadingState = 11,

    /// <summary>
    ///     This is a special State, changing to this state will not trigger State Change Event.
    ///     This is just for displaying purposes. It means Game isn't stared.
    /// </summary>
    GameNotLoaded = 12
}
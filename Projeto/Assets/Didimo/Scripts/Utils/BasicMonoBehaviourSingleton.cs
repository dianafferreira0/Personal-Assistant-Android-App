namespace Didimo.Utils
{
    /// <summary>
    /// A basic singleton that is a monobehaviour. Common usage is to create Co-routines from classes that are not monobehaviours and don't have a reference to a monobehaviour at hand.
    /// </summary>
    public class BasicMonoBehaviourSingleton : MonoBehaviourSingleton<BasicMonoBehaviourSingleton>
    {
    }
}
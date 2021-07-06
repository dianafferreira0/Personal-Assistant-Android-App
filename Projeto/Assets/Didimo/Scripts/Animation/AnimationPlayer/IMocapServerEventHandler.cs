namespace Didimo.Animation.AnimationPlayer
{
    public interface IMocapServerEventHandler
    {
        void OnGetServerAudio(short[] audio, ulong timeStamp);
        void OnGetServerFacNames(string[] facNames);
        void OnGetServerFacs(float[] facs, ulong timeStamp);
    }
}
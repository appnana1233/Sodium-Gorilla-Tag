// Code by Potassium, made by niko-tar-gz https://github.com/niko-tar-gz/Potassium

using UnityEngine;

namespace Sodium
{
    internal sealed class RoomJoinStuff : MonoBehaviour
    {
        private void Update()
        {
            RoomJoinPatch.RunNext();
            ScoreboardRefreshPatch.RunNext();
            RigPoolPatch.WarmNext();
        }
    }
}
